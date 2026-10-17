# Attributes

An attribute is a numeric value such as health, attack or stamina. Each one has:

| Value | Meaning |
|---|---|
| **Base value** | The "permanent" value. Changed by instant effects (damage, healing). |
| **Current value** | Base value plus all active modifiers. This is what gameplay reads. |
| **Min / Max** | Limits applied to the result. |

## Defining an attribute

Create a **Gameplay Attribute Definition** `Create > Ability System > Attribute > Attribute Definition` and set:

- **Attribute Tag**: the tag that identifies it (`Attribute.Health`). Everything in the system refers to attributes by tag.
- **Base Value**, **Min**, **Max**.
- **Max Attribute Tag** (optional): see below.

Definitions are shared assets. Hundreds of characters use the same definition; each character keeps its own runtime values.

## Attribute sets

An **Attribute Set** lists the definitions a character has. Assign it to the `AbilitySystemComponent`, and the character is created with the base values of each definition.

You can also override the initial value per character in the `AbilitySystemComponent`.

## Dynamic maximum

Health shouldn't exceed the character's maximum health, and that maximum can change (a buff, a level up). Instead of hardcoding the limit, point the attribute to another one:

- `Health` -> **Max Attribute Tag** = `Attribute.MaxHealth`

Now the maximum of `Health` is always the *current value* of `MaxHealth`:
- When `MaxHealth` rises, `Health` keeps its value (it doesn't heal for free).
- When `MaxHealth` drops below `Health`, `Health` is clamped down.

The system rejects loops (A's max is B, B's max is A) when the set is built.

## Modifiers

Effects change attributes through modifiers. There are three operations, applied in this order:

1. **Add**: `+10`.
2. **Multiply**: `x1.5`. All multipliers are combined.
3. **Override**: forces the value, ignoring the rest.

```
current = (base + all add) x all multiply        // unless an Override is active
```

- *Duration* and *Infinite* effects apply their modifiers to the **current** value and remove them when the effect ends. The base value is not touched.
- *Instant* effects change the **base** value permanently.

Example: `AttackBuff` (+10 Attack, infinite). While active, `Attack` is 20 instead of 10; remove the effect and it returns to 10.

## Reading and listening

```csharp

bool success = asc.TryGetAttribute(dead, out GameplayAttribute attribute);

//Lambda subscriptions cannot be unsubscribed directly; use a named method instead.
attribute.LimitsChanged += (gameplayAttribute) => {/* ... */} ;
attribute.ValueChanged += (gameplayAttribute, value) => {/* ... */};
```

Prefer listening over polling. The sample's `UiGasBridge` does exactly this: it listens to `Health` and `MaxHealth` and updates the bar only when something changes.

## Reacting to thresholds

For "do something when health drops below 25%" you don't need to write code: use [Attribute Reactions](Reactions.md).