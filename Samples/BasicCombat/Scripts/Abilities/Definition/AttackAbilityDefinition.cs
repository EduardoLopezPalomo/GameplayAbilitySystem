using UnityEngine;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Effects;

[CreateAssetMenu(menuName =  "Ability System/Abilities/Attack Ability", fileName =  "NewAttackAbility")]
public class AttackAbilityDefinition : GameplayAbilityDefinition
{
    [Tooltip("The rotation angle of the attack at the start of the animation.")]
    [SerializeField] private float startRotation = 50f;
    [SerializeField] private float endRotation = 50f;
    [SerializeField] private float attackSpeed = 200f;
    
    [Tooltip("Layer mask for detecting enemies hit by the attack.")]
    [SerializeField] private LayerMask enemyLayerMask;
    
    [Tooltip("Effects applied to the enemy when hit by the attack.")]
    [SerializeField] private GameplayEffectDefinition damageEffectAttributeBase;
    [SerializeField] private GameplayEffectDefinition invulnerabilityEffect;
    
    public float StartRotation => startRotation;
    public float EndRotation => endRotation;
    public float AttackSpeed => attackSpeed;

    public LayerMask EnemylayerMask => enemyLayerMask;
    public GameplayEffectDefinition DamageEffectAttributeBase => damageEffectAttributeBase;
    public GameplayEffectDefinition InvulnerabilityEffect => invulnerabilityEffect;

    public override GameplayAbility CreateAbility()
    {
        return new AttackAbility();
    }
}
