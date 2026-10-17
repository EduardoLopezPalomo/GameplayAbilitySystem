# Abilities

An ability is an action a character can perform: attack, dash, cast a spell. As with effects, the **definition** is a ScriptableObject shared by all characters.
Each character that has the ability gets its own **runtime instance**, so cooldowns and internal state never leak between characters.

## Anatomy

| Field | Purpose |
|---|---|
| **Ability Tag** | Identifies the ability (`Ability.Attack`). |
| **Display name**| Used as the ability name, for example, in a menu.| 
| **Icon**| Use as the ability sprite for UI.| 
| **Description**| Description of the ability, if needed for the UI..| 
| **Cost effect** | Effect applied on activation (e.g. spend stamina). If the character can't pay, the ability doesn't activate. |
| **Cooldown effect** | Effect that grants a cooldown tag. While the character has it, the ability can't activate. |
| **Activation required tags** | Tags the character must have. |
| **Activation blocked tags** | Tags that prevent activation (`State.Stunned`, `State.Dead`). |
| **Cancel abilities with tags** | Active abilities with these tags are canceled when this one activates. |
| **Trigger event tags** | Activate automatically when a gameplay event is received. |

Activation rules are checked in this order: required tags, blocked tags, cooldown, cost. Failing any of them cancels the activation without side effects.

## Writing the behaviour

The definition holds the data. The behaviour lives in a class that inherits from the runtime ability. 

```csharp
// Definition (ScriptableObject): data shared by all characters
[CreateAssetMenu(menuName = "...")]
public class AttackAbilityDefinition : GameplayAbilityDefinition
{
    public GameplayEffectDefinition damageEffect;
    public override GameplayAbility CreateAbility()
    {
        return new AttackAbility();
    }
}

// Instance: one per character
public class AttackAbility : GameplayAbility
{
    protected override void OnActivate()
    {
        // sweep, find targets, apply effects...
        End();
    }
}
```

Always call `End()` (or let a wait finish) when the ability is over. An ability that never ends keeps blocking itself.

## Granting and activating

```csharp
GameplayAbility abilityInstance = asc.GrantAbility(attackAbilityDefinition);
bool success = asc.TryActivateAbility(abilityTag);
```

The sample binds `AttackAbility` to left click through the input binder. The component itself has no input dependency, so you can drive abilities from the Input System, the legacy input, AI, or a network message.

## Waiting inside an ability

Abilities can wait without coroutines. Cache the callback in a field to avoid allocations.
It can wait for a duration, or for a **gameplay event** or tag (see [Events](Events.md)).For example, an attack can wait for an "animation hit" event and only then apply damage.

```csharp
WaitForDelay(seconds, () => {/* ... */});
WaitForEvent(eventTag, (GameplayEventData) => {/* ... */}, once ); // once is true by default, it only fires the first time
WaitForTag( gameplayTag, isPresent, () =>{/* ... */}, once ); //the owner get the tags (isPresent = true) or losses it (isPresent = false)
```

## Events as triggers

An ability can be configured to activate when a gameplay event with a given tag arrives, with the event payload available to the ability. This is how "when hit, parry" or "on death, explode" are built without polling.

## Good practices

- Put numbers in effects, not in ability code. Designers can tweak `CooldownAttack` without touching C#.
- Use blocked tags for rules ("can't attack while stunned") instead of `if` checks.
- Keep abilities small. Combine them through tags and events.