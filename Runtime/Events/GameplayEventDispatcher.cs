using System;
using System.Collections.Generic;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Events
{
    /// <summary>
    /// Delivers the events of one character to the scripts that registered for them.
    /// A listener of a parent tag (Event.Hit) also hears its children (Event.Hit.Critical).
    /// </summary>
    public sealed class GameplayEventDispatcher
    {
        private sealed class Listener
        {
            public GameplayTag Tag;
            public Action<GameplayEventData> Callback;
            public bool Removed;
        }

        private readonly List<Listener> listeners = new List<Listener>();

        // A callback can register, unregister or send another event, so each delivery
        // works on a copy of the list. One copy per nesting level, reused.
        private readonly List<List<Listener>> snapshots = new List<List<Listener>>();
        private int snapshotDepth;

        /// <summary>
        /// Keep the delegate if you want to unregister it later (a lambda can't be unregistered).
        /// </summary>
        public void Register(GameplayTag tag, Action<GameplayEventData> callback)
        {
            if (!tag.isValid || callback == null)
                return;

            listeners.Add(new Listener { Tag = tag, Callback = callback });
        }

        public bool Unregister(GameplayTag tag, Action<GameplayEventData> callback)
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

        internal void Send(GameplayEventData data)
        {
            if (listeners.Count == 0)
                return;

            if (snapshotDepth == snapshots.Count)
                snapshots.Add(new List<Listener>());

            List<Listener> buffer = snapshots[snapshotDepth++];
            buffer.Clear();
            buffer.AddRange(listeners);

            try
            {
                for (int i = 0; i < buffer.Count; i++)
                {
                    Listener listener = buffer[i];

                    // It may have been unregistered by a previous callback.
                    if (!listener.Removed && data.Tag.Matches(listener.Tag))
                        listener.Callback(data);
                }
            }
            finally
            {
                buffer.Clear();
                snapshotDepth--;
            }
        }
    }
}
