using System;
using supercachola.AbilitySystem.Events;
using supercachola.AbilitySystem.Tags;
using UnityEngine;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>
    /// Runtime instance of an ability, one per character.
    /// Inherit from it and override the On... methods.
    /// </summary>
    public abstract class GameplayAbility
    {
        private AbilityWaits waits;
        
        public GameplayAbilityDefinition Definition { get; private set; }
        public AbilitySystemComponent Owner { get; private set; }
        public GameplayTag Tag => Definition.AbilityTag;
        public bool IsActive { get; private set; }

        public bool IsOnCooldown => Owner.OwnedTags.HasAny(Definition.CooldownTags);
        public float CooldownRemaining => Owner.Effects.GetRemainingTime(Definition.CooldownTags);
        
        /// <summary>True if the ability was started by an event (see Trigger Event Tags).</summary>
        public bool WasTriggeredByEvent { get; private set; }

        /// <summary>The event that started the ability. Only valid if WasTriggeredByEvent.</summary>
        public GameplayEventData TriggerEvent { get; private set; }

        // The ability's own tag (with its parents), used to match "cancel abilities with tags".
        internal GameplayTagContainer IdentityTags { get; private set; }

        //First parameter: the ability that has to end
        //Second parameter: true if the ability was cancelled instead of finishing.
        internal event Action<GameplayAbility, bool> Ended;

        /// <summary>True if the ability could be activated right now.</summary>
        public bool CanActivate()
        {
            if (IsActive)
                return false;

            GameplayTagCountContainer tags = Owner.OwnedTags;

            if (!tags.HasAll(Definition.ActivationRequiredTags)) return false;
            if (tags.HasAny(Definition.ActivationBlockedTags)) return false;
            if (IsOnCooldown) return false;
            if (!Owner.Effects.CanAfford(Definition.CostEffect)) return false;

            return CanActivateCustom();
        }

        // ---------- To override ----------

        /// <summary>Called once, when the ability is granted. Cache your components here.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>Extra requirements of this specific ability.</summary>
        protected virtual bool CanActivateCustom() => true;

        /// <summary>The ability starts. Call End() when it finishes.</summary>
        protected abstract void OnActivate();

        /// <summary>
        /// Activated again while active (needs Allow Reactivation While Active).
        /// Return true if it was handled.
        /// </summary>
        protected virtual bool OnReactivate() => false;

        /// <summary>Every frame, only while the ability is active.</summary>
        protected virtual void OnTick(float deltaTime) { }
        
        /// <summary>
        /// A gameplay event arrived while the ability is active. It is called for every event,
        /// whatever its tag. To wait for one specific event, use WaitForEvent.
        /// </summary>
        protected virtual void OnGameplayEvent(GameplayEventData data) { }

        /// <summary>The ability ended or was cancelled. Always runs exactly once. Clean up here.</summary>
        protected virtual void OnEnd(bool wasCancelled) { }

        // ---------- To call from inside the ability ----------

        /// <summary>Pays the cost and starts the cooldown. Returns false if the cost can't be paid.</summary>
        protected bool Commit()
        {
            if (!Owner.Effects.CanAfford(Definition.CostEffect))
                return false;

            if (Definition.CostEffect != null)
                Owner.ApplyEffect(Definition.CostEffect);

            if (Definition.CooldownEffect != null)
                Owner.ApplyEffect(Definition.CooldownEffect);

            return true;
        }

        /// <summary>Applies the "Effects To Self" list of the definition to the owner.</summary>
        protected void ApplyEffectsToSelf()
        {
            var effects = Definition.EffectsToSelf;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                    Owner.ApplyEffect(effects[i]);
            }
        }

        /// <summary>The ability has finished normally.</summary>
        protected void End()
        {
            Finish(false);
        }
        
        // ---------- Waiting (cleared automatically when the ability ends) ----------
        // Call them from OnActivate or later. A wait is only valid while the ability is active.

        /// <summary>
        /// Calls "callback" when an event arrives whose tag is "eventTag" or a child of it.
        /// With once = true (default) it only fires the first time.
        /// </summary>
        protected void WaitForEvent(GameplayTag eventTag, Action<GameplayEventData> callback, bool once = true)
        {
            if (!eventTag.isValid)
            {
                Debug.LogWarning($"{GetType().Name}.WaitForEvent: the event tag is empty.");
                return;
            }

            GetWaits("WaitForEvent", callback)?.AddEvent(eventTag, callback, once);
        }

        /// <summary>Calls "callback" after some seconds (game time). It doesn't fire if the ability ends first.</summary>
        protected void WaitForDelay(float seconds, Action callback)
        {
            GetWaits("WaitForDelay", callback)?.AddDelay(seconds, callback);
        }

        /// <summary>
        /// Calls "callback" when the owner gets the tag (toBePresent = true) or loses it (false).
        /// If it is already in that state, it waits for the next change.
        /// </summary>
        protected void WaitForTag(GameplayTag tag, bool toBePresent, Action callback, bool once = true)
        {
            if (!tag.isValid)
            {
                Debug.LogWarning($"{GetType().Name}.WaitForTag: the tag is empty.");
                return;
            }

            GetWaits("WaitForTag", callback)?.AddTag(tag, toBePresent, callback, once);
        }

        // ---------- Called by the AbilityContainer ----------

        internal void Initialize(AbilitySystemComponent owner, GameplayAbilityDefinition definition)
        {
            Owner = owner;
            Definition = definition;

            IdentityTags = new GameplayTagContainer();
            IdentityTags.AddTag(definition.AbilityTag);

            OnInitialize();
        }

        internal void Begin()
        {
            IsActive = true;
            Owner.OwnedTags.AddTags(Definition.ActivationOwnedTags);
        }

        internal void Run()
        {
            if (Definition.CommitOnActivate)
                Commit();

            // Paying the cost may have triggered events that cancelled us.
            if (IsActive)
                OnActivate();
        }

        internal bool Reactivate() => IsActive && OnReactivate();

        internal void Tick(float deltaTime)
        {
            if (!IsActive)
                return;

            waits?.Tick(deltaTime);

            if (IsActive)
                OnTick(deltaTime);
        }
        
        internal void HandleGameplayEvent(GameplayEventData data)
        {
            if (!IsActive)
                return;

            waits?.DispatchEvent(data);

            if (IsActive)
                OnGameplayEvent(data);
        }

        internal void SetTrigger(GameplayEventData data)
        {
            TriggerEvent = data;
            WasTriggeredByEvent = true;
        }

        internal void ClearTrigger()
        {
            TriggerEvent = default;
            WasTriggeredByEvent = false;
        }


        internal void Cancel()
        {
            Finish(true);
        }
        
        //---------- End ability ----------
        private void Finish(bool cancelled)
        {
            if (!IsActive)
                return;

            IsActive = false;
            Owner.OwnedTags.RemoveTags(Definition.ActivationOwnedTags);
            
            waits?.Clear();

            OnEnd(cancelled);
            Ended?.Invoke(this, cancelled);
            
            // An event handler could have started the ability again: don't wipe its trigger.
            if (!IsActive)
                ClearTrigger();
        }
        
        private AbilityWaits GetWaits(string method, Delegate callback)
        {
            if (callback == null)
            {
                Debug.LogWarning($"{GetType().Name}.{method}: the callback is empty.");
                return null;
            }

            if (!IsActive)
            {
                Debug.LogWarning($"{GetType().Name}.{method}: the ability is not active, so the wait is ignored. Call it from OnActivate or later.");
                return null;
            }

            return waits ?? (waits = new AbilityWaits(this));
        }
    }
}
