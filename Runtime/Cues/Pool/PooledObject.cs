using UnityEngine;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>Base of the components that mark an object as managed by the pool.</summary>
    internal abstract class PooledObject : MonoBehaviour
    {
        // Changes every time the object is handed out. A delayed release compares it,
        // so a late release can never take back an object that is already being used again.
        internal int Generation;

        internal bool IsFree;
    }
}
