using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// Configuration for a gameplay effect. This ScriptableObject defines the duration policy, duration, and modifiers that will be applied when the effect is executed.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability System/Gameplay Effect", fileName = "NewEffect")]
    public class GameplayEffectDefinition : ScriptableObject
    {
        [SerializeField] private EffectDurationPolicy durationPolicy = EffectDurationPolicy.Instant;

        [Tooltip("Seconds. Only used if DurationPolicy is set to Duration.")]
        [Min(0.01f)]
        [SerializeField] private float duration = 1f;
        
        [Header("Periodic")]
        [Tooltip("How many seconds between each execution of the effect. 0 = not periodic. Not used with Instant.")]
        [Min(0f)]
        [SerializeField] private float period = 0f;

        [Tooltip("If true, the effect will be executed when the application starts.")]
        [SerializeField] private bool executeOnApplication = false;
        
        [Header("Modifiers")]
        [SerializeField] private List<EffectModifierDefinition> modifiers = new List<EffectModifierDefinition>();
        
        [Header("Executions")]
        [Tooltip("Custom calculations. They only run in Instant and Periodic effects.")]
        [SerializeField] private List<EffectExecution> executions = new List<EffectExecution>();
        
        [Header("Stacking (Duration and Infinite effects)")]
        [Tooltip("What happens when the effect is applied again while it is already active.")]
        [SerializeField] private EffectStackingPolicy stackingPolicy = EffectStackingPolicy.Independent;

        [Tooltip("Target: one stack per character. Instigator: one stack per character that applies it.")]
        [SerializeField] private EffectStackScope stackScope = EffectStackScope.Target;

        [Tooltip("Stack Charges only: maximum number of charges.")]
        [Min(1)]
        [SerializeField] private int maxStacks = 5;

        [Tooltip("Stack Charges only: a new charge also restarts the duration.")]
        [SerializeField] private bool refreshDurationOnStack = true;
        
        [Header("Tags")]
        [Tooltip("Tags that describe this effect. The character does NOT get them. " +
                 "Other effects use them to find this one (cleanse, immunity).")]
        [SerializeField] private GameplayTagContainer effectTags = new GameplayTagContainer();
        
        [Tooltip("The character has this tags when the effect is active. Is not use with instant.")]
        [SerializeField] private GameplayTagContainer grantedTags = new GameplayTagContainer();

        [Tooltip("The effect can only be applied if the character has all of these tags.")]
        [SerializeField] private GameplayTagContainer applicationRequiredTags = new GameplayTagContainer();

        [Tooltip("The effect is blocked if the character has any of these tags.")]
        [SerializeField] private GameplayTagContainer applicationBlockedTags = new GameplayTagContainer();
        
        [Tooltip("When this effect is applied, the active effects whose Effect Tags or Granted Tags " +
                 "match any of these are removed first (a cleanse).")]
        [SerializeField] private GameplayTagContainer removeEffectsWithTags = new GameplayTagContainer();

        [Tooltip("While this effect is active, the character ignores the effects whose Effect Tags " +
                 "or Granted Tags match any of these. Not used with Instant.")]
        [SerializeField] private GameplayTagContainer grantedImmunityTags = new GameplayTagContainer();

        [Header("Ongoing requirements")]
        [Tooltip("The effect is suspended while the character lacks ANY of these tags. Not used with Instant.")]
        [SerializeField] private GameplayTagContainer ongoingRequiredTags = new GameplayTagContainer();

        [Tooltip("The effect is suspended while the character has ANY of these tags. Not used with Instant.")]
        [SerializeField] private GameplayTagContainer ongoingBlockedTags = new GameplayTagContainer();
        
        [Header("Cues")]
        [Tooltip("Cues played by this effect. Instant: when applied. Periodic: in each period. Duration/Infinite: while it is active.")]
        [SerializeField] private List<GameplayTag> cueTags = new List<GameplayTag>();


        public EffectDurationPolicy DurationPolicy => durationPolicy;
        public float Duration => duration;
        public float Period => period;
        public bool ExecuteOnApplication => executeOnApplication;
        public bool IsPeriodic => period > 0f && durationPolicy != EffectDurationPolicy.Instant;

        public IReadOnlyList<EffectModifierDefinition> Modifiers => modifiers;
        public IReadOnlyList<EffectExecution> Executions => executions;
        public bool HasExecutions => executions.Count > 0;
        
        public EffectStackingPolicy StackingPolicy => stackingPolicy;
        public EffectStackScope StackScope => stackScope;
        public int MaxStacks => maxStacks;
        public bool RefreshDurationOnStack => refreshDurationOnStack;
        
        public GameplayTagContainer EffectTags => effectTags;
        public GameplayTagContainer GrantedTags => grantedTags;
        public GameplayTagContainer ApplicationRequiredTags => applicationRequiredTags;
        public GameplayTagContainer ApplicationBlockedTags => applicationBlockedTags;
        public GameplayTagContainer RemoveEffectsWithTags => removeEffectsWithTags;
        public GameplayTagContainer GrantedImmunityTags => grantedImmunityTags;
        public GameplayTagContainer OngoingRequiredTags => ongoingRequiredTags;
        public GameplayTagContainer OngoingBlockedTags => ongoingBlockedTags;
        
        public bool HasRemoveEffectsWithTags => !removeEffectsWithTags.IsEmpty;
        public bool HasOngoingRequirements => !ongoingRequiredTags.IsEmpty || !ongoingBlockedTags.IsEmpty;
        
        public IReadOnlyList<GameplayTag> CueTags => cueTags;
        
        
        internal bool HasCues => cueTags.Count > 0;
        
        // True if this effect is identified by any of these tags (by its Effect Tags or its Granted Tags).
        internal bool MatchesAny(GameplayTagContainer tags)
        {
            return effectTags.HasAny(tags) || grantedTags.HasAny(tags);
        }

        // True if the immunity tags of "covering" cover this effect.
        internal bool IsCoveredBy(GameplayTagCountContainer covering)
        {
            return covering.CoversAny(effectTags) || covering.CoversAny(grantedTags);
        }
        
        private void OnEnable() => FreezeContainers();

        private void FreezeContainers()
        {
            grantedTags?.Freeze();
            applicationRequiredTags?.Freeze();
            applicationBlockedTags?.Freeze();
            effectTags?.Freeze();
            removeEffectsWithTags?.Freeze();
            grantedImmunityTags?.Freeze();
            ongoingRequiredTags?.Freeze();
            ongoingBlockedTags?.Freeze();
        }
        
        private void OnValidate()
        {
            FreezeContainers();
            
            if (period > 0f && period < 0.01f)
                period = 0.01f;

            if (durationPolicy == EffectDurationPolicy.Instant)
            {
                if (period > 0f)
                    Debug.LogWarning($"'{name}': an instant effect ignores period", this);

                if (grantedTags != null && !grantedTags.IsEmpty)
                    Debug.LogWarning($"'{name}':an instant effect ignores granted tags.", this);
                
                if (stackingPolicy != EffectStackingPolicy.Independent)
                    Debug.LogWarning($"'{name}': an instant effect ignores stacking.", this);

                if (!grantedImmunityTags.IsEmpty)
                    Debug.LogWarning($"'{name}': an instant effect ignores Granted Immunity Tags.", this);

                if (HasOngoingRequirements)
                    Debug.LogWarning($"'{name}': an instant effect ignores ongoing requirements.", this);
            }
            else if (stackingPolicy == EffectStackingPolicy.RefreshDuration && durationPolicy != EffectDurationPolicy.Duration)
            {
                Debug.LogWarning($"'{name}': Refresh Duration does nothing in an Infinite effect.", this);
            }
            
            if (executions.Count > 0 && durationPolicy != EffectDurationPolicy.Instant && period <= 0f)
                Debug.LogWarning($"'{name}': executions only run in Instant or Periodic effects.", this);
            
            if (!ongoingBlockedTags.IsEmpty && grantedTags != null && grantedTags.HasAny(ongoingBlockedTags))
                Debug.LogWarning($"'{name}': it grants a tag that blocks itself, so it would switch on and off.", this);
            
            for (int i = 0; i < modifiers.Count; i++)
            {
                EffectModifierDefinition m = modifiers[i];

                if (!m.AttributeTag.isValid)
                    Debug.LogWarning($"'{name}': modifier {i} has no attribute.", this);

                if (m.MagnitudeType == MagnitudeType.AttributeBased && !m.CapturedAttribute.isValid)
                    Debug.LogWarning($"'{name}': modifier {i} is attribute-based but has no attribute to read.", this);

                if (m.MagnitudeType == MagnitudeType.SetByCaller && !m.SetByCallerTag.isValid)
                    Debug.LogWarning($"'{name}': modifier {i} is set-by-caller but has no tag.", this);
            }
        }

    }
}
