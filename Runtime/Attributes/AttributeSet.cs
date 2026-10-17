using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>
    /// The attributes of a character. This is a collection of GameplayAttribute objects, each of which has a current value and a definition.
    /// </summary>
    public sealed class AttributeSet
    {
        /// <summary>
        /// An array is use instead of a dictionary because the number is small, it is more efficient to iterate over an array than to use a dictionary.
        /// if the number of attributes is large, a dictionary should be used instead.
        /// </summary>
        private readonly GameplayAttribute[] _attributes;

        public int count => _attributes.Length;

        public GameplayAttribute this[int index] => _attributes[index];
        
        public AttributeSet(AttributeSetDefinition set) : this(new[] { set })
        {
        }

        public AttributeSet(IReadOnlyList<AttributeSetDefinition> sets)
        {
            var list = new List<GameplayAttribute>();

            for (int s = 0; s < sets.Count; s++)
            {
                var set = sets[s];

                if (set == null)
                {
                    Debug.LogWarning("AttributeSet: there is a empty set.");
                    continue;
                }

                var definitions = set.Attributes;
                for (int i = 0; i < definitions.Count; i++)
                    TryAdd(list, definitions[i], set.name);
            }

            _attributes = list.ToArray();
            
            //Connect each attribute to its maximum attribute.
            LinkMaxAttributes();
        }

        public bool TryGetAttribute(GameplayTag tag, out GameplayAttribute attribute)
        {
            for (int i = 0; i < _attributes.Length; i++)
            {
                if (_attributes[i].Tag == tag)
                {
                    attribute = _attributes[i];
                    return true;
                }
            }

            attribute = null;
            return false;
        }

        /// <summary>
        /// Actual value of the attribute with the given tag.
        /// If the attribute does not exist, returns 0 and logs a warning.
        /// </summary>
        public float GetCurrentValue(GameplayTag tag)
        {
            if (TryGetAttribute(tag, out var attribute))
                return attribute.currentValue;

            Debug.LogWarning($"AttributeSet: '{tag.Name}' doesn't exist.");
            return 0f;
        }

        private static void TryAdd(List<GameplayAttribute> list, GameplayAttributeDefinition definition, string setName)
        {
            if (definition == null)
            {
                Debug.LogWarning($"AttributeSet: there is a empty definition in '{setName}'.");
                return;
            }

            if (!definition.Tag.isValid)
            {
                Debug.LogWarning($"AttributeSet: '{definition.name}' doesn't have a tag.");
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Tag == definition.Tag)
                {
                    Debug.LogWarning(
                        $"AttributeSet: tag '{definition.Tag.Name}' is duplicated ('{definition.name}' in '{setName}'). It's ignore.");
                    return;
                }
            }

            list.Add(new GameplayAttribute(definition));
        }
        
        // Connects each attribute to the attribute that limits it.
        private void LinkMaxAttributes()
        {
            for (int i = 0; i < count; i++)
            {
                GameplayAttribute attribute = this[i];
                GameplayTag maxTag = attribute.Definition.MaxAttributeTag;

                if (!maxTag.isValid)
                    continue;

                if (!TryGetAttribute(maxTag, out GameplayAttribute source))
                {
                    Debug.LogWarning($"AttributeSet: '{attribute.Tag.Name}' uses '{maxTag.Name}' as its maximum, but the set doesn't have it.");
                    continue;
                }

                if (WouldLoop(attribute, source))
                {
                    Debug.LogWarning($"AttributeSet: '{attribute.Tag.Name}' and '{maxTag.Name}' limit each other. The link is ignored.");
                    continue;
                }

                attribute.LinkMax(source);
            }
        }

        // Follows the chain of maximums. If it comes back to the attribute, it would be a loop.
        private bool WouldLoop(GameplayAttribute attribute, GameplayAttribute source)
        {
            GameplayAttribute current = source;
            for (int guard = 0; current != null && guard <= count; guard++)
            {
                if (current == attribute)
                    return true;
                current = current.MaxSource;
            }
            return false;
        }
    }
}
