using UnityEngine;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// A custom calculation for an effect. It reads attributes and tags of the instigator and the target
    /// and says what changes in the target. It only runs in Instant and Periodic effects.
    /// The asset is shared by every character, so it must not store state.
    /// </summary>
    public abstract class EffectExecution : ScriptableObject
    {
        public abstract void Execute(in EffectExecutionContext context, ExecutionOutput output);
    }
}
