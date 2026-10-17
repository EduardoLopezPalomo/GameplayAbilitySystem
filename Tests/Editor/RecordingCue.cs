using supercachola.AbilitySystem.Cues;

namespace supercachola.AbilitySystem.Tests
{
    public sealed class RecordingCue : GameplayCueDefinition
    {
        public int Executed;
        public int Added;
        public int Removed;
        public float LastMagnitude;

        public override void OnExecuted(in GameplayCueParameters parameters)
        {
            Executed++;
            LastMagnitude = parameters.Magnitude;
        }

        public override object OnAdded(in GameplayCueParameters parameters)
        {
            Added++;
            return this;
        }

        public override void OnRemoved(in GameplayCueParameters parameters, object state)
        {
            Removed++;
        }
    }
}
