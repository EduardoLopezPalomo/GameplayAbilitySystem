# Getting Started

This guide has two parts:

1. **Run the Basic Combat sample** (5 minutes).
2. **Build the same character from scratch**, so you can understand how each piece works.

---

## 1. Run the sample

### Requirements
- Unity 6000.0 or newer.
- The **Input System** package (`com.unity.inputsystem`). The sample uses it for movement and attacks. If it is missing, the sample scripts won't compile and the sample will show a notice.
- Works in URP and HDRP.

### Import
1. Open **Window > Package Manager** and select **Ability System** under *In Project* / *My Assets*.
2. Open the scene `Scene/Sandbox`.
3. Press **Play**.

### Controls
| Action | Input |
|---|---|
| Move | A W S D |
| Attack | Left mouse click |

### What to try
- **Attack the enemy.** The sword sweeps in front of you and applies `DamageEffect` to whatever it hits. A spark cue plays on the target, and the health bar goes down.
- **Walk into the green zone** (`PoisonZone`, variant of EffectZone). It applies the `Poison` effect, which does damage over time and plays a looping poison cue. Leave and enter again to stack the effect up to 3 stacks.
- **Walk into the purple zone** (`AttackBuffZone`, variant of EffectZone). It applies the `AttackBuff` effect, which add a +10 to the attack attribute so the damage to the enemy is greater.
- **Spam attacks.** Attacking again before the 1 second cooldown ends does nothing: `CooldownAttack` blocks the ability.
- **Kill the enemy.** When health reaches 0, the `Dead` tag is granted, `DeathHandler` sends an event and destroys the character after a delay.
- **Open the debugger:** (**Window > Ability System > Debugger**). In Play mode to see its attributes, active effects, granted tags and active cues live.

---

## 2. How the sample is built

Everything the sample uses lives in `GameplayAbilitySystem/`. All of it is data (ScriptableObjects) except the scripts.

```
GameplayAbilitySystem
├─ Tags/TagSource             Tags used by the sample
├─ AttributeDefinitions/      Attack, Defense, Health, MaxHealth, Resistance
├─ AttributeSets/CombatSet    The five attributes above, as one set
├─ Effects/                   AttackBuff, CooldownAttack, DamageEffect, Dead, Poison
├─ Executions/DamageExecution How damage is calculated
├─ Abilities/AttackAbility    Left click attack
└─ Cues/                      Poison, Spark, and the CueLibrary DefaultCues
```

### Step 1. Tags
Tags are the vocabulary of the system. Instead of strings, you use tags such as `State.Dead` or `Event.Death`.

1. Create **Gameplay Tag Settings** asset (`Window > Ability System > Tag Manager`) and click create *GameplayTagSettings* in `Assets/Resources`. If the asset is already created use the manager to add and delete tags.
2. If the sample is imported, a `TagSource` will create the *GameplayTagSettings* with the tags that the sample needs. `TagSource` is only for the sample, use the tag manager for everything else.

### Step 2. Attributes
An attribute is a number with a base value, a current value, and limits.

| Attribute | Base | Min | Max |
|---|---|---|---|
| Attack | 10 | 0 | 100 |
| Defense | 5 | 0 | 100 |
| MaxHealth | 100 | 0 | 9999 |
| Health | 100 | 0 | **MaxHealth** |
| Resistance | 0 | 0 | 100 |

`Health` uses a **dynamic max**: its maximum is the current value of another attribute (`MaxHealth`), set through the *Max Attribute Tag* field. If `MaxHealth` changes, `Health` is clamped automatically.

The five definitions are grouped in the **CombatSet** attribute set. A set is the "character sheet" you assign to a character.

### Step 3. Effects
Effects are how anything changes in the system: damage, buffs, cooldowns, states.

