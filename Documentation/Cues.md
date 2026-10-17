# Gameplay Cues

Cues are the visual and audio feedback of gameplay: sparks, poison clouds, sounds. They are completely separate from gameplay logic: effects only mention a **cue tag**, and a **cue library** decides what that tag looks and sounds like.
Swapping a particle for a different one never touches gameplay.

## Three moments

| Moment | When | Typical use |
|---|---|---|
| **Executed** | Once, at an instant | Hit spark, damage number |
| **Added** | When a lasting effect starts | Poison cloud, burning |
| **Removed** | When the lasting effect ends | Stop the cloud |

An instant effect fires *Executed*. A duration or infinite effect fires *Added* when it becomes active and *Removed* when it ends or is suspended.

## Lasting cues are shared

If two effects add the same cue tag to the same character (two poison stacks), the cue is played **once** and removed when the **last** one ends. You never get duplicated particles.

## Cue definitions

| Type | What it does |
|---|---|
| **Prefab Cue** | Spawns a prefab. Modes: *One shot* (plays and disappears after a lifetime) or *Persistent* (lives until removed). Can be attached to the target. |
| **Audio Cue** | Plays a sound. |
| **Your own** | Inherit from the cue definition and override the three callbacks. `GameplayCueDefinition` |

The sample's cues:

| Cue | Mode |
|---|---|
| `Spark` | One shot, lifetime 1 s, attached to the target |
| `Poison` | Persistent, attached to the target |

## The cue library

A **Cue Library** maps cue tags to cue definitions. Assign it to the `AbilitySystemComponent`. Characters can share a library, or have different ones (a robot and an orc can show different sparks for the same `Cue.Spark`).

## Using cues from effects

In the effect Inspector, add tags to **Cue Tags**. That's it. `Cue.Spark` on `DamageEffect` plays the spark on every hit.

## Using cues from code

```csharp
asc.ExecuteCue(sparkTag, magnitude); // magnitude is by default 0
asc.AddCue(poisonTag);
asc.RemoveCue(poisonTag);
```

`GameplayCueParameters` carries the instigator, the target, a position and a magnitude so a cue can adapt (a bigger spark for more damage).

## Pooling

Prefab and audio cues use an internal pool. No instantiate or destroy at runtime after warm-up. Set **Prewarm Count** on a Prefab Cue to create the instances at startup, so the first hit doesn't hitch.

## When a character is destroyed

Its lasting cues are ended automatically and returned to the pool.