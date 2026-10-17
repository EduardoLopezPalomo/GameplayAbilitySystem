# Attribute Reactions

A reaction watches an attribute and does something when it crosses a threshold. It needs no code: "below 25% health, apply `Enraged`", "at 0 stamina, apply `Exhausted`".

## Configuring

Reactions are a list on the `AbilitySystemComponent`. Each one has:

| Field | Meaning |
|---|---|
| **Attribute** | The attribute to watch (by tag). |
| **Comparison** | `Less Than`, `Less Or Equal`, `Greater Than`, `Greater Or Equal`. |
| **Threshold mode** | **Absolute** (a number) or **Percent Of Max** (e.g. 0.25 of the attribute's max). |
| **Threshold** | The value. |
| **Effect while met** | Effect applied **while the condition is met**. |
| **Enter event / Exit event** | Gameplay events sent when the condition becomes true / false. |

## Edge-triggered

A reaction acts **when the condition changes**, not on every change of the attribute:

- Condition becomes true -> the effect is applied and the enter event is sent, once.
- Condition becomes false -> the effect is removed and the exit event is sent, once.

Taking damage while already below the threshold does nothing extra. This also means the effect is never applied twice.

## Percent of max

With **Percent Of Max**, the threshold follows the attribute's maximum. `Health < 25%` stays correct if a buff raises `MaxHealth` (see dynamic maximum in [Attributes](Attributes.md)).

## Examples

| Goal | Setup |
|---|---|
| Low-health warning | `Health`, Less Than, Percent 0.25, enter event `Event.LowHealth` |
| Berserk | `Health`, Less Or Equal, Percent 0.3, effect `Berserk` (+damage) |
| Exhaustion | `Stamina`, Less Or Equal, Absolute 0, effect `Exhausted` |
| Death | `Health`, Less Or Equal, Absolute 0, effect `Dead` |

## Safety

If a reaction's effect changes the very attribute it watches, the system prevents infinite loops with a re-entrancy guard: a reaction can't re-trigger itself recursively