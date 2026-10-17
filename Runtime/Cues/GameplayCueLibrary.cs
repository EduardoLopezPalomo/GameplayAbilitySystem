using System;
using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>List of cues a character can play. One library can be shared by all characters.</summary>
    [CreateAssetMenu(menuName = "Ability System/Cues/Cue Library", fileName = "CueLibrary")]
    public sealed class GameplayCueLibrary : ScriptableObject
    {
        [SerializeField] private List<GameplayCueDefinition> cues = new List<GameplayCueDefinition>();

        [NonSerialized] private Dictionary<int, GameplayCueDefinition> lookup;

        public bool TryGetCue(GameplayTag tag, out GameplayCueDefinition cue)
        {
            cue = null;
            if (!tag.isValid)
                return false;

            if (lookup == null)
                Build();

            return lookup.TryGetValue(tag.Id, out cue);
        }
        
        /// <summary>Prepares every cue of the library. Safe to call several times.</summary>
        public void PrewarmAll()
        {
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] != null)
                    cues[i].Prewarm();
            }
        }

        private void Build()
        {
            lookup = new Dictionary<int, GameplayCueDefinition>(cues.Count);

            for (int i = 0; i < cues.Count; i++)
            {
                GameplayCueDefinition cue = cues[i];
                if (cue == null || !cue.CueTag.isValid)
                    continue;

                if (!lookup.TryAdd(cue.CueTag.Id, cue))
                    Debug.LogWarning($"GameplayCueLibrary '{name}': two cues use the tag '{cue.CueTag.Name}'. Only the first one is used.", this);
            }
        }

        private void OnEnable() => lookup = null;
        private void OnValidate() => lookup = null;
    }
}
