using System;
using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Events;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>
    /// The abilities of one character: the ones it has (granted) and the ones running (active).
    /// </summary>
    public sealed class AbilityContainer
    {
        private readonly AbilitySystemComponent owner;
        private readonly List<GameplayAbility> granted = new List<GameplayAbility>();
        private readonly List<GameplayAbility> active = new List<GameplayAbility>();

        // Copies of the active list to iterate safely: an ability can end, or activate another
        // in the middle of the loop. One copy per nesting level, reused to avoid garbage.
        private readonly List<List<GameplayAbility>> snapshots = new List<List<GameplayAbility>>();
        // The current nesting level of snapshots. It is incremented when a snapshot is rented and decremented when it is returned.
        private int snapshotDepth;
        
        // If abilities keep triggering each other through events, this stops it before a stack overflow.
        private const int MaxEventDepth = 16;
        private int eventDepth;

        public int GrantedCount => granted.Count;
        public int ActiveCount => active.Count;

        public event Action<GameplayAbility> AbilityActivated;

        /// <summary>The second parameter is true if the ability was cancelled.</summary>
        public event Action<GameplayAbility, bool> AbilityEnded;

        public AbilityContainer(AbilitySystemComponent owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public GameplayAbility GetGrantedAt(int index) => granted[index];

        // ---------- Granting ----------

        /// <summary>Gives the character an ability. Returns null if it could not be granted.</summary>
        public GameplayAbility Grant(GameplayAbilityDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("AbilityContainer: tried to grant an empty ability.");
                return null;
            }

            if (!definition.AbilityTag.isValid)
            {
                Debug.LogWarning($"AbilityContainer: '{definition.name}' has no tag.", definition);
                return null;
            }

            if (TryGetAbility(definition.AbilityTag, out _))
            {
                Debug.LogWarning($"AbilityContainer: '{definition.AbilityTag.Name}' is already granted.");
                return null;
            }

            GameplayAbility ability = definition.CreateAbility();
            if (ability == null)
            {
                Debug.LogWarning($"AbilityContainer: '{definition.name}' returned no ability from CreateAbility.", definition);
                return null;
            }

            ability.Initialize(owner, definition);
            ability.Ended += OnAbilityEnded;
            granted.Add(ability);
            return ability;
        }

        /// <summary>Takes an ability away from the character, cancelling it if it is running.</summary>
        public bool Revoke(GameplayTag abilityTag)
        {
            if (!TryGetAbility(abilityTag, out GameplayAbility ability))
                return false;

            ability.Cancel();
            ability.Ended -= OnAbilityEnded;
            granted.Remove(ability);
            return true;
        }

        public bool TryGetAbility(GameplayTag abilityTag, out GameplayAbility ability)
        {
            for (int i = 0; i < granted.Count; i++)
            {
                if (granted[i].Tag == abilityTag)
                {
                    ability = granted[i];
                    return true;
                }
            }

            ability = null;
            return false;
        }

        public bool IsAbilityActive(GameplayTag abilityTag)
        {
            return TryGetAbility(abilityTag, out GameplayAbility ability) && ability.IsActive;
        }

        // ---------- Activating and cancelling ----------

        public bool TryActivate(GameplayTag abilityTag)
        {
            if (!TryGetAbility(abilityTag, out GameplayAbility ability))
            {
                Debug.LogWarning($"AbilityContainer: the character doesn't have '{abilityTag.Name}'.");
                return false;
            }

            return TryActivate(ability);
        }

        public bool TryActivate(GameplayAbility ability)
        {
            if (ability == null || !granted.Contains(ability))
                return false;

            // Already running: only combo-style abilities accept to be activated again,
            // and they have already paid, so the normal requirements are skipped.
            if (ability.IsActive)
                return ability.Definition.AllowReactivationWhileActive && ability.Reactivate();

            if (!ability.CanActivate())
                return false;

            CancelAbilitiesWithTags(ability.Definition.CancelAbilitiesWithTags, ability);

            active.Add(ability);
            ability.Begin();

            // Notified before running, so an ability that ends instantly still produces
            // "activated" first and "ended" after.
            AbilityActivated?.Invoke(ability);

            ability.Run();
            return true;
        }

        public bool CancelAbility(GameplayTag abilityTag)
        {
            if (!TryGetAbility(abilityTag, out GameplayAbility ability) || !ability.IsActive)
                return false;

            ability.Cancel();
            return true;
        }

        /// <summary>Cancels the active abilities whose tag matches any of the given tags.</summary>
        public void CancelAbilitiesWithTags(GameplayTagContainer tags, GameplayAbility except = null)
        {
            if (tags == null || tags.IsEmpty || active.Count == 0)
                return;

            List<GameplayAbility> buffer = RentSnapshot();
            try
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    GameplayAbility ability = buffer[i];
                    if (ability != except && ability.IsActive && ability.IdentityTags.HasAny(tags))
                        ability.Cancel();
                }
            }
            finally
            {
                ReturnSnapshot(buffer);
            }
        }

        public void CancelAll()
        {
            if (active.Count == 0)
                return;

            List<GameplayAbility> buffer = RentSnapshot();
            try
            {
                for (int i = 0; i < buffer.Count; i++)
                    buffer[i].Cancel();
            }
            finally
            {
                ReturnSnapshot(buffer);
            }
        }

        // ---------- Time and events ----------

        /// <summary>Calls OnTick on the active abilities. Abilities that are not running cost nothing.</summary>
        public void Tick(float deltaTime)
        {
            if (active.Count == 0)
                return;

            List<GameplayAbility> buffer = RentSnapshot();
            try
            {
                for (int i = 0; i < buffer.Count; i++)
                    buffer[i].Tick(deltaTime);
            }
            finally
            {
                ReturnSnapshot(buffer);
            }
        }
        
        /// <summary>Sends an event to the active abilities and starts the abilities that trigger on it.</summary>
        public void SendGameplayEvent(GameplayEventData data)
        {
            if (!data.Tag.isValid)
                return;

            if (eventDepth >= MaxEventDepth)
            {
                Debug.LogWarning(
                    $"AbilityContainer: event '{data.Tag.Name}' ignored. Events are triggering each other too deep ({MaxEventDepth}). Check the Trigger Event Tags of your abilities.");
                return;
            }

            eventDepth++;
            try
            {
                // 1. The active abilities: their waits and OnGameplayEvent.
                if (active.Count > 0)
                {
                    List<GameplayAbility> buffer = RentSnapshot();
                    try
                    {
                        for (int i = 0; i < buffer.Count; i++)
                            buffer[i].HandleGameplayEvent(data);
                    }
                    finally
                    {
                        ReturnSnapshot(buffer);
                    }
                }

                // 2. The abilities that start with this event.
                TriggerAbilities(data);
            }
            finally
            {
                eventDepth--;
            }
        }

        private void TriggerAbilities(GameplayEventData data)
        {
            // An activation can grant or revoke abilities, so the count is checked on every step.
            for (int i = 0; i < granted.Count; i++)
            {
                GameplayAbility ability = granted[i];

                if (ability.IsActive || !ability.Definition.TriggerEventTags.Covers(data.Tag))
                    continue;

                // Set before activating, so CanActivateCustom and OnActivate can read it.
                ability.SetTrigger(data);

                if (!TryActivate(ability))
                    ability.ClearTrigger();
            }
        }

        // ---------- Internal ----------

        private void OnAbilityEnded(GameplayAbility ability, bool wasCancelled)
        {
            active.Remove(ability);
            AbilityEnded?.Invoke(ability, wasCancelled);
        }

        private List<GameplayAbility> RentSnapshot()
        {
            if (snapshotDepth == snapshots.Count)
                snapshots.Add(new List<GameplayAbility>());

            List<GameplayAbility> buffer = snapshots[snapshotDepth++];
            buffer.Clear();
            buffer.AddRange(active);
            return buffer;
        }

        private void ReturnSnapshot(List<GameplayAbility> buffer)
        {
            buffer.Clear();
            snapshotDepth--;
        }
    }
}
