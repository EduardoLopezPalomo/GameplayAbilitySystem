namespace supercachola.AbilitySystem.Effects
{
    public enum EffectDurationPolicy
    {
        /// <summary>Modify the value one time and ends</summary>
        Instant,

        /// <summary>Add modifiers and removes them when the time is up</summary>
        Duration,

        /// <summary>Add modifiers until somebody removes it</summary>
        Infinite
    }
}
