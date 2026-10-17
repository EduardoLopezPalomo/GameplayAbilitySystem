using System;
using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>
    /// The attribute in a character. It has a base value, a current value, and a definition that defines its limits and tag.
    /// </summary>
    public sealed class GameplayAttribute
    {
        private readonly List<AttributeModifier> _modifiers = new List<AttributeModifier>();
        private int _nextModifierId = 1;
        
        private GameplayAttribute maxSource;
        private int maxSourceCount;
        
        public GameplayAttributeDefinition Definition { get; }
        public GameplayTag Tag => Definition.Tag;

        /// <summary>Value without temporary modifiers .</summary>
        public float baseValue { get; private set; }

        /// <summary>Final value, with limits applied</summary>
        public float currentValue { get; private set; }
        
        public int modifierCount => _modifiers.Count;
        
        /// <summary>The attribute that limits this one from above. Null if the limit is fixed.</summary>
        public GameplayAttribute MaxSource => maxSource;
        
        /// <summary>Current maximum: the value of the max attribute, or the fixed one of the definition.</summary>
        public float MaxValue => maxSource != null ? maxSource.currentValue : Definition.MaxValue;

        /// <summary>True if another attribute uses this one as its maximum.</summary>
        internal bool IsMaxSource => maxSourceCount > 0;

        /// <summary>Raised when the limits change, even if the value doesn't (a percentage threshold may need checking again).</summary>
        public event Action<GameplayAttribute> LimitsChanged;

        /// <summary>Is call when the current value changes. The float parameter is the old value.</summary>
        public event Action<GameplayAttribute, float> ValueChanged;

        public GameplayAttribute(GameplayAttributeDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Definition = definition;
            baseValue = ClampToLimits(definition.BaseValue);
            currentValue = baseValue;
        }

        public void SetBaseValue(float value)
        {
            baseValue = ClampToLimits(value);
            Recalculate();
        }
        /// <summary>
        /// It's use for instant changes to the base value, like when a character takes damage or heals.
        /// </summary>
        public void ApplyToBaseValue(ModifierOperation operation, float value)
        {
            switch (operation)
            {
                case ModifierOperation.Add:
                    SetBaseValue(baseValue + value);
                    break;
                case ModifierOperation.Multiply:
                    SetBaseValue(baseValue * value);
                    break;
                case ModifierOperation.Override:
                    SetBaseValue(value);
                    break;
            }
        }
        /// <summary>
        /// Add a modifier to the attribute. The modifier will be applied to the current value, and will be removed when the handle is disposed.
        /// </summary>
        public ModifierHandle AddModifier(ModifierOperation operation, float value)
        {
            var handle = new ModifierHandle(_nextModifierId++);
            _modifiers.Add(new AttributeModifier(handle.Id, operation, value));
            Recalculate();
            return handle;
        }
        /// <summary>
        /// Remove a modifier from the attribute. The modifier will be removed from the current value, and the handle will be invalidated.
        /// </summary>
        public bool RemoveModifier(ModifierHandle handle)
        {
            if (!handle.IsValid)
                return false;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Id == handle.Id)
                {
                    _modifiers.RemoveAt(i);
                    Recalculate();
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>Changes the value of an existing modifier. It keeps its position (it matters for overrides).</summary>
        internal bool SetModifierValue(ModifierHandle handle, float value)
        {
            if (!handle.IsValid)
                return false;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Id == handle.Id)
                {
                    _modifiers[i] = new AttributeModifier(handle.Id, _modifiers[i].Operation, value);
                    Recalculate();
                    return true;
                }
            }
            return false;
        }
        
        internal void LinkMax(GameplayAttribute source)
        {
            maxSource = source;
            source.maxSourceCount++;
            source.ValueChanged += OnMaxChanged;

            SetBaseValue(baseValue); // clamps to the new maximum
        }

        private void Recalculate()
        {
            float oldValue = currentValue;
            currentValue = ClampToLimits(Evaluate());

            if (!currentValue.Equals(oldValue))
                ValueChanged?.Invoke(this, oldValue);
        }
        
        private float Evaluate()
        {
            float sumAdd = 0f;
            float productMultiply = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                AttributeModifier m = _modifiers[i];

                switch (m.Operation)
                {
                    case ModifierOperation.Add:
                        sumAdd += m.Value;
                        break;
                    case ModifierOperation.Multiply:
                        productMultiply *= m.Value;
                        break;
                    case ModifierOperation.Override:
                        //the last override modifier wins, so we just store the value
                        hasOverride = true;
                        overrideValue = m.Value;
                        break;
                }
            }

            if (hasOverride)
                return overrideValue;

            return (baseValue + sumAdd) * productMultiply;
        }
        
        private void OnMaxChanged(GameplayAttribute source, float oldValue)
        {
            // re-clamps the base and recalculates (it raises ValueChanged if the value changes)
            SetBaseValue(baseValue); 
            LimitsChanged?.Invoke(this);
        }

        private float ClampToLimits(float value)
        {
            float min = Definition.MinValue;
            float max = Mathf.Max(min, MaxValue); 
            return Mathf.Clamp(value, min, max);
        }
    }
}
