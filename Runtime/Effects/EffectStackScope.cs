namespace supercachola.AbilitySystem.Effects
{
    /// <summary>Who a stack belongs to.</summary>
    public enum EffectStackScope
    {
        /// <summary>One stack per character that receives the effect.</summary>
        Target,

        /// <summary>One stack per character that applies the effect.</summary>
        Instigator
    }
}
