using System;
using UnityEngine;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Attributes
{
    public enum AttributeComparison
    {
        LessThan,
        LessOrEqual,
        GreaterOrEqual,
        GreaterThan
    }

    public enum ThresholdMode
    {
        Absolute,
        /// <summary>The threshold is a fraction of the maximum: 0.25 = 25%. It needs a finite maximum.</summary>
        PercentOfMax
    }

    /// <summary>
    /// "When this attribute meets a condition, do this." It reacts when the condition starts
    /// and when it stops being met, not on every change.
    /// </summary>
    [Serializable]
    public struct AttributeReaction
    {
        [SerializeField] private GameplayTag attribute;
        [SerializeField] private AttributeComparison condition;
        [SerializeField] private ThresholdMode thresholdMode;

        [Tooltip("Absolute: a value (0 for death). Percent Of Max: a fraction (0.25 = 25%).")]
        [SerializeField] private float threshold;

        [Tooltip("Applied when the condition starts and removed when it stops. Use an Infinite effect that grants a tag (State.Dead).")]
        [SerializeField] private GameplayEffectDefinition effectWhileMet;

        [Tooltip("Event sent when the condition starts (Event.Death).")]
        [SerializeField] private GameplayTag eventOnEnter;

        [Tooltip("Event sent when the condition stops (Event.Revived).")]
        [SerializeField] private GameplayTag eventOnExit;

        public GameplayTag Attribute => attribute;
        public GameplayEffectDefinition EffectWhileMet => effectWhileMet;
        public GameplayTag EventOnEnter => eventOnEnter;
        public GameplayTag EventOnExit => eventOnExit;

        internal bool IsMet(GameplayAttribute target)
        {
            float limit = thresholdMode == ThresholdMode.PercentOfMax
                ? threshold * target.MaxValue
                : threshold;

            switch (condition)
            {
                case AttributeComparison.LessThan:
                    return target.currentValue < limit;
                case AttributeComparison.LessOrEqual:
                    return target.currentValue <= limit;
                case AttributeComparison.GreaterOrEqual:
                    return target.currentValue >= limit;
                case AttributeComparison.GreaterThan:
                    return target.currentValue > limit;
            }
            
            return false;
        }
    }
}
