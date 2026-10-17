namespace supercachola.AbilitySystem.Effects
{
    public enum EffectStackingPolicy
    {
        /// <summary>Every application is a separate effect.</summary>
        Independent,

        /// <summary>Applying it again restarts the duration (Duration effects only).</summary>
        RefreshDuration,

        /// <summary>Applying it again adds a charge, up to a maximum. It is still one effect.</summary>
        StackCharges,

        /// <summary>Applying it again does nothing.</summary>
        Ignore
    }
}
