using System;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem
{
    /// <summary>
    /// Overrides the starting base value of an attribute for one character.
    /// </summary>
    [Serializable]
    public struct AttributeInitialValue
    {
        [SerializeField] private GameplayTag attribute;
        [SerializeField] private float baseValue;
        
        public GameplayTag Attribute => attribute;
        public float BaseValue => baseValue;
    }
}