| Effect | Type | What it does |
|---|---|---|
| `DamageEffect` | Instant | Runs `DamageExecution` and plays the `Spark` cue |
| `AttackBuff` | Infinite | Adds +10 to `Attack` until it's removed |
| `CooldownAttack` | Duration (1 s) | Grants a cooldown tag. `AttackAbility` uses it as its cooldown |
| `Poison` | Duration (5 s) | Periodic damage, value set by the caller, stacks up to 3, plays the `Poison` cue |
| `Dead` | Infinite | Only grants the dead tag. Other systems react to the tag |

Good to know:
- An effect that only grants a tag is perfectly valid. Tags are state.
- `Poison` uses **Set By Caller**: the number isn't stored in the asset (`Default` is a value in case the caller doesn't set it). `PoisonZone` provides it.

### Step 4. Executions
`DamageExecution` is a ScriptableObject that calculates damage from the attacker's `Attack` and the target's `Defense`/`Resistance`, with a chance of a critical hit, then reduces `Health`. See `Scripts/Executions/DamageExecution.cs`

### Step 5. Cues
Cues are visuals and audio, kept separate from gameplay logic.

| Cue | Mode | Notes |
|---|---|---|
| `Spark` | One shot (one execute) | Lifetime 1 s, attached to the target |
| `Poison` | Persistent | Lives while the effect is active, attached to the target |

Both are registered in the **DefaultCues** library. The character's component points to that library, and cues are played by **tag**, so you can swap visuals without touching gameplay.
Cue prefabs are pooled: no instantiate/destroy at runtime, `Prewarn Count` specifies how many objects the pool initializes at startup (if needed, it creates more objects dynamically).

### Step 6. The ability
`AttackAbility` is an ability asset with:
- **Cooldown:** `CooldownAttack`.
- **Input binding:** left click.
- **Behaviour:** `Scripts/Abilities/Instances/AttackAbility.cs`. When activated, it sweeps the sword (`Sword.cs` defines the start and end transforms) in front of the player. Any object tagged Enemy on the Enemy layer receives `DamageEffect`.

The data (`AttackAbilityDefinition`) is shared by every character. Each character gets its own runtime instance, so cooldowns and state never leak between characters.

### Step 7. The characters
Both **Player** and **Enemy** prefabs have an `AbilitySystemComponent` with:
- Attribute set: `CombatSet`.
- Cue library: `DefaultCues`.

Extra scripts:
- **Player:** `MovementPlayer` (movement from the Input System).
- **Enemy:** `DeathHandler` (reacts to the dead tag, raises an event, destroys the character after a delay).
- **UI:** `UiGasBridge` listens to attribute changes (`Health` and `MaxHealth` by tag) and fills the bar with `Health / MaxHealth`.

### Step 8. The world
`EffectZones` applies an effect when the player enters its trigger and can remove it on exit. It can pass a **Set By Caller** value. `PoisonZone` and `AttackBuffZone`.

---

## 3. Make your own character

1. Create **GameplayTagSettings** with the `TagManager` and add your tags.
2. Create **Attribute Definitions** and group them into an **Attribute Set**.
3. Create the **Effects** your game needs (a damage effect, a heal, a buff).
4. Create an **Ability** and assign cost, cooldown and effects.
5. Add an `AbilitySystemComponent` to your prefab, assign the attribute set, the ability and (optionally) a cue library.
6. Grant the ability and trigger it from code:

```csharp
asc.TryActivateAbility(attackAbilityTag);
asc.TryApplyEffect(damageEffectDefinition, out _);
```

7. Open the **Ability Debugger** in Play mode and watch what happens. The Ability Debugger has an option called `Default Target`.
The **Ability Debugger** shows the target by default that has that Tag(unity tag) or Layer. To inspect other characters, use the Next button.

## Next steps
- [Tags](Tags.md), [Attributes](Attributes.md), [Effects](Effects.md), [Abilities](Abilities.md)
- [Gameplay Events](Events.md), [Gameplay Cues](Cues.md), [Attribute Reactions](Reactions.md)
- [Debugging](Debugging.md), [Performance](Performance.md)