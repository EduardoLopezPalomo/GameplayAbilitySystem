using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>An instance of a cue prefab that the pool reuses.</summary>
    [AddComponentMenu("")]
    internal sealed class PooledCueInstance : PooledObject
    {
        private ParticleSystem[] particles;
        private TrailRenderer[] trails;

        internal GameObject Prefab { get; private set; }

        internal void Initialize(GameObject prefab)
        {
            Prefab = prefab;

            // Searched once, when the instance is created. Not each time it is used.
            particles = GetComponentsInChildren<ParticleSystem>(true);
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        /// <summary>Stops creating new particles. The existing ones fade out.</summary>
        internal void StopEmitting()
        {
            for (int i = 0; i < particles.Length; i++)
                particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Leaves it clean for the next use.</summary>
        internal void ResetState()
        {
            for (int i = 0; i < particles.Length; i++)
                particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            for (int i = 0; i < trails.Length; i++)
                trails[i].Clear();
        }
    }
}
