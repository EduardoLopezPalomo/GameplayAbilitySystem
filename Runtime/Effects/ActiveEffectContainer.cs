using System;
using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Cues;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// Apply and manage active gameplay effects on a character.
    /// Call Tick every frame.
    /// </summary>
    public sealed class ActiveEffectContainer
    {
        // Marginal tolerance for time comparisons, to avoid issues with floating point precision.
        private const float TimeTolerance = 0.0001f;

        // Maximum executions of one periodic effect in a single Tick.
        // Protects against huge time steps (lag spikes, wrong deltaTime...).
        private const int MaxExecutionsPerTick = 32;

        // Maximum passes when checking ongoing requirements again. Two effects could keep
        // switching each other on and off, and this makes sure it always finishes.
        private const int MaxSuspensionPasses = 8;

        private static readonly AppliedModifier[] NoModifiers = Array.Empty<AppliedModifier>();

        private readonly AttributeSet attributes;
        private readonly GameplayTagCountContainer ownedTags;

        // Immunity tags granted by the active effects.
        private readonly GameplayTagCountContainer immunityTags = new GameplayTagCountContainer();

        private readonly List<ActiveGameplayEffect> active = new List<ActiveGameplayEffect>();
        private readonly List<ActiveGameplayEffect> executing = new List<ActiveGameplayEffect>();
        private readonly List<ActiveGameplayEffect> expired = new List<ActiveGameplayEffect>();

        // Output buffers for executions, one per nesting level (an event can apply another effect in the middle).
        private readonly List<ExecutionOutput> outputPool = new List<ExecutionOutput>();
        private int outputDepth;

        // Copies of the active list to iterate safely, one per nesting level.
        private readonly List<List<ActiveGameplayEffect>> snapshots = new List<List<ActiveGameplayEffect>>();
        private int snapshotDepth;

        private int nextId = 1;

        // Active effects that have ongoing requirements. If there are none, tag changes cost nothing.
        private int ongoingCount;
        private bool checkingSuspension;
        private bool suspensionDirty;
        
        //Set by AbilitySystemComponent. Null means this character has no cues
        internal GameplayCueRunner Cues;

        /// <summary>Number of active effects.</summary>
        public int Count => active.Count;

        /// <summary>
        /// Raised after an effect has been applied to this character (instant effects too).
        /// It is also raised when applying it again changes a stack (a new charge, a refreshed duration).
        /// </summary>
        public event Action<GameplayEffectSpec> EffectApplied;

        public ActiveEffectContainer(AttributeSet attributes, GameplayTagCountContainer ownedTags)
        {
            this.attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            this.ownedTags = ownedTags ?? throw new ArgumentNullException(nameof(ownedTags));

            // Both live as long as the character, so this needs no unsubscribing.
            this.ownedTags.TagsChanged += OnOwnedTagsChanged;
        }

        public ActiveGameplayEffect GetActiveAt(int index) => active[index];

        // ---------- Queries ----------

        /// <summary>
        /// True if the effect can be applied: the character has all the required tags,
        /// none of the blocked tags, and no active effect gives it immunity to this one.
        /// </summary>
        public bool CanApply(GameplayEffectDefinition effect)
        {
            if (effect == null)
                return false;

            return ownedTags.HasAll(effect.ApplicationRequiredTags)
                   && !ownedTags.HasAny(effect.ApplicationBlockedTags)
                   && !effect.IsCoveredBy(immunityTags);
        }

        /// <summary>True if an active effect gives this character immunity to the effect.</summary>
        public bool IsImmuneTo(GameplayEffectDefinition effect)
        {
            return effect != null && effect.IsCoveredBy(immunityTags);
        }

        /// <summary>
        /// True if the character could pay this effect as a cost: it can be applied (tags)
        /// and no Add modifier would take an attribute below its minimum.
        /// The cost is calculated as if the character applied it to itself.
        /// </summary>
        public bool CanAfford(GameplayEffectDefinition cost)
        {
            if (cost == null)
                return true;

            if (!CanApply(cost))
                return false;

            var selfSpec = new GameplayEffectSpec(cost, null);
            var modifiers = cost.Modifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                EffectModifierDefinition modifier = modifiers[i];

                if (!attributes.TryGetAttribute(modifier.AttributeTag, out GameplayAttribute attribute))
                    return false;

                // No warnings here: this is called many times (UI, debugger).
                float magnitude = ResolveMagnitude(cost, modifier, selfSpec, false);

                if (modifier.Operation == ModifierOperation.Add && attribute.currentValue + magnitude < attribute.Definition.MinValue)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Longest remaining time of the active effects that grant any of these tags (0 if none).
        /// </summary>
        public float GetRemainingTime(GameplayTagContainer grantedTags)
        {
            float longest = 0f;
            for (int i = 0; i < active.Count; i++)
            {
                ActiveGameplayEffect effect = active[i];
                if (effect.RemainingTime > longest && effect.Definition.GrantedTags.HasAny(grantedTags))
                    longest = effect.RemainingTime;
            }
            return longest;
        }

        // ---------- Applying and removing ----------

        /// <summary>
        /// Applies an effect to this character without an instigator (it counts as self-applied).
        /// Returns a handle to the active effect, or ActiveEffectHandle.None if it is instant or could not be applied.
        /// </summary>
        public ActiveEffectHandle ApplyEffect(GameplayEffectDefinition effect)
        {
            TryApplyEffect(effect, out ActiveEffectHandle handle);
            return handle;
        }

        /// <summary>
        /// Applies an effect without an instigator. Returns true if it was applied.
        /// The handle is only valid for effects with duration or infinite.
        /// </summary>
        public bool TryApplyEffect(GameplayEffectDefinition effect, out ActiveEffectHandle handle)
        {
            return TryApplySpec(new GameplayEffectSpec(effect, null), out handle);
        }

        /// <summary>
        /// Applies a spec to this character. Returns true if it was applied.
        /// For an effect that was already active, the stacking policy decides: it returns true
        /// if a charge was added or the duration was refreshed, and the handle is the existing effect's.
        /// </summary>
        public bool TryApplySpec(in GameplayEffectSpec spec, out ActiveEffectHandle handle)
        {
            handle = ActiveEffectHandle.None;

            GameplayEffectDefinition effect = spec.Definition;
            if (effect == null)
            {
                Debug.LogWarning("ActiveEffectContainer: the effect is empty.");
                return false;
            }

            if (!CanApply(effect))
                return false;

            // The cleanse goes first: an antidote removes the poison before anything else.
            if (effect.HasRemoveEffectsWithTags)
                RemoveEffectsWithTags(effect.RemoveEffectsWithTags);

            bool applied = effect.DurationPolicy == EffectDurationPolicy.Instant
                ? ApplyInstant(spec)
                : ApplyTimed(spec, out handle);

            if (applied)
                EffectApplied?.Invoke(spec);

            return applied;
        }

        /// <summary>
        /// Removes an active effect (with all its charges). Returns false if the handle is invalid or the effect no longer exists.
        /// </summary>
        public bool RemoveEffect(ActiveEffectHandle handle)
        {
            if (!TryGetActiveEffect(handle, out ActiveGameplayEffect effect))
                return false;

            Remove(effect);
            return true;
        }

        /// <summary>
        /// Removes the active effects whose Effect Tags or Granted Tags match any of these tags
        /// (a child tag matches its parent). Returns how many were removed.
        /// </summary>
        public int RemoveEffectsWithTags(GameplayTagContainer tags)
        {
            if (tags == null || tags.IsEmpty || active.Count == 0)
                return 0;

            int removed = 0;
            List<ActiveGameplayEffect> buffer = RentSnapshot();
            try
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    ActiveGameplayEffect effect = buffer[i];

                    // An event may have removed it already.
                    if (effect.IsActive && effect.Definition.MatchesAny(tags))
                    {
                        Remove(effect);
                        removed++;
                    }
                }
            }
            finally
            {
                ReturnSnapshot(buffer);
            }

            return removed;
        }

        /// <summary>
        /// Finds an active effect by its handle. Returns false if the handle is invalid or the effect no longer exists.
        /// </summary>
        public bool TryGetActiveEffect(ActiveEffectHandle handle, out ActiveGameplayEffect effect)
        {
            if (handle.IsValid)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    if (active[i].Handle.Id == handle.Id)
                    {
                        effect = active[i];
                        return true;
                    }
                }
            }

            effect = null;
            return false;
        }

        // ---------- Time ----------

        /// <summary>
        /// Advances time, executes periodic effects and removes the ones that have ended.
        /// Call it every frame with the time since the last frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return; // Paused (timeScale = 0) or nothing to do.

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                Debug.LogWarning($"ActiveEffectContainer: invalid deltaTime ({deltaTime}). Tick ignored.");
                return;
            }

            if (active.Count == 0)
                return;

            executing.Clear();
            expired.Clear();

            // Phase 1: advance times and collect what has to happen. No events are triggered here.
            for (int i = 0; i < active.Count; i++)
            {
                ActiveGameplayEffect effect = active[i];
                GameplayEffectDefinition definition = effect.Definition;
                bool timed = definition.DurationPolicy == EffectDurationPolicy.Duration;

                // If the effect ends in this frame, ignore the time left after its end.
                float usable = timed ? Mathf.Min(deltaTime, effect.RemainingTime) : deltaTime;

                // A suspended effect keeps its period frozen, so it doesn't run a burst when it resumes.
                if (definition.IsPeriodic && !effect.IsSuspended)
                {
                    effect.PeriodTimer -= usable;

                    if (effect.PeriodTimer <= TimeTolerance)
                    {
                        float period = definition.Period;

                        // How many periods have passed. A division instead of a loop: it always finishes.
                        float elapsed = Mathf.Floor((TimeTolerance - effect.PeriodTimer) / period) + 1f;

                        if (elapsed > MaxExecutionsPerTick)
                        {
                            // Huge time step: run the maximum and discard the rest of the lost time.
                            effect.PendingExecutions = MaxExecutionsPerTick;
                            effect.PeriodTimer = period;
                        }
                        else
                        {
                            effect.PendingExecutions = (int)elapsed;
                            effect.PeriodTimer += elapsed * period;
                        }

                        executing.Add(effect);
                    }
                }

                // The duration keeps running while the effect is suspended.
                if (timed)
                {
                    effect.RemainingTime -= deltaTime;
                    if (effect.RemainingTime <= TimeTolerance)
                        expired.Add(effect);
                }
            }

            // Phase 2: execute periodic effects. This triggers events, and an event can apply, remove
            // or suspend effects, so it is checked before every execution.
            for (int i = 0; i < executing.Count; i++)
            {
                ActiveGameplayEffect effect = executing[i];
                int pending = effect.PendingExecutions;
                effect.PendingExecutions = 0;

                for (int n = 0; n < pending && effect.IsActive && !effect.IsSuspended; n++)
                    ExecutePeriodic(effect);
            }

            // Phase 3: remove the effects that have ended.
            for (int i = 0; i < expired.Count; i++)
                Remove(expired[i]);

            executing.Clear();
            expired.Clear();
        }

        // ---------- Magnitudes ----------

        // Calculates the amount of one modifier. It runs once, when the effect is applied.
        private float ResolveMagnitude(GameplayEffectDefinition effect, EffectModifierDefinition modifier, in GameplayEffectSpec spec, bool warn)
        {
            switch (modifier.MagnitudeType)
            {
                case MagnitudeType.AttributeBased:
                {
                    // Without an instigator (or if it was destroyed) the character uses its own attributes.
                    AttributeSet source =
                        modifier.CaptureFrom == AttributeCaptureSource.Instigator && spec.Instigator != null
                            ? spec.Instigator.Attributes
                            : attributes;

                    if (source.TryGetAttribute(modifier.CapturedAttribute, out GameplayAttribute captured))
                        return captured.currentValue * modifier.Coefficient + modifier.Magnitude;

                    if (warn)
                    {
                        Debug.LogWarning(
                            $"ActiveEffectContainer: '{effect.name}' reads '{modifier.CapturedAttribute.Name}' from the {modifier.CaptureFrom}, but it doesn't have that attribute. Using the flat amount.");
                    }
                    return modifier.Magnitude;
                }

                case MagnitudeType.SetByCaller:
                {
                    if (spec.TryGetSetByCaller(modifier.SetByCallerTag, out float value))
                        return value;

                    if (warn)
                    {
                        Debug.LogWarning(
                            $"ActiveEffectContainer: '{effect.name}' needs a value for '{modifier.SetByCallerTag.Name}' and the caller didn't send one. Using the default ({modifier.Magnitude}).");
                    }
                    return modifier.Magnitude;
                }

                default:
                    return modifier.Magnitude;
            }
        }

        // The amount of a modifier with some charges.
        // Add: amount x charges. Multiply: the bonus grows with each charge (x1.2 becomes x1.4 with 2). Override: unchanged.
        private static float Scale(ModifierOperation operation, float magnitude, int stacks)
        {
            if (stacks <= 1)
                return magnitude;

            switch (operation)
            {
                case ModifierOperation.Add:
                    return magnitude * stacks;
                case ModifierOperation.Multiply:
                    return (magnitude - 1f) * stacks + 1f;
                default:
                    return magnitude;
            }
        }

        // ---------- Applying ----------

        private bool ApplyInstant(in GameplayEffectSpec spec)
        {
            GameplayEffectDefinition effect = spec.Definition;
            bool anyApplied = false;
            float cueMagnitude = 0f;
            var modifiers = effect.Modifiers;

            for (int i = 0; i < modifiers.Count; i++)
            {
                EffectModifierDefinition modifier = modifiers[i];

                if (!attributes.TryGetAttribute(modifier.AttributeTag, out GameplayAttribute attribute))
                {
                    WarnMissingAttribute(effect, modifier);
                    continue;
                }

                float magnitude = ResolveMagnitude(effect, modifier, spec, true);
                //Saves the magnitude of the first modifier.
                if (!anyApplied)
                    cueMagnitude = magnitude;
                
                attribute.ApplyToBaseValue(modifier.Operation, magnitude);
                anyApplied = true;
            }

            // Executions run after the modifiers.
            if (effect.HasExecutions)
            {
                RunExecutions(effect, spec, 1);
                anyApplied = true;
            }

            // An antidote has no modifiers: removing the effects is its job.
            if (effect.HasRemoveEffectsWithTags)
                anyApplied = true;
            
            // A cue-only effect (a sound, a flash) has nothing else to apply, but it is still useful.
            if (modifiers.Count == 0 && effect.HasCues)
                anyApplied = true;
            
            if (anyApplied && effect.HasCues)
                Cues?.ExecuteEach(effect.CueTags, spec.Instigator, cueMagnitude);

            return anyApplied;
        }

        private bool ApplyTimed(in GameplayEffectSpec spec, out ActiveEffectHandle handle)
        {
            handle = ActiveEffectHandle.None;

            GameplayEffectDefinition effect = spec.Definition;

            // Already active? Then the stacking policy decides.
            if (effect.StackingPolicy != EffectStackingPolicy.Independent && TryFindStack(spec, out ActiveGameplayEffect existing))
            {
                return ApplyToExistingStack(existing, out handle);
            }

            var modifiers = effect.Modifiers;
            bool periodic = effect.IsPeriodic;

            // 1. Find the attribute of each modifier and calculate its amount, once.
            //    The effect keeps that amount until it ends.
            AppliedModifier[] applied = modifiers.Count > 0 ? new AppliedModifier[modifiers.Count] : NoModifiers;
            int resolved = 0;

            for (int i = 0; i < modifiers.Count; i++)
            {
                EffectModifierDefinition modifier = modifiers[i];

                if (!attributes.TryGetAttribute(modifier.AttributeTag, out GameplayAttribute attribute))
                {
                    WarnMissingAttribute(effect, modifier);
                    continue;
                }

                float magnitude = ResolveMagnitude(effect, modifier, spec, true);
                applied[resolved++] = new AppliedModifier(attribute, ModifierHandle.None, modifier.Operation, magnitude);
            }

            // An effect without modifiers can still be useful: a "stunned" (granted tags), a ward (immunity)
            // or a periodic effect that only has executions.
            bool hasPassiveEffect = !effect.GrantedTags.IsEmpty
                                    || !effect.GrantedImmunityTags.IsEmpty
                                    || effect.HasCues;
            
            if (resolved == 0 && !hasPassiveEffect && !(periodic && effect.HasExecutions))
            {
                Debug.LogWarning($"ActiveEffectContainer: '{effect.name}' has nothing to do on this character.");
                return false;
            }

            // Remove the unused slots (only when some modifier points to a missing attribute).
            if (resolved == 0)
                applied = NoModifiers;
            else if (resolved < applied.Length)
                Array.Resize(ref applied, resolved);

            // 2. Register the effect as active.
            var effectHandle = new ActiveEffectHandle(nextId++);
            var instance = new ActiveGameplayEffect(effectHandle, effect, applied, spec) { IsActive = true };
            active.Add(instance);
            handle = effectHandle;

            if (effect.HasOngoingRequirements)
                ongoingCount++;

            // 3. Switch it on, or start it suspended if its ongoing requirements are not met.
            if (OngoingRequirementsMet(effect))
                Activate(instance);
            else
                instance.IsSuspended = true;

            if (periodic && effect.ExecuteOnApplication)
                ExecutePeriodic(instance);

            return true;
        }

        // ---------- Stacking ----------

        private bool TryFindStack(in GameplayEffectSpec spec, out ActiveGameplayEffect found)
        {
            GameplayEffectDefinition definition = spec.Definition;
            bool perInstigator = definition.StackScope == EffectStackScope.Instigator;

            for (int i = 0; i < active.Count; i++)
            {
                ActiveGameplayEffect candidate = active[i];

                if (candidate.Definition != definition)
                    continue;

                if (perInstigator && candidate.Instigator != spec.Instigator)
                    continue;

                found = candidate;
                return true;
            }

            found = null;
            return false;
        }

        // Returns true if something changed: a new charge or a refreshed duration.
        private static bool ApplyToExistingStack(ActiveGameplayEffect existing, out ActiveEffectHandle handle)
        {
            GameplayEffectDefinition definition = existing.Definition;
            bool timed = definition.DurationPolicy == EffectDurationPolicy.Duration;
            bool changed = false;

            switch (definition.StackingPolicy)
            {
                case EffectStackingPolicy.RefreshDuration:
                    if (timed)
                    {
                        existing.RemainingTime = definition.Duration;
                        changed = true;
                    }
                    break;

                case EffectStackingPolicy.StackCharges:
                    if (existing.Stacks < definition.MaxStacks)
                    {
                        SetStacks(existing, existing.Stacks + 1);
                        changed = true;
                    }

                    if (timed && definition.RefreshDurationOnStack)
                    {
                        existing.RemainingTime = definition.Duration;
                        changed = true;
                    }
                    break;

                // Ignore: nothing changes.
            }

            handle = changed ? existing.Handle : ActiveEffectHandle.None;
            return changed;
        }

        private static void SetStacks(ActiveGameplayEffect effect, int stacks)
        {
            effect.Stacks = stacks;

            // Continuous modifiers change their amount. A suspended or periodic effect has no handles, so nothing happens.
            AppliedModifier[] applied = effect.AppliedModifiers;
            for (int i = 0; i < applied.Length; i++)
            {
                if (!effect.IsActive)
                    return;

                AppliedModifier m = applied[i];
                if (m.Handle.IsValid)
                    m.Attribute.SetModifierValue(m.Handle, Scale(m.Operation, m.Magnitude, stacks));
            }
        }

        // ---------- Switching effects on and off ----------

        private static bool IsOn(ActiveGameplayEffect effect) => effect.IsActive && !effect.IsSuspended;

        // Gives the character what the effect grants: tags, immunity and continuous modifiers.
        // Any of these steps can fire events that suspend or remove the effect, so it is checked in between.
        private void Activate(ActiveGameplayEffect effect)
        {
            GameplayEffectDefinition definition = effect.Definition;

            if (!effect.TagsGranted)
            {
                effect.TagsGranted = true;
                ownedTags.AddTags(definition.GrantedTags);
            }
            if (!IsOn(effect))
                return;

            if (!effect.ImmunityGranted)
            {
                effect.ImmunityGranted = true;
                immunityTags.AddTags(definition.GrantedImmunityTags);
            }
            if (!IsOn(effect))
                return;

            AddContinuousModifiers(effect);
            
            //Add the cues if the effect is still active and they are not added
            //This calls the method OnAdded from the definition
            if (!IsOn(effect))
                return;

            if (!effect.CuesAdded && definition.HasCues)
            {
                effect.CuesAdded = true;
                Cues?.AddEach(definition.CueTags, effect.Instigator);
            }
        }

        // Takes back everything the effect has given. Used to suspend it and to remove it.
        private void Deactivate(ActiveGameplayEffect effect)
        {
            GameplayEffectDefinition definition = effect.Definition;
            
            //Removes the cues, this calls the method OnRemoved from the definition
            if (effect.CuesAdded)
            {
                effect.CuesAdded = false;
                Cues?.RemoveEach(definition.CueTags);
            }

            AppliedModifier[] applied = effect.AppliedModifiers;
            for (int i = 0; i < applied.Length; i++)
            {
                if (!applied[i].Handle.IsValid)
                    continue;

                // Cleared first, so an event can't remove it twice.
                ModifierHandle modifierHandle = applied[i].Handle;
                applied[i].Handle = ModifierHandle.None;
                applied[i].Attribute.RemoveModifier(modifierHandle);
            }

            if (effect.ImmunityGranted)
            {
                effect.ImmunityGranted = false;
                immunityTags.RemoveTags(definition.GrantedImmunityTags);
            }

            if (effect.TagsGranted)
            {
                effect.TagsGranted = false;
                ownedTags.RemoveTags(definition.GrantedTags);
            }
        }

        private static void AddContinuousModifiers(ActiveGameplayEffect effect)
        {
            // Periodic effects change the base value in each period instead.
            if (effect.Definition.IsPeriodic)
                return;

            AppliedModifier[] applied = effect.AppliedModifiers;
            for (int i = 0; i < applied.Length; i++)
            {
                if (!IsOn(effect))
                    return;

                if (applied[i].Handle.IsValid)
                    continue;

                AppliedModifier m = applied[i];
                ModifierHandle modifierHandle = m.Attribute.AddModifier(m.Operation, Scale(m.Operation, m.Magnitude, effect.Stacks));

                // The event of the change may have suspended or removed the effect: don't leave the modifier behind.
                if (!IsOn(effect))
                {
                    m.Attribute.RemoveModifier(modifierHandle);
                    return;
                }

                applied[i].Handle = modifierHandle;
            }
        }

        // ---------- Ongoing requirements ----------

        private bool OngoingRequirementsMet(GameplayEffectDefinition effect)
        {
            return ownedTags.HasAll(effect.OngoingRequiredTags)
                   && !ownedTags.HasAny(effect.OngoingBlockedTags);
        }

        private void OnOwnedTagsChanged()
        {
            if (ongoingCount == 0)
                return;

            // Switching an effect changes tags, which comes back here: mark it and let the loop below repeat.
            if (checkingSuspension)
            {
                suspensionDirty = true;
                return;
            }

            checkingSuspension = true;
            try
            {
                for (int pass = 0; pass < MaxSuspensionPasses; pass++)
                {
                    suspensionDirty = false;
                    UpdateSuspension();

                    if (!suspensionDirty)
                        return;
                }

                Debug.LogWarning(
                    "ActiveEffectContainer: ongoing requirements keep changing. Probably two effects are switching each other on and off. Stopped.");
            }
            finally
            {
                checkingSuspension = false;
            }
        }

        private void UpdateSuspension()
        {
            List<ActiveGameplayEffect> buffer = RentSnapshot();
            try
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    ActiveGameplayEffect effect = buffer[i];
                    if (!effect.IsActive || !effect.Definition.HasOngoingRequirements)
                        continue;

                    bool met = OngoingRequirementsMet(effect.Definition);

                    if (met && effect.IsSuspended)
                    {
                        effect.IsSuspended = false;
                        Activate(effect);
                    }
                    else if (!met && !effect.IsSuspended)
                    {
                        effect.IsSuspended = true;
                        Deactivate(effect);
                    }
                }
            }
            finally
            {
                ReturnSnapshot(buffer);
            }
        }

        // ---------- Executing ----------

        // Applies the modifiers of a periodic effect to the base value, like an instant effect.
        private void ExecutePeriodic(ActiveGameplayEffect effect)
        {
            AppliedModifier[] applied = effect.AppliedModifiers;
            int stacks = effect.Stacks;
            float cueMagnitude = 0f;

            for (int i = 0; i < applied.Length; i++)
            {
                // An event from a previous attribute may have removed or suspended this effect.
                if (!IsOn(effect))
                    return;

                AppliedModifier m = applied[i];
                
                //Saves the magnitude of the first modifier
                float magnitude = Scale(m.Operation, m.Magnitude, stacks);
                if (i == 0)
                    cueMagnitude = magnitude;
                
                m.Attribute.ApplyToBaseValue(m.Operation, Scale(m.Operation, m.Magnitude, stacks));
            }

            // Executions are calculated again in every period.
            if (IsOn(effect) && effect.Definition.HasExecutions)
                RunExecutions(effect.Definition, effect.Spec, stacks);
            
            if (IsOn(effect) && effect.Definition.HasCues)
                Cues?.ExecuteEach(effect.Definition.CueTags, effect.Instigator, cueMagnitude);
        }

        private void RunExecutions(GameplayEffectDefinition effect, in GameplayEffectSpec spec, int stacks)
        {
            var executions = effect.Executions;
            var context = new EffectExecutionContext(spec, attributes, ownedTags, stacks);

            ExecutionOutput output = RentOutput();
            try
            {
                for (int i = 0; i < executions.Count; i++)
                {
                    EffectExecution execution = executions[i];
                    if (execution == null)
                        continue;

                    output.Clear();
                    execution.Execute(context, output);
                    ApplyOutput(effect, output);
                }
            }
            finally
            {
                ReturnOutput(output);
            }
        }

        private void ApplyOutput(GameplayEffectDefinition effect, ExecutionOutput output)
        {
            List<ExecutionOutput.Entry> entries = output.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                ExecutionOutput.Entry entry = entries[i];

                if (attributes.TryGetAttribute(entry.Attribute, out GameplayAttribute attribute))
                {
                    attribute.ApplyToBaseValue(entry.Operation, entry.Magnitude);
                }
                else
                {
                    Debug.LogWarning(
                        $"ActiveEffectContainer: an execution of '{effect.name}' changes '{entry.Attribute.Name}', but this character doesn't have that attribute.");
                }
            }
        }

        // ---------- Removing ----------

        private void Remove(ActiveGameplayEffect effect)
        {
            if (!effect.IsActive)
                return;

            // First it leaves the list, then everything is undone,
            // so any event that fires sees the updated state.
            effect.IsActive = false;
            active.Remove(effect);

            if (effect.Definition.HasOngoingRequirements)
                ongoingCount--;

            Deactivate(effect);
        }

        // ---------- Pools ----------

        private ExecutionOutput RentOutput()
        {
            if (outputDepth == outputPool.Count)
                outputPool.Add(new ExecutionOutput());

            ExecutionOutput output = outputPool[outputDepth++];
            output.Clear();
            return output;
        }

        private void ReturnOutput(ExecutionOutput output)
        {
            output.Clear();
            outputDepth--;
        }

        private List<ActiveGameplayEffect> RentSnapshot()
        {
            if (snapshotDepth == snapshots.Count)
                snapshots.Add(new List<ActiveGameplayEffect>());

            List<ActiveGameplayEffect> buffer = snapshots[snapshotDepth++];
            buffer.Clear();
            buffer.AddRange(active);
            return buffer;
        }

        private void ReturnSnapshot(List<ActiveGameplayEffect> buffer)
        {
            buffer.Clear();
            snapshotDepth--;
        }

        private static void WarnMissingAttribute(GameplayEffectDefinition effect, EffectModifierDefinition modifier)
        {
            Debug.LogWarning(
                $"ActiveEffectContainer: '{effect.name}' modifies '{modifier.AttributeTag.Name}', but this character doesn't have that attribute.");
        }
    }
}