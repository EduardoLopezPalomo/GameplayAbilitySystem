using supercachola.AbilitySystem;
using UnityEngine;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;

public class EffectZone : MonoBehaviour
{
    [SerializeField] private GameplayEffectDefinition effect;
    [SerializeField] private bool removeInExit = true;

    [Header("Set by caller case")] 
    [SerializeField] private GameplayTag gameplayTag;
    [SerializeField] private float value;
    
    private ActiveEffectHandle effectHandle = ActiveEffectHandle.None;
    

    private void OnTriggerEnter(Collider other)
    {
        AbilitySystemComponent abilitySystemComponent = other.GetComponentInParent<AbilitySystemComponent>();
        
        if (abilitySystemComponent == null || effect == null) return;
        
        
        bool usesSetByCaller = false;

        foreach (EffectModifierDefinition modifier in effect.Modifiers)
        {
            if (modifier.MagnitudeType == MagnitudeType.SetByCaller)
            {
                usesSetByCaller = true;
                break;
            }
        }
        
        if (usesSetByCaller)
        {
            GameplayEffectSpec spec = abilitySystemComponent.MakeSpec(effect);
            spec.SetByCaller(gameplayTag, value);

            abilitySystemComponent.TryApplySpec(spec, out effectHandle);
        }
        else
        {
            effectHandle = abilitySystemComponent.ApplyEffect(effect);
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (!removeInExit) return;
        
        
        AbilitySystemComponent abilitySystemComponent = other.GetComponentInParent<AbilitySystemComponent>();
        if (abilitySystemComponent != null && effectHandle.IsValid)
        {
            abilitySystemComponent.RemoveEffect(effectHandle);
            effectHandle = ActiveEffectHandle.None;
        }
    }
}
