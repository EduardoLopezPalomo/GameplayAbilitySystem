using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>An AudioSource that the pool reuses to play cue sounds.</summary>
    [AddComponentMenu("")]
    internal sealed class PooledSound : PooledObject
    {
        internal AudioSource Source;
    }
}
