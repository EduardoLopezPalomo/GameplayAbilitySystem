# Gameplay Effects

An effect is the only way gameplay changes a character: damage, healing, buffs, debuffs, cooldowns, states, immunities. An effect is a ScriptableObject: **shared data**. When you apply it to a character, the system creates a lightweight runtime instance for that character.

## Duration types

| Type | Lifetime | Typical use |
|---|---|---|
| **Instant** | Applies once and disappears | Damage, healing |
| **Duration** | Ends after N seconds | Cooldown, temporary buff, poison |
| **Infinite** | Until removed explicitly | Equipment bonus, permanent state |

## What an effect can contain

| Section | What it does |
|---|---|
| **Modifiers** | Change attributes (Add / Multiply / Override). See [Attributes](Attributes.md). |
| **Magnitude** | Where the number comes from: a fixed value, **Set By Caller**, or an attribute of the instigator/target, using this formula (attribute x coefficient + magnitude). |
| **Executions** | Custom calculations in code (e.g. `DamageExecution`). |
| **Period** | Executes modifiers/executions every N seconds (damage over time). |
| **Granted tags** | Tags the character has while the effect is active. |
| **Application requirements** | Tags the target must have / must not have for the effect to apply. |
| **Ongoing requirements** | Tags needed while the effect lives. If they stop being met, the effect is *suspended* (its modifiers, tags and cues are removed) and resumes when they are met again. |
| **Remove effects with tags** | Removes other active effects (cleanse, dispel). |
| **Immunity** | Granted immunity tags block effects tagged with them. |
| **Stacking** | See below. |
| **Cues** | Visual/audio feedback by tag. See [Cues](Cues.md). |

An effect must do *something*: modifiers, an execution, granted tags, immunity or cues. Otherwise the system warns that the effect "has nothing to do".

## Applying an effect

Effects can come from abilities (cost, cooldown, hit effects), from world objects (`EffectZone`), or from your own code.

```csharp
bool applied    = asc.TryApplyEffect(effectDefinition, out ActiveEffectHandle handle);
bool appliedTo  = asc.TryApplyEffectTo(targetAsc, effectDefinition, out ActiveEffectHandle targetHandle);
bool specApplied = asc.TryApplySpec(spec, out ActiveEffectHandle specHandle); //See set by caller option
```
The `handle` is used to identify an effect so it can be removed later. It is also possible to remove effects by tags.

```csharp
bool removed = asc.RemoveEffect(handle);
int howMany  = asc.RemoveEffectsWithTags(container);
```

## Set By Caller

Sometimes the number is not known when you author the effect (damage depends on the weapon, poison on the zone). Mark the magnitude as **Set By Caller** with a tag, and pass the value when applying.
For that we use a *GameplayEffectSpec* that needs the effect definition and the `AbilitySystemComponent` of the instigator.

```csharp
GameplayEffectSpec spec = new GameplayEffectSpec(effectDefinition, ascInstigator);
//or use the AbilitySystemComponent to be that component the instigator
//GameplayEffectSpec spec = asc.MakeSpec(effectDefinition);

spec.SetByCaller(gameplayTag, value);
bool success = spec.TryGetSetByCaller(gameplayTag, out float stored);
```

The sample's `Poison` effect works this way: each `EffectZone` decides how strong its poison is.

## Stacking

When the same effect is applied again to the same character, stacking decides what happens: 

- **Independent**: each effect acts like if no other effect is present.
- **Stack Charges**: maximum number of stacks (the sample's poison: 3).
- **Refresh duration** on new application.
- **Igoner**: if the same effect is already present, the new application is ignored.

## Executions

When a modifier isn't enough, an **Execution** is a ScriptableObject with a method that reads attributes from the instigator and the target and returns what to change. `DamageExecution` in the sample calculates damage from `Attack`, `Defense` and `Resistance`.

```csharp
public override void Execute(in EffectExecutionContext context, ExecutionOutput output)
```

Use it when the result depends on several attributes at once.

**Effect execution context** has the `effect spec`, the `attributes` and `tags` of the target and instigator. 
**Execution Output** is a list of operations, each specifying an `attribute`, a `modifier`, and a value.

## Tags as state

Effects that only grant a tag are valid and useful. `Dead` does nothing by itself; `DeathHandler` listens for the tag and decides what to do. This keeps gameplay rules decoupled: adding a new reaction to death never requires editing the effect.

## Performance notes

- Ticking effects allocates **0 bytes**.
- Applying and removing an **instant** effect allocates **0 bytes**.
- Applying a **duration/infinite** effect allocates a small runtime instance (around 150 B). 