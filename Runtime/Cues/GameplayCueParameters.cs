using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>Data a cue receives. Read-only, so it can be passed by 'in' without copies.</summary>
    public readonly struct GameplayCueParameters
    {
        public GameplayTag Tag { get; }

        /// <summary>Who caused it. Can be null.</summary>
        public AbilitySystemComponent Instigator { get; }

        /// <summary>The character the cue plays on.</summary>
        public AbilitySystemComponent Target { get; }

        /// <summary>A number the cue can use (damage, heal...). Its meaning depends on who sends it.</summary>
        public float Magnitude { get; }

        /// <summary>Position of the target. Zero if it has been destroyed.</summary>
        public Vector3 Position => Target != null ? Target.transform.position : Vector3.zero;

        public GameplayCueParameters(GameplayTag tag, AbilitySystemComponent instigator, AbilitySystemComponent target, float magnitude)
        {
            Tag = tag;
            Instigator = instigator;
            Target = target;
            Magnitude = magnitude;
        }
    }
}
