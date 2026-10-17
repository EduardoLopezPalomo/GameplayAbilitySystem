# Gameplay Tags

Tags are hierarchical names such as `State.Dead` or `Damage.Fire`. Internally each tag is an `int`, so comparing them costs nothing and creates no garbage. You never compare strings at runtime.

## Hierarchy

A tag *matches* its parents. A character that has `State.Stunned.Frozen` also matches `State.Stunned` and `State`.

| Character has | Query | Result |
|---|---|---|
| `State.Stunned.Frozen` | `State.Stunned` | match |
| `State.Stunned` | `State.Stunned.Frozen` |no match |

Use this to write broad rules ("block everything that is `State.Stunned`") without listing every variant.

## Defining tags

Tags live in `GameplayTagSettings` (`Assets/Resources/GameplayTagSettings.asset`, created automatically). There is one way to add them:

1. **Tag manager** `Window > Ability System > Tag Manager`: add tags one by one or delete tags.

Tags are stored by name in your assets and resolved to ids when the game loads. Renaming a tag in the settings does not rename it in assets that already reference it, so treat names as stable.

## Using tags in the Inspector

Every field of type `GameplayTag` or `GameplayTagContainer` gets a tag picker. Containers are shown as a list of chips.

## Using tags in code

```csharp

AbilitySystemComponent asc;
GameplayTag dead = GameplayTag.Request("State.Dead");   // cache this, don't request every frame
GameplayTagContainer container;

if (asc.HasTag(dead)) { /* ... */ }
if (asc.HasAnyTag(container)) { /* ... */ }
if (asc.HasAllTags(container)) { /* ... */ }
if (asc.HasExactTag(dead)){ /* ... */} //HasExactTag matches only that exact tag, not its children. 
```

Cache tags in `static readonly` fields or serialized fields. Looking one up by name is the only operation in the system that touches strings.

## Containers

`GameplayTagContainer` is a bitset: `HasAny`, `HasAll` and `Contains` are a few integer operations. Containers inside ScriptableObjects are **frozen** when the asset loads, so the shared asset cannot be modified at runtime by mistake.

## Tags on characters

A character's tags are counted, not just stored. If two effects grant `State.Burning`, the tag stays until **both** are gone. You never need to track this yourself.

Tags come from:
- **Granted tags** of active effects (while the effect is active).
- **Owned tags** of active abilities (while the ability is active).
- Anything you add through the ability component like this:

```csharp
asc.OwnedTags.AddTag(tag);
asc.OwnedTags.AddTags(container);
```

## Naming conventions

| Prefix | Use | Example |
|---|---|---|
| `State.` | Something the character *is* | `State.Dead`, `State.Stunned` |
| `Ability.` | Identifies an ability | `Ability.Attack` |
| `Cooldown.` | Marks a cooldown | `Cooldown.Attack` |
| `Effect.` | Categorises effects (to cleanse them, for example) | `Effect.Poison` |
| `Attribute.` | Identifies an attribute | `Attribute.Health` |
| `Event.` | Gameplay events | `Event.Death` |
| `Cue.` | Gameplay cues | `Cue.Spark` |

It's only a convention, but following it makes the debugger and the Inspector much easier to read.