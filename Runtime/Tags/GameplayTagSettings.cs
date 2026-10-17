using UnityEngine;
using System.Collections.Generic;


namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// A registry for gameplay tags in the project.
    /// Is in Assets/Resources/GameplayTagSettings.asset
    /// </summary>
    public class GameplayTagSettings : ScriptableObject
    {
        public const string ResourceName = "GameplayTagSettings";
        
        [SerializeField, ReadOnly] private List<string> tags = new List<string>();

        public IReadOnlyList<string> Tags => tags;
        
        public static bool IsValidName(string name)
        {
            return GameplayTagRegistry.IsValidName(name);
        }
        
        public static GameplayTagSettings Load()
        {
            return Resources.Load<GameplayTagSettings>(ResourceName);
        }
        /// <summary>
        /// Add a tag to the list. Returns false if the tag is invalid or already exists.
        /// </summary>
        public bool AddTag(string name)
        {
            if (!IsValidName(name) || tags.Contains(name))
                return false;
            
            return AddWithParents(name);;
        }
        /// <summary>
        /// Add to the list all the parents of the tags in the list. Returns false if nothing changed.  
        /// Returns true if any parent was added. This is useful to ensure that all tags have their parents in the list, so that the hierarchy is complete.
        /// </summary>
        public bool EnsureParents()
        {
            bool changed = false;
            int originalCount = tags.Count;

            for (int i = 0; i < originalCount; i++)
            {
                if (IsValidName(tags[i]) && AddWithParents(tags[i]))
                    changed = true;
            }
            return changed;
        }

        private bool AddWithParents(string name)
        {
            bool added = false;
            int dotIndex = name.IndexOf('.');
            
            while (dotIndex != -1)
            {
                string parent = name.Substring(0, dotIndex);
                if (!tags.Contains(parent))
                {
                    tags.Add(parent);
                    added = true;
                }
                
                dotIndex = name.IndexOf('.', dotIndex + 1);
            }
            
            if (!tags.Contains(name))
            {
                tags.Add(name);
                added = true;
            }

            return added;
        }

        /// <summary>
        /// Remove a tag form the list. Also removes all its children. Returns false if the tag does not exist.
        /// </summary>
        public void RemoveTag(string name)
        {
            tags.RemoveAll(t => t == name || t.StartsWith(name + "."));
        }

        /// <summary>
        /// Register all tags in the list to the GameplayTagRegistry. This is called automatically on startup.
        /// </summary>
        public void RegisterAll()
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (IsValidName(tags[i]))
                    GameplayTagRegistry.GetOrRegister(tags[i]);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterOnStartup()
        {
            var settings = Load();
            if (settings != null)
                settings.RegisterAll();
        }
    }
}
