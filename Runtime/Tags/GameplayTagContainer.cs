using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// The collection of gameplay tags that an object has. This is used to determine if an object has a certain tag, or if it matches a certain tag query.
    /// Each tag is a bit (the id)
    /// </summary>
    [Serializable]
    public class GameplayTagContainer : ISerializationCallbackReceiver
    {
        //A container use in a shared asset can not be changed
        [NonSerialized] private bool frozen;
        
        //The name of the tags that are save in disk. This is used to serialize the container, because the bits are not stable across sessions.
        [SerializeField] private List<string> tagNames = new List<string>();
        
        //tags that were added explicitly. No parents are added here, only the tags that were added directly.
        private ulong[] explicitBits = new ulong[1];

        //Tags that were added explicitly, plus all their parents. This is used to check if a tag is present, including its parents.
        private ulong[] expandedBits = new ulong[1];

        private int count;

        /// <summary>Number of tags added explicitly.</summary>
        public int Count => count;

        public bool IsEmpty => count == 0;
        
        public bool IsFrozen => frozen;

        /// <summary>
        /// Add a tag. Returns false if it was already present or if it's not valid.
        /// </summary>
        public bool AddTag(GameplayTag tag)
        {
            ThrowIfFrozen();
            if (!tag.isValid)
                return false;

            int id = tag.Id;
            EnsureCapacity(id);

            if (IsBitSet(explicitBits, id))
                return false;

            SetBit(explicitBits, id);
            count++;
            AddWithParents(id);
            return true;
        }

        /// <summary>
        /// Remove a tag. Returns false if it was not present or if it's not valid.
        /// </summary>
        public bool RemoveTag(GameplayTag tag)
        {
            ThrowIfFrozen();
            if (!tag.isValid || !IsBitSet(explicitBits, tag.Id))
                return false;

            ClearBit(explicitBits, tag.Id);
            count--;
            RebuildExpanded();
            return true;
        }

        public void Clear()
        {
            ThrowIfFrozen();
            Array.Clear(explicitBits, 0, explicitBits.Length);
            Array.Clear(expandedBits, 0, expandedBits.Length);
            count = 0;
        }

        /// <summary>
        /// Returns true if the container has that tag explicitly, without considering its parents.
        /// </summary>
        public bool HasTagExact(GameplayTag tag)
        {
            return tag.isValid && IsBitSet(explicitBits, tag.Id);
        }

        /// <summary>
        /// Returns true if the container has that tag, or any of its children. This considers the hierarchy of tags.
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            return tag.isValid && IsBitSet(expandedBits, tag.Id);
        }
        
        /// <summary>
        /// True if the tag is in this container, or is a child of a tag in it.
        /// It is the opposite direction of HasTag: with "Event.Hit" in the container, "Event.Hit.Critical" is covered.
        /// </summary>
        public bool Covers(GameplayTag tag)
        {
            if (!tag.isValid)
                return false;

            // Walks up the parents. It always ends: a parent has a lower id than its children.
            int id = tag.Id;
            while (id != 0)
            {
                if (IsBitSet(explicitBits, id))
                    return true;

                id = GameplayTagRegistry.GetParentId(id);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the container has any of the tags in "other".
        /// If "other" is empty, returns false.
        /// </summary>
        public bool HasAny(GameplayTagContainer other)
        {
            int length = Math.Min(expandedBits.Length, other.explicitBits.Length);
            for (int i = 0; i < length; i++)
            {
                if ((expandedBits[i] & other.explicitBits[i]) != 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the container has all of the tags in "other". This considers the hierarchy of tags.
        /// if "other" is empty, returns true.
        /// </summary>
        public bool HasAll(GameplayTagContainer other)
        {
            for (int i = 0; i < other.explicitBits.Length; i++)
            {
                ulong required = other.explicitBits[i];
                ulong mine = i < expandedBits.Length ? expandedBits[i] : 0UL;

                if ((mine & required) != required)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Add to results all explicit tags. This does not include parents of the tags.
        /// </summary>
        public void AppendTagsTo(List<GameplayTag> results)
        {
            for (int w = 0; w < explicitBits.Length; w++)
            {
                ulong word = explicitBits[w];
                if (word == 0) continue;

                for (int b = 0; b < 64; b++)
                {
                    if ((word & (1UL << b)) != 0)
                        results.Add(new GameplayTag((w << 6) + b));
                }
            }
        }
        
        public override string ToString()
        {
            var list = new List<GameplayTag>();
            AppendTagsTo(list);

            var sb = new StringBuilder("[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(list[i].Name);
            }
            return sb.Append(']').ToString();
        }

        // ---------- Internal for bit operations----------

        private void AddWithParents(int id)
        {
            ThrowIfFrozen();
            // Parents have a lower id than their children, so we can just go up the hierarchy until we reach the root (id 0).
            while (id != 0)
            {
                // if this bit is already set, we can stop, because all parents are already set too.
                if (IsBitSet(expandedBits, id))
                    break;

                SetBit(expandedBits, id);
                id = GameplayTagRegistry.GetParentId(id);
            }
        }

        private void RebuildExpanded()
        {
            Array.Clear(expandedBits, 0, expandedBits.Length);

            for (int w = 0; w < explicitBits.Length; w++)
            {
                ulong word = explicitBits[w];
                if (word == 0) continue;

                for (int b = 0; b < 64; b++)
                {
                    if ((word & (1UL << b)) != 0)
                        AddWithParents((w << 6) + b);
                }
            }
        }

        private void EnsureCapacity(int id)
        {
            int wordsNeeded = (id >> 6) + 1;
            if (wordsNeeded <= explicitBits.Length)
                return;

            int newSize = Math.Max(wordsNeeded, explicitBits.Length * 2);
            Array.Resize(ref explicitBits, newSize);
            Array.Resize(ref expandedBits, newSize);
        }

        private static bool IsBitSet(ulong[] bits, int id)
        {
            int word = id >> 6;
            return word < bits.Length && (bits[word] & (1UL << (id & 63))) != 0;
        }

        private static void SetBit(ulong[] bits, int id)
        {
            bits[id >> 6] |= 1UL << (id & 63);
        }

        private static void ClearBit(ulong[] bits, int id)
        {
            bits[id >> 6] &= ~(1UL << (id & 63));
        }
        // ---------- Serialization ----------
        //Before serializing, we need to convert the bits to names, because the bits are not stable across sessions. 
        // it's a reference to the GameplayTagRegistry name string.
        public void OnBeforeSerialize()
        {
            //In Build the list will be null because is not needed.
            if (tagNames == null)
                tagNames = new List<string>();
            
            tagNames.Clear();

            for (int w = 0; w < explicitBits.Length; w++)
            {
                ulong word = explicitBits[w];
                if (word == 0) continue;

                for (int b = 0; b < 64; b++)
                {
                    if ((word & (1UL << b)) != 0)
                        tagNames.Add(GameplayTagRegistry.GetName((w << 6) + b));
                }
            }
        }
        // After deserializing, we need to convert the names back to bits.
        public void OnAfterDeserialize()
        {
            // Unity is loading new data into this container (the Inspector was edited or the asset was reloaded).
            // The asset freezes it again in OnEnable / OnValidate.
            frozen = false;
            
            Array.Clear(explicitBits, 0, explicitBits.Length);
            Array.Clear(expandedBits, 0, expandedBits.Length);
            count = 0;

            if (tagNames != null)
            {
                for (int i = 0; i < tagNames.Count; i++)
                {
                    string name = tagNames[i];
                    if (GameplayTagRegistry.IsValidName(name))
                        AddTag(GameplayTag.Request(name));
                }
            }
#if !UNITY_EDITOR
    // In build, we don't need the tagNames list anymore, so we can free it to save memory.
    tagNames = null;
#endif
        }
        
        // ---------- Freeze ----------
        
        internal void Freeze() => frozen = true;

        private void ThrowIfFrozen()
        {
            if (frozen)
                throw new InvalidOperationException(
                    "This GameplayTagContainer belongs to an asset shared by many characters, so it can't be changed at runtime. " +
                    "Create your own GameplayTagContainer if you need a modified copy.");
        }
    }
}
