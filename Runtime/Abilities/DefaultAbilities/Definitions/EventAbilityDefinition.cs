using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>An ability that lasts until an event arrives, a tag appears, or a time limit passes.</summary>
    [CreateAssetMenu(menuName = "Ability System/Abilities/Event Ability", fileName = "NewEventAbility")]
    public class EventAbilityDefinition : GameplayAbilityDefinition
    {
        [Tooltip("The ability ends when this event arrives (or a child of it). Optional.")]
        [SerializeField] private GameplayTag endEvent;

        [Tooltip("The ability ends when the character gets this tag. Optional.")]
        [SerializeField] private GameplayTag endTag;

        [Tooltip("Safety limit in seconds. 0 = no limit.")]
        [Min(0f)]
        [SerializeField] private float timeout = 5f;

        public GameplayTag EndEvent => endEvent;
        public GameplayTag EndTag => endTag;
        public float Timeout => timeout;

        public override GameplayAbility CreateAbility()
        {
            return new EventAbility();   
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!endEvent.isValid && !endTag.isValid && timeout <= 0f)
                Debug.LogWarning($"'{name}': nothing ends this ability. Set an end event, an end tag or a timeout.", this);
        }
    }
}
