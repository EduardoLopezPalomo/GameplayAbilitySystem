using System;
using UnityEngine;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// How an effect modifies one attribute: which one, with what operation, and how much.
    /// The amount can be fixed, based on an attribute, or sent by the caller.
    /// </summary>
    [Serializable]
    public struct EffectModifierDefinition
    {
        [SerializeField] private GameplayTag attributeTag;
        [SerializeField] private ModifierOperation operation;
        
        [Tooltip("Determines how the modifier magnitude is calculated.")]
        [SerializeField] private MagnitudeType magnitudeType;
        
        [Tooltip("The fixed value or flat amount used by the modifier, depending on the magnitude type.")]
        [SerializeField] private float magnitude;
        
        [Tooltip("Specifies whether the captured attribute comes from the effect's source or target.")]
        [SerializeField] private AttributeCaptureSource captureFrom;
        [Tooltip("The tag of the attribute used to calculate the modifier magnitude.")]
        [SerializeField] private GameplayTag capturedAttribute;
        [Tooltip("The coefficient applied to the captured attribute. The calculation is: Attribute * Coefficient + Magnitude.")]
        [SerializeField] private float coefficient;
        
        [Tooltip("The tag used to retrieve the caller-provided value for this modifier.")]
        [SerializeField] private GameplayTag setByCallerTag;

        public GameplayTag AttributeTag => attributeTag;
        public ModifierOperation Operation => operation;
        public MagnitudeType MagnitudeType => magnitudeType;
        public float Magnitude => magnitude;
        public AttributeCaptureSource CaptureFrom => captureFrom;
        public GameplayTag CapturedAttribute => capturedAttribute;
        public float Coefficient => coefficient;
        public GameplayTag SetByCallerTag => setByCallerTag;

        public EffectModifierDefinition(GameplayTag attribute, ModifierOperation operation, float magnitude)
        {
            attributeTag = attribute;
            this.operation = operation;
            this.magnitude = magnitude;
            magnitudeType = MagnitudeType.Fixed;
            captureFrom = AttributeCaptureSource.Instigator;
            capturedAttribute = default;
            coefficient = 1f;
            setByCallerTag = default;
        }
    }
}
