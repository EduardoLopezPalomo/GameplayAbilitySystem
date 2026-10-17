namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// Identifier for an active effect. This struct is used to track and manage active effects applied to an attribute set.
    /// </summary>
    public readonly struct ActiveEffectHandle
    {
        public static readonly ActiveEffectHandle None = default;

        internal readonly int Id;

        internal ActiveEffectHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id != 0;
    }
}
