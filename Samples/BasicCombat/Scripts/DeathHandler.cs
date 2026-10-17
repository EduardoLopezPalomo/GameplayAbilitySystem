using UnityEngine;
using supercachola.AbilitySystem;
using supercachola.AbilitySystem.Events;
using supercachola.AbilitySystem.Tags;

public class DeathHandler : MonoBehaviour
{
    [SerializeField] private AbilitySystemComponent asc;
    [SerializeField] private GameplayTag deathEvent;
    
    [Tooltip("0 = don't destroy the object.")]
    [SerializeField] private float destroyDelay = 3f;

    private void OnEnable() => asc.RegisterGameplayEvent(deathEvent, OnDeath);
    private void OnDisable() => asc.UnregisterGameplayEvent(deathEvent, OnDeath);

    private void OnDeath(GameplayEventData data)
    {

        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
    }
}
