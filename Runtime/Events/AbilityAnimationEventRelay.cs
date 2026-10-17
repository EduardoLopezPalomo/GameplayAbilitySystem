using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Events
{
    /// <summary>
    /// Put it on the GameObject that has the Animator. In the animation clip, add an Animation Event:
    /// Function = SendAbilityEvent, String = the name of an event tag (for example Event.Dash.End).
    /// Float and Object are optional: they travel as the magnitude and the payload of the event.
    /// </summary>
    [AddComponentMenu("Ability System/Ability Animation Event Relay")]
    public class AbilityAnimationEventRelay : MonoBehaviour
    {
        [Tooltip("If empty, it is searched in this GameObject and its parents.")]
        [SerializeField] private AbilitySystemComponent target;

        private void Awake()
        {
            if (target == null)
                target = GetComponentInParent<AbilitySystemComponent>();

            if (target == null)
                Debug.LogWarning($"AbilityAnimationEventRelay '{name}': no AbilitySystemComponent found.", this);
        }

        // Called by Animation Events. Unity doesn't support overloads here, so there is only one method.
        public void SendAbilityEvent(AnimationEvent animationEvent)
        {
            if (target == null)
                return;

            string tagName = animationEvent.stringParameter;
            if (!GameplayTag.TryGet(tagName, out GameplayTag tag))
            {
                Debug.LogWarning(
                    $"AbilityAnimationEventRelay '{name}': '{tagName}' is not a known tag. Create it in the Tag Manager.", this);
                return;
            }

            target.SendGameplayEvent(new GameplayEventData(
                tag, target, target, animationEvent.floatParameter, animationEvent.objectReferenceParameter));
        }
    }
}
