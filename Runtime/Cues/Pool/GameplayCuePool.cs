using System.Collections.Generic;
using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>
    /// Reuses the objects of the cues (particles, sounds) instead of creating and destroying them
    /// every time. It is created by itself the first time it is needed.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class GameplayCuePool : MonoBehaviour
    {
        private struct Scheduled
        {
            public PooledObject Target;
            public int Generation;
            public float ReleaseTime;
        }

        private static GameplayCuePool instance;
        private static bool quitting;

        private readonly Dictionary<GameObject, Stack<PooledCueInstance>> freeCues = new Dictionary<GameObject, Stack<PooledCueInstance>>();

        private readonly Stack<PooledSound> freeSounds = new Stack<PooledSound>();
        private readonly List<Scheduled> scheduled = new List<Scheduled>();

        // ---------- Static access ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when "Enter Play Mode" is set to not reload the domain.
            instance = null;
            quitting = false;

            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => quitting = true;

        private static GameplayCuePool Instance
        {
            get
            {
                if (instance == null && !quitting && Application.isPlaying)
                {
                    var host = new GameObject("[Gameplay Cue Pool]");
                    DontDestroyOnLoad(host);
                    instance = host.AddComponent<GameplayCuePool>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            // It only runs while something is waiting to be released.
            enabled = false;
        }

        // ---------- Prefabs ----------

        /// <summary>
        /// Gets an active instance of the prefab. The position, rotation and scale start as the prefab's,
        /// relative to the parent. Give it back with Release.
        /// </summary>
        public static GameObject Get(GameObject prefab, Transform parent = null)
        {
            GameplayCuePool pool = Instance;
            if (pool == null || prefab == null)
                return null;

            return pool.GetCue(prefab, parent).gameObject;
        }

        /// <summary>Makes sure there are at least 'count' free instances, so the first uses don't create any.</summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            GameplayCuePool pool = Instance;
            if (pool == null || prefab == null)
                return;

            Stack<PooledCueInstance> free = pool.FreeStackFor(prefab);
            while (free.Count < count)
                pool.ReleaseNow(pool.CreateCue(prefab));
        }

        /// <summary>
        /// Gives an instance back after 'delay' seconds. 'stopEmitting' lets its particles fade
        /// out during that time. An object that doesn't come from the pool is destroyed.
        /// </summary>
        public static void Release(GameObject instance, float delay = 0f, bool stopEmitting = false)
        {
            if (instance == null || quitting)
                return;

            GameplayCuePool pool = Instance;
            if (pool == null || !instance.TryGetComponent(out PooledCueInstance cue))
            {
                Destroy(instance, delay);
                return;
            }

            if (stopEmitting)
                cue.StopEmitting();

            if (delay <= 0f)
                pool.ReleaseNow(cue);
            else
                pool.Schedule(cue, Time.time + delay);
        }

        // ---------- Sounds ----------

        /// <summary>Plays a clip in 3D. Replaces AudioSource.PlayClipAtPoint, which creates an object each time.</summary>
        public static void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            GameplayCuePool pool = Instance;
            if (pool == null || clip == null)
                return;

            pool.PlaySoundInternal(clip, position, volume);
        }

        // ---------- Internal ----------

        private Stack<PooledCueInstance> FreeStackFor(GameObject prefab)
        {
            if (!freeCues.TryGetValue(prefab, out Stack<PooledCueInstance> stack))
            {
                stack = new Stack<PooledCueInstance>();
                freeCues.Add(prefab, stack);
            }
            return stack;
        }

        private PooledCueInstance CreateCue(GameObject prefab)
        {
            GameObject created = Instantiate(prefab, transform);
            PooledCueInstance cue = created.AddComponent<PooledCueInstance>();
            cue.Initialize(prefab);
            return cue;
        }

        private PooledCueInstance GetCue(GameObject prefab, Transform parent)
        {
            Stack<PooledCueInstance> free = FreeStackFor(prefab);

            PooledCueInstance cue = null;
            while (free.Count > 0 && cue == null)
                cue = free.Pop(); // A destroyed one (null) is skipped.

            if (cue == null)
                cue = CreateCue(prefab);

            cue.IsFree = false;
            cue.Generation++;

            Transform t = cue.transform;
            Transform source = prefab.transform;
            t.SetParent(parent, false);
            t.localPosition = source.localPosition;
            t.localRotation = source.localRotation;
            t.localScale = source.localScale;
            cue.gameObject.SetActive(true);

            return cue;
        }

        private void ReleaseNow(PooledObject pooled)
        {
            if (pooled == null || pooled.IsFree)
                return;

            pooled.IsFree = true;

            if (pooled is PooledCueInstance cue)
            {
                cue.ResetState();
                cue.gameObject.SetActive(false);
                cue.transform.SetParent(transform, false);
                FreeStackFor(cue.Prefab).Push(cue);
            }
            else if (pooled is PooledSound sound)
            {
                sound.Source.Stop();
                sound.Source.clip = null;
                sound.gameObject.SetActive(false);
                freeSounds.Push(sound);
            }
        }

        private void PlaySoundInternal(AudioClip clip, Vector3 position, float volume)
        {
            PooledSound sound = null;
            while (freeSounds.Count > 0 && sound == null)
                sound = freeSounds.Pop();

            if (sound == null)
                sound = CreateSound();

            sound.IsFree = false;
            sound.Generation++;

            sound.transform.position = position;
            sound.gameObject.SetActive(true);

            AudioSource source = sound.Source;
            source.clip = clip;
            source.volume = volume;
            source.Play();

            Schedule(sound, Time.time + clip.length + 0.1f);
        }

        private PooledSound CreateSound()
        {
            var go = new GameObject("Cue Sound");
            go.transform.SetParent(transform, false);

            PooledSound sound = go.AddComponent<PooledSound>();
            sound.Source = go.AddComponent<AudioSource>();
            sound.Source.playOnAwake = false;
            sound.Source.spatialBlend = 1f;
            return sound;
        }

        // ---------- Delayed releases ----------

        private void Schedule(PooledObject target, float releaseTime)
        {
            scheduled.Add(new Scheduled
            {
                Target = target,
                Generation = target.Generation,
                ReleaseTime = releaseTime
            });

            enabled = true;
        }

        private void Update()
        {
            float now = Time.time;

            // Backwards, so removing an entry doesn't skip the next one.
            for (int i = scheduled.Count - 1; i >= 0; i--)
            {
                Scheduled item = scheduled[i];

                // Destroyed together with its owner (a scene change, for example).
                if (item.Target == null)
                {
                    RemoveScheduledAt(i);
                    continue;
                }

                if (now < item.ReleaseTime)
                    continue;

                RemoveScheduledAt(i);

                // If the generation changed, it is already being used again: this release is old.
                if (item.Target.Generation == item.Generation)
                    ReleaseNow(item.Target);
            }

            enabled = scheduled.Count > 0;
        }

        private void RemoveScheduledAt(int index)
        {
            int last = scheduled.Count - 1;
            scheduled[index] = scheduled[last];
            scheduled.RemoveAt(last);
        }
    }
}
