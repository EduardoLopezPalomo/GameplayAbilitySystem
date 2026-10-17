namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>
    /// Identifies a modifier that is applied to an attribute. This is used to track which modifiers are currently affecting an attribute, and to remove them when they are no longer needed.
    /// </summary>
    public readonly struct ModifierHandle
    {
        public static readonly ModifierHandle None = default;

        internal readonly int Id;

        internal ModifierHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id != 0;
    }
}
