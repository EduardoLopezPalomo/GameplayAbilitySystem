using UnityEngine;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>
    /// An ability that last some seconds, or until is cancelled. It can be used for channeling, toggles, or any ability that has a duration.
    /// </summary>
    
    [CreateAssetMenu(menuName = "Ability System/Abilities/Timed Ability", fileName = "NewTimedAbility")]
    public class TimedAbilityDefinition : GameplayAbilityDefinition
    {
        [Min(0.01f)]
        [SerializeField] private float duration = 1f;

        public float Duration => duration;

        public override GameplayAbility CreateAbility() => new TimedAbility();
    }
}
