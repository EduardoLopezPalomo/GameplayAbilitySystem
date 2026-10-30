using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Attributes;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// What an execution can read: the spec, and the attributes and tags of the instigator and the target.
    /// Without an instigator (or if it was destroyed), the instigator values are the target's.
    /// </summary>
    public readonly struct EffectExecutionContext
    {
        public GameplayEffectSpec Spec { get; }
        public AbilitySystemComponent Target { get; }
        public AttributeSet TargetAttributes { get; }
        public GameplayTagCountContainer TargetTags { get; }
        public AttributeSet InstigatorAttributes { get; }
        public GameplayTagCountContainer InstigatorTags { get; }
        
        public int StackCount { get; }

        internal EffectExecutionContext(in GameplayEffectSpec spec, AbilitySystemComponent target, AttributeSet targetAttributes, GameplayTagCountContainer targetTags,  int stackCount)
        {
            Spec = spec;
            Target = target;
            TargetAttributes = targetAttributes;
            TargetTags = targetTags;
            StackCount = stackCount;

            AbilitySystemComponent instigator = spec.Instigator;
            if (instigator != null)
            {
                InstigatorAttributes = instigator.Attributes;
                InstigatorTags = instigator.OwnedTags;
            }
            else
            {
                InstigatorAttributes = targetAttributes;
                InstigatorTags = targetTags;
            }
        }

        /// <summary>Current value of an attribute of the instigator. False (and 0) if it doesn't have it.</summary>
        public bool TryGetInstigatorValue(GameplayTag attribute, out float value)
        {
            if (InstigatorAttributes.TryGetAttribute(attribute, out GameplayAttribute found))
            {
                value = found.currentValue;
                return true;
            }

            value = 0f;
            return false;
        }

        /// <summary>Current value of an attribute of the target. False (and 0) if it doesn't have it.</summary>
        public bool TryGetTargetValue(GameplayTag attribute, out float value)
        {
            if (TargetAttributes.TryGetAttribute(attribute, out GameplayAttribute found))
            {
                value = found.currentValue;
                return true;
            }

            value = 0f;
            return false;
        }

        /// <summary>Plays a cue once on the character that receives the effect. Does nothing if there is no target.</summary>
        public void ExecuteCue(GameplayTag cue, float magnitude = 0f)
        {
            if (Target != null)
                Target.ExecuteCue(cue, magnitude);
        }
    }
}