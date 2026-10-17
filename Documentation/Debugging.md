# Debugging

## Ability Debugger

Open it from `Window > Ability System > Debugger` and enter Play mode.

**Choosing a character**
- **Default Target**: the character selected automatically when Play starts (by Unity tag or layer).
- **Player**: jumps to the default character.
- **Next**: shows the next character in the list.

**What you see for the selected character**
- **Attributes**: base and current values, and max for attributes with a dynamic maximum.
- **Active effects**: type, remaining time, stack count.
- **Granted tags**: with their counts.
- **Active cues**.
- **Abilities** and their state.

Use it to answer the usual questions: "why can't I activate this ability?" (look at the tags), "why is my damage so low?" (look at the attributes and modifiers), "why does the effect not apply?" (look at tags and immunity).

## Console warnings

The system warns about configuration mistakes instead of failing silently, for example:

- An effect that "has nothing to do" (no modifiers, tags, immunity, execution or cues).
- Attribute max cycles.
- Missing tags.

Read the warning text: it names the asset involved.

## The `Instances` property

`AbilitySystemComponent.Instances` lists the enabled components in loaded scenes, for editor tools. It exists **only in the Editor** and is not available in builds. Don't use it from runtime code.

## Tests

The package includes EditMode tests. They are visible in the Test Runner.

## Tips

- Cache tags; if one is missing, you'll see a warning with its name.
- Prefer attribute `ValueChanged` events to polling in `Update`.
- To test an effect quickly, apply it from a context menu on a test component and watch the debugger.