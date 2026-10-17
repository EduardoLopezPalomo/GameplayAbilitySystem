# Gameplay Events

Gameplay events let systems talk to each other through tags, without direct references. "The character died" is an event; the UI, the loot system and the XP system can each listen to it, and none of them knows about the others.

## Sending

```csharp
asc.SendGameplayEvent(eventTag);

GameplayEventData data = new GameplayEventData(eventTag, instigatorAsc, targetAsc, magnitude, payload);
asc.SendGameplayEvent(data);
```

An event has a **tag**, an optional **instigator** and **target**, optional **magnitude** and optional **Object**.

## Listening

```csharp
asc.RegisterGameplayEvent(deathEventTag, OnDeath);
asc.UnregisterGameplayEvent(deathEventTag, OnDeath);

void OnDeath(GameplayEventData data) { /* ... */ }
```

Always unregister in `OnDisable`/`OnDestroy` if the listener can outlive the character.

## Hierarchical listeners

Listening to a parent tag receives every child event. A listener on `Event.Combat` receives `Event.Combat.Hit` and `Event.Combat.Death`. A listener on a leaf only receives that exact event.

## Typical uses

| Use | Example |
|---|---|
| Notify other systems | `DeathHandler` raises `Event.Death` |
| Trigger abilities | A parry ability triggered by `Event.Hit` |
| Wake a waiting ability | An attack waiting for `Event.Animation.Hit` |
| Animation -> gameplay | An Animation Event sends a gameplay event |
| Attribute reactions | A reaction raises an event when health drops below 25% |

## When not to use events

If you need an answer ("can the character do X?"), use tags and requirements instead. Events are one-way notifications.