using supercachola.AbilitySystem.Attributes;

namespace supercachola.AbilitySystem.Effects
{
    //A modifier that has been applied to an attribute.
    internal struct AppliedModifier
    {
        public readonly GameplayAttribute Attribute;
        
        // None for periodic effects: they change the base value instead of adding a modifier.
        public ModifierHandle Handle;
        public readonly ModifierOperation Operation;
        public readonly float Magnitude;

        public AppliedModifier(GameplayAttribute attribute, ModifierHandle handle, ModifierOperation operation, float magnitude)
        {
            Attribute = attribute;
            Handle = handle;
            Operation = operation;
            Magnitude = magnitude;
        }
    }

    /// <summary>
    /// An effect with duration or infinite that has been applied to an attribute set.
    /// This class tracks the remaining time and applied modifiers for the effect.
    /// </summary>
    public sealed class ActiveGameplayEffect
    {
        internal readonly AppliedModifier[] AppliedModifiers;
        
        // The spec the effect was applied with. Periodic executions use it in every period.
        internal readonly GameplayEffectSpec Spec;
        
        //Seconds since the last period tick. Used for periodic effects.
        internal float PeriodTimer;
        internal int PendingExecutions;
        
        // What this effect has given to the character right now, so only that is taken back.
        internal bool TagsGranted;
        internal bool ImmunityGranted;
        internal bool CuesAdded;
        
        public ActiveEffectHandle Handle { get; }
        public GameplayEffectDefinition Definition { get; }
        
        /// <summary>Who applied the effect. Null if it was applied without an instigator.</summary>
        public AbilitySystemComponent Instigator => Spec.Instigator;
        
        /// <summary>False when the effect is remove.</summary>
        public bool IsActive { get; internal set; }
        
        /// <summary>True while its ongoing requirements are not met: it has no modifiers, tags or immunity.</summary>
        public bool IsSuspended { get; internal set; }

        /// <summary>Number of charges (1 unless the effect stacks).</summary>
        public int Stacks { get; internal set; } = 1;

        /// <summary>Seconds left or Infinite.</summary>
        public float RemainingTime { get; internal set; }

        internal ActiveGameplayEffect( ActiveEffectHandle handle, GameplayEffectDefinition definition, AppliedModifier[] appliedModifiers, in GameplayEffectSpec spec)
        {
            Handle = handle;
            Definition = definition;
            AppliedModifiers = appliedModifiers;
            Spec = spec;
            
            PeriodTimer = definition.IsPeriodic ? definition.Period : 0f;

            RemainingTime = definition.DurationPolicy == EffectDurationPolicy.Duration ? definition.Duration : float.PositiveInfinity;
        }
    }
}
