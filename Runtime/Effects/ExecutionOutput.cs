using System.Collections.Generic;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// What an execution wants to change in the target. Changes are applied to the base value,
    /// like an instant effect, after the execution returns.
    /// </summary>
    public sealed class ExecutionOutput
    {
        internal struct Entry
        {
            public GameplayTag Attribute;
            public ModifierOperation Operation;
            public float Magnitude;
        }

        internal readonly List<Entry> Entries = new List<Entry>();

        public void Add(GameplayTag attribute, ModifierOperation operation, float magnitude)
        {
            if (!attribute.isValid)
                return;

            Entries.Add(new Entry { Attribute = attribute, Operation = operation, Magnitude = magnitude });
        }

        internal void Clear()
        {
            Entries.Clear();
        }
    }
}
