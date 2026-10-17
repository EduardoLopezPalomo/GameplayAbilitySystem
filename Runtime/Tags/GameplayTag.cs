using System;
using UnityEngine;

namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// A gameplay tag is a string that represents a category or property of an object.
    /// Tags can be hierarchical, with child tags inheriting the properties of their parent tags.
    /// </summary>
    [Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>, ISerializationCallbackReceiver
    {
        public static readonly GameplayTag None = default;
        
        [SerializeField] private string name;
        
        [NonSerialized] internal int Id;

        internal GameplayTag(int id)
        {
            Id = id;
            name = GameplayTagRegistry.GetName(id);
        }
        
        public bool isValid => Id != 0;
        
        public string Name => GameplayTagRegistry.GetName(Id);
        
        /// <summary>
        /// Obtains a gameplay tag from the registry. If the tag does not exist, it will be created.
        /// </summary>
        public static GameplayTag Request(string name)
        {
            return new GameplayTag(GameplayTagRegistry.GetOrRegister(name));   
        }
        /// <summary>
        /// Searches for a gameplay tag in the registry. If the tag does not exist, it will return false and the out parameter will be None.
        /// </summary>
        public static bool TryGet(string name, out GameplayTag tag)
        {
            if (GameplayTagRegistry.TryGetId(name, out int id))
            {
                tag = new GameplayTag(id);
                return true;
            }
            tag = None;
            return false;
        }
        /// <summary>
        /// True if the other tag is the same as this tag. This does not consider hierarchy.
        /// </summary>
        public bool MatchesExact(GameplayTag other)
        {
            return isValid && Id == other.Id;
        }
        /// <summary>
        /// True if this tag is the same as the other tag, or if this tag is a child of the other tag. This considers hierarchy.
        /// "Ability.Fire.Fireball".Matches("Ability.Fire") => true
        /// "Ability.Fire".Matches("Ability.Fire.Fireball") => false
        /// </summary>
        public bool Matches(GameplayTag other)
        {
            return isValid && other.isValid && GameplayTagRegistry.IsChildOrEqual(Id, other.Id);
        }
        
        //The name is serialized, but the Id is not. When serializing, we don't need to do anything, but when deserializing, we need to convert the name back to an Id.
        public void OnBeforeSerialize() { }
        
        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(name) || !GameplayTagRegistry.IsValidName(name))
            {
                Id = 0;
                return;
            }
            Id = GameplayTagRegistry.GetOrRegister(name);
            name = GameplayTagRegistry.GetName(Id); // Use the name of the register by reference
        }
        
        public bool Equals(GameplayTag other) => Id == other.Id;
        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => name;

        public static bool operator ==(GameplayTag a, GameplayTag b) => a.Id == b.Id;
        public static bool operator !=(GameplayTag a, GameplayTag b) => a.Id != b.Id;
    }
}
