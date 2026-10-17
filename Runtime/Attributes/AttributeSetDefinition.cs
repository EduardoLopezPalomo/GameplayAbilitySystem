using System.Collections.Generic;
using UnityEngine;

namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>
    /// A set of attributes that a character can have. Each attribute has a definition and a current value.
    /// Is just a configuration, the actual values are stored in the AttributeSet class.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability System/Attribute/Attribute Set", fileName = "NewAttributeSet")]
    public class AttributeSetDefinition : ScriptableObject
    {
        [SerializeField] private List<GameplayAttributeDefinition> attributes = new List<GameplayAttributeDefinition>();

        public IReadOnlyList<GameplayAttributeDefinition> Attributes => attributes;

        //When a new attribute is added, check that it does not have the same tag as any other attribute in the list. If it does, log a warning.
        private void OnValidate()
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                var attribute1 = attributes[i];
                if (attribute1 == null) continue;

                for (int j = i + 1; j < attributes.Count; j++)
                {
                    var attribute2 = attributes[j];
                    if (attribute2 == null) continue;

                    if (attribute1 == attribute2 || (attribute1.Tag.isValid && attribute1.Tag == attribute2.Tag))
                    {
                        Debug.LogWarning(
                            $"'{name}': '{attribute1.name}' and '{attribute2.name}' use the same tag.", this);
                    }
                }
            }
        }
    }
}
