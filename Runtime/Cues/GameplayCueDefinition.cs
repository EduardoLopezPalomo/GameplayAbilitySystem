using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>
    /// Base of every cue. It is a shared asset, so it must not keep per-character data:
    /// what OnAdded returns is stored by the system and given back in OnRemoved.
    /// </summary>
    public abstract class GameplayCueDefinition : ScriptableObject
    {
        [SerializeField] private GameplayTag cueTag;

        public GameplayTag CueTag => cueTag;
        
        /// <summary>Creates what the cue will need ahead of time, so the first use doesn't have to.</summary>
        public virtual void Prewarm() { }

        /// <summary>One-time execution: an instant effect, or a period of a periodic one.</summary>
        public virtual void OnExecuted(in GameplayCueParameters parameters) { }

        /// <summary>Starts a lasting cue. The returned object (an instance, a handle...) is given back in OnRemoved.</summary>
        public virtual object OnAdded(in GameplayCueParameters parameters) => null;

        /// <summary>Ends a lasting cue.</summary>
        public virtual void OnRemoved(in GameplayCueParameters parameters, object state) { }
    }
}
