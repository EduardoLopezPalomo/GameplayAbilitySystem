# Performance

The package is designed for many characters, no garbage in steady state, and cheap queries.

## Design choices

- **Tags are integers.** No string comparisons at runtime.
- **Tag containers are bitsets.** `HasAny` / `HasAll` are integer operations.
- **Shared definitions, per-character instances.** Hundreds of characters share one asset; only small runtime state is per character.
- **Pooled cues.** Visual and audio cues are reused.
- **No LINQ, no closures** on hot paths.

## Measured allocations

Measured with the Unity Profiler (Deep Profile off), in the editor:

| Operation | GC Alloc |
|---|---|
| Component update / effect tick | **0 B** |
| Apply an instant effect | **0 B** |
| Remove an effect | **0 B** |
| Apply a duration/infinite effect | around 150 B (the runtime instance) |

Effects that are applied often and live a short time are the only source of garbage. 

## Measure it yourself

Don't trust `GC.GetTotalMemory` or `GC.GetAllocatedBytesForCurrentThread` in Unity: they are unreliable on Mono. Use the **Profiler**:

1. Open **Window > Analysis > Profiler** and select the **CPU Usage** module.
2. Switch the bottom view to **Hierarchy**.
3. Sort by the **GC Alloc** column.
4. Wrap the code you want to measure in a named sample:

```csharp
UnityEngine.Profiling.Profiler.BeginSample("MyTest.ApplyEffect");
asc.TryApplyEffect(effect, out _);
UnityEngine.Profiling.Profiler.EndSample();
```

Run it **during Update** (not from a context menu), then search the sample by name in the frame.

## Tips for large crowds

- Prefer **Instant** effects when a permanent change is what you want.
- Use **Percent Of Max** reactions instead of polling health.
- Give cue prefabs a **Prewarm Count** to avoid hitches on first use.
- Keep **Cue Libraries** small and shared.
- Disable the `AbilitySystemComponent` of characters far from the player when they don't need updating. If you need to tick but pause one, use **Ongoing blocked tags**.
