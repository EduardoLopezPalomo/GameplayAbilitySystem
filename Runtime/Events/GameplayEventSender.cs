using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Events
{
    /// <summary>
    /// Sends a gameplay event when you call Send from a UnityEvent (a button, a trigger, a timeline signal...).
    /// </summary>
    [AddComponentMenu("Ability System/Gameplay Event Sender")]
    public class GameplayEventSender : MonoBehaviour
    {
        [Tooltip("The character that receives the event. If empty, it is searched in this GameObject and its parents.")]
        [SerializeField] private AbilitySystemComponent target;

        [SerializeField] private GameplayTag eventTag;
        [SerializeField] private float magnitude;

        private void Awake()
        {
            if (target == null)
                target = GetComponentInParent<AbilitySystemComponent>();
        }

        [ContextMenu("Send now")]
        public void Send()
        {
            SendWithMagnitude(magnitude);
        }

        /// <summary>For UnityEvents that pass a number (a slider, for example).</summary>
        public void SendWithMagnitude(float value)
        {
            if (target == null)
            {
                Debug.LogWarning($"GameplayEventSender '{name}': no AbilitySystemComponent found.", this);
                return;
            }

            target.SendGameplayEvent(new GameplayEventData(eventTag, target, target, value));
        }
    }
}
