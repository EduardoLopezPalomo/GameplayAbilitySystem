using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>
    /// Configuration of an ability. Shared by every character that has it.
    /// It never stores runtime state: each character gets its own GameplayAbility.
    /// </summary>
    public abstract class GameplayAbilityDefinition : ScriptableObject
    {
        private static readonly GameplayTagContainer NoTags = new GameplayTagContainer();
        
        [Header("Identity")]
        [Tooltip("Unique tag of the ability, for example Ability.Fire.Fireball.")]
        [SerializeField] private GameplayTag abilityTag;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;

        [Header("Cost and cooldown")]
        [Tooltip("Instant effect applied on commit, for example Mana -20.")]
        [SerializeField] private GameplayEffectDefinition costEffect;

        [Tooltip("Duration effect that grants a tag while the ability is cooling down.")]
        [SerializeField] private GameplayEffectDefinition cooldownEffect;

        [Tooltip("If true, cost and cooldown are paid when the ability activates. " +
                 "If false, the ability calls Commit() when it wants.")]
        [SerializeField] private bool commitOnActivate = true;

        [Header("Tags")]
        [Tooltip("The character has these tags while the ability is active.")]
        [SerializeField] private GameplayTagContainer activationOwnedTags = new GameplayTagContainer();

        [Tooltip("The character needs ALL of these tags to activate the ability.")]
        [SerializeField] private GameplayTagContainer activationRequiredTags = new GameplayTagContainer();

        [Tooltip("The ability can't activate if the character has ANY of these tags.")]
        [SerializeField] private GameplayTagContainer activationBlockedTags = new GameplayTagContainer();

        [Tooltip("When this ability activates, it cancels the active abilities whose tag matches any of these.")]
        [SerializeField] private GameplayTagContainer cancelAbilitiesWithTags = new GameplayTagContainer();
        
        [Tooltip("When a gameplay event matching any of these tags arrives, the ability tries to activate. " +
                 "The event is available inside the ability as TriggerEvent.")]
        [SerializeField] private GameplayTagContainer triggerEventTags = new GameplayTagContainer();

        [Header("Behaviour")]
        [Tooltip("If true, activating the ability while it is active calls OnReactivate (combos).")]
        [SerializeField] private bool allowReactivationWhileActive = false;

        [Tooltip("Effects the ability can apply to its owner (see ApplyEffectsToSelf).")]
        [SerializeField] private List<GameplayEffectDefinition> effectsToSelf = new List<GameplayEffectDefinition>();
        
        public GameplayTag AbilityTag => abilityTag;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public string Description => description;

        public GameplayEffectDefinition CostEffect => costEffect;
        public GameplayEffectDefinition CooldownEffect => cooldownEffect;
        public bool CommitOnActivate => commitOnActivate;
        public bool AllowReactivationWhileActive => allowReactivationWhileActive;

        public GameplayTagContainer ActivationOwnedTags => activationOwnedTags;
        public GameplayTagContainer ActivationRequiredTags => activationRequiredTags;
        public GameplayTagContainer ActivationBlockedTags => activationBlockedTags;
        public GameplayTagContainer CancelAbilitiesWithTags => cancelAbilitiesWithTags;
        public GameplayTagContainer TriggerEventTags => triggerEventTags;
        public IReadOnlyList<GameplayEffectDefinition> EffectsToSelf => effectsToSelf;

        // The tags that mean "this ability is cooling down".
        internal GameplayTagContainer CooldownTags =>
            cooldownEffect != null ? cooldownEffect.GrantedTags : NoTags;

        /// <summary>Creates the runtime instance for one character.</summary>
        public abstract GameplayAbility CreateAbility();
        
        private void OnEnable() => FreezeContainers();

        private void FreezeContainers()
        {
            activationOwnedTags?.Freeze();
            activationRequiredTags?.Freeze();
            activationBlockedTags?.Freeze();
            cancelAbilitiesWithTags?.Freeze();
            triggerEventTags?.Freeze();
        }
        
        protected virtual void OnValidate()
        {
            FreezeContainers();
            
            if (!abilityTag.isValid)
                Debug.LogWarning($"'{name}': the ability has no tag.", this);

            if (costEffect != null && costEffect.DurationPolicy != EffectDurationPolicy.Instant)
                Debug.LogWarning($"'{name}': the cost effect should be Instant.", this);

            if (cooldownEffect != null &&
                (cooldownEffect.DurationPolicy != EffectDurationPolicy.Duration || cooldownEffect.GrantedTags.IsEmpty))
                Debug.LogWarning($"'{name}': the cooldown effect should be Duration and grant a tag.", this);
        }
    }
}
