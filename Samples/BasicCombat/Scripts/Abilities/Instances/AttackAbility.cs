using System.Collections.Generic;
using supercachola.AbilitySystem;
using UnityEngine;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Tags;

public class AttackAbility : GameplayAbility
{
    private const int MaxHitColliders = 16;
    
    private readonly Collider[] hitBuffer = new Collider[MaxHitColliders];
    private readonly List<AbilitySystemComponent> alreadyHit = new List<AbilitySystemComponent>();

    private AttackAbilityDefinition config;
    private GameObject sword;
    private Transform hitStart;
    private Transform hitEnd;
    private float hitRadius;
    private float currentRotation;

    protected override void OnInitialize()
    {
        config = (AttackAbilityDefinition)Definition;
        
        Sword swordComp = Owner.gameObject.GetComponentInChildren<Sword>(true);
        
        if(swordComp == null) return;
        
        hitStart = swordComp.hitStart;
        hitEnd = swordComp.hitEnd;
        hitRadius = swordComp.hitRadius;
        
        sword = swordComp.gameObject;
        
        sword.SetActive(false);
    }

    protected override bool CanActivateCustom()
    {
        if (sword == null)
        {
            Debug.LogWarning($"AttackAbility: '{Owner.name} has no sword component so the ability can't activate");
            return false;
        }
        return true;
    }

    protected override void OnActivate()
    {
        alreadyHit.Clear();
        sword.SetActive(true);
      
        currentRotation = config.StartRotation;
        sword.transform.localRotation = Quaternion.Euler(0f, currentRotation, 0f);
    }

    protected override void OnTick(float deltaTime)
    {
        currentRotation = Mathf.MoveTowards(currentRotation, config.EndRotation, deltaTime * config.AttackSpeed);
        
        sword.transform.localRotation = Quaternion.Euler(0f,currentRotation, 0f );
        
        CheckHit();
        
        if (Mathf.Approximately(currentRotation,config.EndRotation))
        {
            End();
        }
    }

    private void CheckHit()
    {
        int count = Physics.OverlapCapsuleNonAlloc(
            hitStart.position, hitEnd.position, hitRadius,
            hitBuffer, config.EnemylayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = hitBuffer[i];
            if (!hit.CompareTag("Enemy"))
                continue;
            
            AbilitySystemComponent target = hit.GetComponentInParent<AbilitySystemComponent>();
            if (target == null || target == Owner || alreadyHit.Contains(target))
                continue;
            
            alreadyHit.Add(target);
            
            if (Owner.TryApplyEffectTo(target, config.DamageEffectAttributeBase, out _) && config.InvulnerabilityEffect != null)
                target.ApplyEffect(config.InvulnerabilityEffect);   
        }
    }


    protected override void OnEnd(bool wasCancelled)
    {
        alreadyHit.Clear();
        
        if(sword != null)
            sword.SetActive(false);
    }
}
