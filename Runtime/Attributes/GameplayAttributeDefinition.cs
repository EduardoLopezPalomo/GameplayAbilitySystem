using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>
    /// Configuration for a gameplay attribute. This is a ScriptableObject that can be created in the editor.
    /// It defines the base value, minimum value, and maximum value of an attribute, as well as the gameplay tag associated with it.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability System/Attribute/Attribute Definition", fileName = "NewAttribute")]
    public class GameplayAttributeDefinition : ScriptableObject
    {
        [SerializeField] private GameplayTag tag;
        [SerializeField] private float baseValue = 100f;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;
        
        [Tooltip("Optional. If set, the maximum of this attribute is the current value of that attribute (for example Health uses Attribute.MaxHealth) and 'Max Value' is ignored.")]
        [SerializeField] private GameplayTag maxAttributeTag;

        public GameplayTag Tag => tag;
        public float BaseValue => baseValue;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        
        public GameplayTag MaxAttributeTag => maxAttributeTag;

        private void OnValidate()
        {
            if (maxValue < minValue)
                maxValue = minValue;

            baseValue = Mathf.Clamp(baseValue, minValue, maxValue);
        }
    }
}
