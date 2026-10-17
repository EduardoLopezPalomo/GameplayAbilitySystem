using UnityEngine;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary> An ability that applies the effects and ends immediately. </summary>
    
    [CreateAssetMenu(menuName =  "Ability System/Abilities/Basic Ability", fileName =  "NewBasicAbility")]
    public class BasicAbilityDefinition : GameplayAbilityDefinition
    {
        public override GameplayAbility CreateAbility()
        {
            return new BasicAbility();
        }
    }
}
