using System;
using System.Collections.Generic;

namespace supercachola.AbilitySystem.Tags
{
    /// <summary>
    /// Tags of a character that can have multiple sources.
    /// For example, if a character has two sources of the same tag, it will be counted twice, and the tag will remain active until both sources are removed.
    /// </summary>
    public sealed class GameplayTagCountContainer
    {
        private sealed class Listener
        {
            public GameplayTag Tag;
            public Action<GameplayTag, bool> Callback;
            public bool IsPresent;
            public bool Removed;
        }
        
        private readonly struct Pending
        {
            public readonly Listener Listener;
            public readonly bool IsPresent;

            public Pending(Listener listener, bool isPresent)
            {
                Listener = listener;
                IsPresent = isPresent;
            }
        }
        
        private readonly GameplayTagContainer present = new GameplayTagContainer();
        private readonly List<GameplayTag> scratch = new List<GameplayTag>();
        
        private readonly List<Listener> listeners = new List<Listener>();

        // One list per nesting level: a callback can add or remove tags, which notifies again.
        private readonly List<List<Pending>> pendingPool = new List<List<Pending>>();
        private int pendingDepth;
        
        
        private int[] ids = new int[8];
        private int[] counts = new int[8];
        private int used;
        
        /// <summary>Raised after some tag appeared or disappeared (once per Add/Remove call).</summary>
        public event Action TagsChanged;

        // ---------- Add and remove ----------

        /// <summary>Adds a source to the tag, returns true if the tag has just appeared.</summary>
        public bool AddTag(GameplayTag tag)
        {
            bool appeared = AddCore(tag);
            if (appeared)
                NotifyChanged();
            return appeared;
        }

        /// <summary>Removes a source of the tag, returns true if the tag has just disappeared.</summary>
        public bool RemoveTag(GameplayTag tag)
        {
            bool disappeared = RemoveCore(tag);
            if (disappeared)
                NotifyChanged();
            return disappeared;
        }

        public void AddTags(GameplayTagContainer tags)
        {
            bool changed = false;

            scratch.Clear();
            tags.AppendTagsTo(scratch);
            for (int i = 0; i < scratch.Count; i++)
                changed |= AddCore(scratch[i]);
            scratch.Clear();

            // Notified at the end, so listeners see all the tags already added.
            if (changed)
                NotifyChanged();
        }

        public void RemoveTags(GameplayTagContainer tags)
        {
            bool changed = false;

            scratch.Clear();
            tags.AppendTagsTo(scratch);
            for (int i = 0; i < scratch.Count; i++)
                changed |= RemoveCore(scratch[i]);
            scratch.Clear();

            if (changed)
                NotifyChanged();
        }
        
        // ---------- Events ----------

        /// <summary>
        /// Calls "callback" when HasTag(tag) changes: the tag (or a child of it) appears or disappears.
        /// The callback receives the tag and true if it is now present.
        /// Keep the delegate if you want to unregister it later (a lambda can't be unregistered).
        /// </summary>
        public void RegisterTagEvent(GameplayTag tag, Action<GameplayTag, bool> callback)
        {
            if (!tag.isValid || callback == null)
                return;

            listeners.Add(new Listener
            {
                Tag = tag,
                Callback = callback,
                IsPresent = present.HasTag(tag)
            });
        }

        public bool UnregisterTagEvent(GameplayTag tag, Action<GameplayTag, bool> callback)
        {
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i].Tag == tag && listeners[i].Callback == callback)
                {
                    listeners[i].Removed = true;
                    listeners.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        // ---------- Queries ----------

        /// <summary>How many sources that exact tag has</summary>
        public int GetCount(GameplayTag tag)
        {
            if (!tag.isValid)
                return 0;

            int index = IndexOf(tag.Id);
            return index >= 0 ? counts[index] : 0;
        }

        /// <summary>Adds the tags that have at least one source to "results", without allocating.</summary>
        public void AppendTagsTo(List<GameplayTag> results)
        {
            present.AppendTagsTo(results);
        }

        public bool HasTag(GameplayTag tag) => present.HasTag(tag);
        public bool HasTagExact(GameplayTag tag) => present.HasTagExact(tag);
        public bool HasAny(GameplayTagContainer other) => present.HasAny(other);
        public bool HasAll(GameplayTagContainer other) => present.HasAll(other);

        /// <summary>
        /// True if any tag of "tags" is equal to one of my tags, or a child of one of them.
        /// It is the opposite direction of HasAny: with "Effect.Poison" in this container,
        /// something tagged "Effect.Poison.Weak" is covered.
        /// </summary>
        public bool CoversAny(GameplayTagContainer tags) => tags.HasAny(present);
        
        // ---------- Internal ----------

        private bool AddCore(GameplayTag tag)
        {
            if (!tag.isValid)
                return false;

            int index = IndexOf(tag.Id);
            if (index >= 0)
            {
                counts[index]++;
                return false;
            }

            if (used == ids.Length)
            {
                Array.Resize(ref ids, used * 2);
                Array.Resize(ref counts, used * 2);
            }

            ids[used] = tag.Id;
            counts[used] = 1;
            used++;

            present.AddTag(tag);
            return true;
        }

        private bool RemoveCore(GameplayTag tag)
        {
            if (!tag.isValid)
                return false;

            int index = IndexOf(tag.Id);
            if (index < 0)
                return false;

            if (--counts[index] > 0)
                return false;

            // No sources left: fill the gap with the last entry.
            used--;
            ids[index] = ids[used];
            counts[index] = counts[used];

            present.RemoveTag(tag);
            return true;
        }

        // The state is already updated when this runs.
        private void NotifyChanged()
        {
            if (listeners.Count > 0)
            {
                List<Pending> pending = RentPending();
                try
                {
                    // First find who changed, then call. A callback can change the tags again.
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        Listener listener = listeners[i];
                        bool now = present.HasTag(listener.Tag);

                        if (now != listener.IsPresent)
                        {
                            listener.IsPresent = now;
                            pending.Add(new Pending(listener, now));
                        }
                    }

                    for (int i = 0; i < pending.Count; i++)
                    {
                        Pending p = pending[i];
                        if (!p.Listener.Removed)
                            p.Listener.Callback(p.Listener.Tag, p.IsPresent);
                    }
                }
                finally
                {
                    ReturnPending(pending);
                }
            }

            TagsChanged?.Invoke();
        }

        private List<Pending> RentPending()
        {
            if (pendingDepth == pendingPool.Count)
                pendingPool.Add(new List<Pending>());

            List<Pending> list = pendingPool[pendingDepth++];
            list.Clear();
            return list;
        }

        private void ReturnPending(List<Pending> list)
        {
            list.Clear();
            pendingDepth--;
        }

        private int IndexOf(int id)
        {
            for (int i = 0; i < used; i++)
            {
                if (ids[i] == id)
                    return i;
            }
            return -1;
        }
    }
}
