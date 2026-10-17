using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Events
{
    /// <summary>
    /// What an event carries: its tag, who caused it, who receives it, a number and an optional object.
    /// </summary>
    public readonly struct GameplayEventData
    {
        public GameplayTag Tag { get; }

        /// <summary>Who caused the event. Can be null.</summary>
        public AbilitySystemComponent Instigator { get; }

        /// <summary>Who the event is about. Can be null.</summary>
        public AbilitySystemComponent Target { get; }

        /// <summary>A number the sender wants to pass (damage, distance, a charge...).</summary>
        public float Magnitude { get; }

        /// <summary>An object the sender wants to pass (a collider, a ScriptableObject...). Can be null.</summary>
        public UnityEngine.Object Payload { get; }

        public GameplayEventData(GameplayTag tag, AbilitySystemComponent instigator = null, AbilitySystemComponent target = null,
            float magnitude = 0f, UnityEngine.Object payload = null)
        {
            Tag = tag;
            Instigator = instigator;
            Target = target;
            Magnitude = magnitude;
            Payload = payload;
        }
    }
}
