using System;
using System.Collections.Generic;
using supercachola.AbilitySystem.Events;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Abilities
{
    /// <summary>
    /// What an active ability is waiting for: events, time and tag changes.
    /// Everything is cleared when the ability ends, so abilities never have to unsubscribe.
    /// </summary>
    internal sealed class AbilityWaits
    {
        private struct EventWait
        {
            public GameplayTag Tag;
            public Action<GameplayEventData> Callback;
            public bool Once;
        }

        private struct DelayWait
        {
            public float Remaining;
            public Action Callback;
        }

        private sealed class TagWait
        {
            public GameplayTag Tag;
            public bool ToBePresent;
            public bool Once;
            public Action Callback;
            public Action<GameplayTag, bool> Handler;
        }

        // Cached: converting a method to a delegate would create a new one on every call.
        private static readonly Predicate<EventWait> IsEmptyEvent = w => w.Callback == null;
        private static readonly Predicate<DelayWait> IsEmptyDelay = w => w.Callback == null;

        private readonly GameplayAbility ability;
        private readonly List<EventWait> events = new List<EventWait>();
        private readonly List<DelayWait> delays = new List<DelayWait>();
        private readonly List<TagWait> tagWaits = new List<TagWait>();

        // Grows every time everything is cleared, so a loop can notice that the ability ended (or restarted) under it.
        private int generation;
        private int eventDepth;

        public AbilityWaits(GameplayAbility ability)
        {
            this.ability = ability;
        }

        public void AddEvent(GameplayTag tag, Action<GameplayEventData> callback, bool once)
        {
            events.Add(new EventWait { Tag = tag, Callback = callback, Once = once });
        }

        public void AddDelay(float seconds, Action callback)
        {
            delays.Add(new DelayWait { Remaining = seconds, Callback = callback });
        }

        public void AddTag(GameplayTag tag, bool toBePresent, Action callback, bool once)
        {
            var wait = new TagWait { Tag = tag, ToBePresent = toBePresent, Once = once, Callback = callback };

            wait.Handler = (changedTag, isPresent) =>
            {
                if (isPresent != wait.ToBePresent || !ability.IsActive)
                    return;

                if (wait.Once)
                    RemoveTagWait(wait);

                wait.Callback();
            };

            tagWaits.Add(wait);
            ability.Owner.RegisterTagEvent(tag, wait.Handler);
        }

        public void DispatchEvent(GameplayEventData data)
        {
            if (events.Count == 0)
                return;

            int gen = generation;

            // Waits added by a callback only hear the next events.
            int count = events.Count;

            eventDepth++;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    // A callback may have ended the ability (everything was cleared).
                    if (gen != generation || !ability.IsActive)
                        return;

                    EventWait wait = events[i];
                    if (wait.Callback == null || !data.Tag.Matches(wait.Tag))
                        continue;

                    // Emptied (not removed) so the indexes stay valid while callbacks run.
                    if (wait.Once)
                        events[i] = default;

                    wait.Callback(data);
                }
            }
            finally
            {
                // Only the outermost delivery cleans up the empty slots.
                if (--eventDepth == 0 && gen == generation)
                    events.RemoveAll(IsEmptyEvent);
            }
        }

        public void Tick(float deltaTime)
        {
            if (delays.Count == 0)
                return;

            int gen = generation;
            int count = delays.Count;

            for (int i = 0; i < count; i++)
            {
                if (gen != generation || !ability.IsActive)
                    return;

                DelayWait wait = delays[i];
                if (wait.Callback == null)
                    continue;

                wait.Remaining -= deltaTime;
                if (wait.Remaining > 0f)
                {
                    delays[i] = wait;
                    continue;
                }

                delays[i] = default;
                wait.Callback();
            }

            if (gen == generation)
                delays.RemoveAll(IsEmptyDelay);
        }

        public void Clear()
        {
            generation++;
            events.Clear();
            delays.Clear();

            for (int i = 0; i < tagWaits.Count; i++)
                ability.Owner.UnregisterTagEvent(tagWaits[i].Tag, tagWaits[i].Handler);

            tagWaits.Clear();
        }

        private void RemoveTagWait(TagWait wait)
        {
            ability.Owner.UnregisterTagEvent(wait.Tag, wait.Handler);
            tagWaits.Remove(wait);
        }
    }
}
