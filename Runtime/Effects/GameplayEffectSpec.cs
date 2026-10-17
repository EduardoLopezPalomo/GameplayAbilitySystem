using System;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// An effect ready to be applied: the definition, who applies it, and the values the caller sends.
    /// It is a struct: build it, apply it, and don't reuse it.
    /// </summary>
    public struct GameplayEffectSpec
    {
        private struct Entry
        {
            public GameplayTag Tag;
            public float Value;
        }

        // Only allocated when SetByCaller is used.
        private Entry[] entries;
        private int entryCount;

        public GameplayEffectDefinition Definition { get; }

        /// <summary>Who applies the effect. Null means the character that receives it.</summary>
        public AbilitySystemComponent Instigator { get; }

        public GameplayEffectSpec(GameplayEffectDefinition definition, AbilitySystemComponent instigator)
        {
            Definition = definition;
            Instigator = instigator;
            entries = null;
            entryCount = 0;
        }

        /// <summary>Sends a value to the effect. Sending the same tag again replaces it.</summary>
        public void SetByCaller(GameplayTag tag, float value)
        {
            if (!tag.isValid)
                return;

            for (int i = 0; i < entryCount; i++)
            {
                if (entries[i].Tag == tag)
                {
                    entries[i].Value = value;
                    return;
                }
            }

            if (entries == null)
                entries = new Entry[2];
            else if (entryCount == entries.Length)
                Array.Resize(ref entries, entryCount * 2);

            entries[entryCount++] = new Entry { Tag = tag, Value = value };
        }

        public readonly bool TryGetSetByCaller(GameplayTag tag, out float value)
        {
            for (int i = 0; i < entryCount; i++)
            {
                if (entries[i].Tag == tag)
                {
                    value = entries[i].Value;
                    return true;
                }
            }

            value = 0f;
            return false;
        }
    }
}