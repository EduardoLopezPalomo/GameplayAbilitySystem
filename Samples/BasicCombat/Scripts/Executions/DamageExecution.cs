using UnityEngine;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Effects
{
    /// <summary>
    /// Damage = max(Attack x multiplier - Defense, minimum), with optional crits and a resistance percentage.
    /// </summary>
    [CreateAssetMenu(menuName = "Ability System/Executions/Damage", fileName = "NewDamageExecution")]
    public class DamageExecution : EffectExecution
    {
        [Header("Attributes")]
        [Tooltip("Read from the instigator.")]
        [SerializeField] private GameplayTag attackAttribute;

        [Tooltip("Read from the target. Optional.")]
        [SerializeField] private GameplayTag defenseAttribute;

        [Tooltip("Read from the target, as a percentage (0-100). Optional.")]
        [SerializeField] private GameplayTag resistanceAttribute;

        [Tooltip("The attribute of the target that loses the damage.")]
        [SerializeField] private GameplayTag healthAttribute;

        [Header("Formula")]
        [Tooltip("The attack is multiplied by this before subtracting the defense.")]
        [SerializeField] private float attackMultiplier = 1f;

        [Tooltip("The damage never goes below this before the resistance. 0 allows a full block.")]
        [Min(0f)]
        [SerializeField] private float minimumDamage = 1f;

        [Header("Critical hits")]
        [Range(0f, 1f)]
        [SerializeField] private float critChance = 0f;

        [Min(1f)]
        [SerializeField] private float critMultiplier = 2f;

        public override void Execute(in EffectExecutionContext context, ExecutionOutput output)
        {
            float attack = 0f;
            if (attackAttribute.isValid)
                context.TryGetInstigatorValue(attackAttribute, out attack);

            float defense = 0f;
            if (defenseAttribute.isValid)
                context.TryGetTargetValue(defenseAttribute, out defense);

            float damage = Mathf.Max(attack * attackMultiplier - defense, minimumDamage);

            if (critChance > 0f && Random.value < critChance)
                damage *= critMultiplier;

            if (resistanceAttribute.isValid && context.TryGetTargetValue(resistanceAttribute, out float resistance))
                damage *= 1f - Mathf.Clamp01(resistance / 100f);

            if (damage > 0f)
                output.Add(healthAttribute, ModifierOperation.Add ,-damage);
        }

        private void OnValidate()
        {
            if (!attackAttribute.isValid)
                Debug.LogWarning($"'{name}': no attack attribute, the attack will count as 0.", this);

            if (!healthAttribute.isValid)
                Debug.LogWarning($"'{name}': no health attribute, this execution does nothing.", this);
        }
    }
}