using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    public enum PrefabCueMode
    {
        /// <summary>Spawns each time the cue is executed (hits, periodic ticks).</summary>
        OneShotOnExecute,
        /// <summary>Spawns once when a lasting effect starts (a flash when a buff is applied).</summary>
        OneShotOnAdd,
        /// <summary>Lives while the effect is active (a burning aura).</summary>
        Persistent
    }

    [CreateAssetMenu(menuName = "Ability System/Cues/Prefab Cue", fileName = "PrefabCue")]
    public sealed class PrefabCueDefinition : GameplayCueDefinition
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private PrefabCueMode mode = PrefabCueMode.OneShotOnExecute;
        [SerializeField] private bool attachToTarget = true;
        [SerializeField] private Vector3 offset;

        [Tooltip("One-shot: seconds before it is destroyed. Persistent: seconds it stays after the cue ends, so particles can fade out.")]
        [SerializeField, Min(0f)] private float lifetime = 2f;
        
        [Tooltip("Instances created when the scene starts, so the first uses don't create any.")]
        [SerializeField, Min(0)] private int prewarmCount = 2;
        
        public override void Prewarm()
        {
            if (prefab != null && prewarmCount > 0)
                GameplayCuePool.Prewarm(prefab, prewarmCount);
        }

        public override void OnExecuted(in GameplayCueParameters parameters)
        {
            if (mode == PrefabCueMode.OneShotOnExecute)
                Spawn(in parameters, lifetime);
        }

        public override object OnAdded(in GameplayCueParameters parameters)
        {
            switch (mode)
            {
                case PrefabCueMode.OneShotOnAdd:
                    Spawn(in parameters, lifetime);
                    return null;

                case PrefabCueMode.Persistent:
                    return Spawn(in parameters, 0f);

                default:
                    return null;
            }
        }

        public override void OnRemoved(in GameplayCueParameters parameters, object state)
        {
            // Unity null check: the instance may have been destroyed together with the character.
            if (!(state is GameObject instance) || instance == null)
                return;

            instance.transform.SetParent(null, true);
            GameplayCuePool.Release(instance, lifetime, stopEmitting: true);
        }

        private GameObject Spawn(in GameplayCueParameters parameters, float releaseAfter)
        {
            if (prefab == null || parameters.Target == null)
                return null;

            Transform parent = attachToTarget ? parameters.Target.transform : null;
            GameObject instance = GameplayCuePool.Get(prefab, parent);

            if (instance == null)
                return null;

            if (attachToTarget)
                instance.transform.localPosition = offset;
            else
                instance.transform.position = parameters.Position + offset;

            if (releaseAfter > 0f)
                GameplayCuePool.Release(instance, releaseAfter);

            return instance;
        }
    }
}
