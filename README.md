# Ability System

A data-driven gameplay ability system for Unity, inspired by Unreal's Gameplay Ability System.
Define abilities, effects, attributes and cues as ScriptableObjects, share them between any number
of characters, and let the runtime keep each character's own state.

## Features

- **Gameplay Tags**: hierarchical tags (`State.Burning.Fire`) with a Tag Manager window and Inspector
  pickers. A child tag matches its parents, never the other way around. Queries are bit operations.
- **Attributes**: base and current value, min and max (the max can follow another attribute),
  and Add / Multiply / Override modifiers. Attribute sets are assets.
- **Gameplay Effects**: Instant, Duration and Infinite, with periodic execution. Magnitudes can be fixed,
  based on an attribute of the instigator or target, or sent by the caller. Custom executions for
  damage formulas. Tags granted, required and blocked, immunity, cleanse, ongoing requirements
  and stacking policies (independent, refresh duration, stack charges, ignore, per target or per instigator).
- **Gameplay Abilities**: cost and cooldown are effects, activation tags, cancel-by-tag, abilities triggered
  by events, and waits (event, delay, tag) that clean up when the ability ends.
- **Gameplay Events**: tagged events with an instigator, a target, a magnitude and a payload.
  Listening to a parent tag also receives its children. Animation Event relay included.
- **Gameplay Cues**: particles and sounds triggered by effects, decoupled from the logic and pooled.
- **Attribute Reactions**: "when Health reaches 0, apply `Dead` and send `Event.Death`", without code.
- **Debugging tools**: a Debugger window that shows attributes, abilities, tags, effects and cues of any
  character while the game runs.
- **Optional Input System binder** for activating abilities from input actions.
- **Tested and measured**: 40+ automated EditMode tests. Ticking, instant effects and removing effects
  allocate no managed memory; applying a timed effect allocates about 150 bytes.

## Requirements

- Unity 6000.0 or newer.
- The Input System package is optional. It is only needed for the input binder and the Basic Combat sample.
  Without it, the sample's scripts are skipped and its scene shows missing-script warnings.
  If you don't use the Input System, don't import the `Samples` folder, or delete it after importing.

## Installation

**Package Manager:** Package Manager > My Assets > Ability System > Download > Import

## Quick start

1. Open **Window > Ability System > Tag Manager** and create your tags.
2. Create a few assets with the **Create > Ability System** menu: an attribute set, an effect and an ability.
3. Add an **Ability System Component** to a GameObject, assign the attribute set and the starting abilities.
4. From code:

```csharp
using supercachola.AbilitySystem;
using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Effects;

public class Example : UnityEngine.MonoBehaviour
{
    [UnityEngine.SerializeField] private AbilitySystemComponent self;
    [UnityEngine.SerializeField] private AbilitySystemComponent enemy;
    [UnityEngine.SerializeField] private GameplayEffectDefinition burn;

    [UnityEngine.ContextMenu("Cast Fireball")]
    public void CastFireball() => self.TryActivateAbility(GameplayTag.Request("Ability.Fireball"));

    [UnityEngine.ContextMenu("Ignite")]
    public void Ignite() => self.TryApplyEffectTo(enemy, burn, out _);
}
```

For a complete walkthrough, import the **Basic Combat** sample from the Package Manager
(*Samples > Import*) and read `Documentation/GettingStarted.md`.

## Documentation

Start with **[Getting Started](Documentation/GettingStarted.md)**: it runs and then rebuilds the Basic Combat sample step by step.

| Topic | |
|---|---|
| [Tags](Documentation/Tags.md) | Hierarchical tags, containers |
| [Attributes](Documentation/Attributes.md) | Base/current values, limits, dynamic max, modifiers |
| [Effects](Documentation/Effects.md) | Instant/duration/infinite, stacking, refresh duration, executions, requirements |
| [Abilities](Documentation/Abilities.md) | Cost, cooldown, activation rules, events and waits |
| [Events](Documentation/Events.md) | Tag-based gameplay events |
| [Cues](Documentation/Cues.md) | Pooled visual and audio feedback |
| [Reactions](Documentation/Reactions.md) | Threshold-driven effects and events |
| [Debugging](Documentation/Debugging.md) | Debugger window, warnings, tests |
| [Performance](Documentation/Performance.md) | Allocation profile and how to measure it |
