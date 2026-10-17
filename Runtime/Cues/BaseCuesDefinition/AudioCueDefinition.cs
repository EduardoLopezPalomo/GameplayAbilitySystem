using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    [CreateAssetMenu(menuName = "Ability System/Cues/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCueDefinition : GameplayCueDefinition
    {
        [Tooltip("One of them is picked at random each time.")]
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool playOnExecuted = true;
        [SerializeField] private bool playOnAdded;

        public override void OnExecuted(in GameplayCueParameters parameters)
        {
            if (playOnExecuted)
                Play(in parameters);
        }

        public override object OnAdded(in GameplayCueParameters parameters)
        {
            if (playOnAdded)
                Play(in parameters);

            return null;
        }

        private void Play(in GameplayCueParameters parameters)
        {
            if (clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                GameplayCuePool.PlaySound(clip, parameters.Position, volume);
        }
    }
}
