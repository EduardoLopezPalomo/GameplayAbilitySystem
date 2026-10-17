using System.Collections.Generic;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Cues
{
    /// <summary>Plays the cues of one character and counts the lasting ones.</summary>
    public sealed class GameplayCueRunner
    {
        private struct ActiveCue
        {
            public GameplayTag Tag;
            public int Count;
            public GameplayCueDefinition Definition;
            public object State;
        }

        private readonly AbilitySystemComponent owner;
        private readonly GameplayCueLibrary library;
        private readonly List<ActiveCue> active = new List<ActiveCue>();

        public int ActiveCount => active.Count;
        public GameplayTag GetActiveTagAt(int index) => active[index].Tag;
        public int GetActiveCountAt(int index) => active[index].Count;

        internal GameplayCueRunner(AbilitySystemComponent owner, GameplayCueLibrary library)
        {
            this.owner = owner;
            this.library = library;
        }

        // ---------- One-time ----------

        public void Execute(GameplayTag tag, AbilitySystemComponent instigator = null, float magnitude = 0f)
        {
            if (!TryGetDefinition(tag, out GameplayCueDefinition definition))
                return;

            var parameters = new GameplayCueParameters(tag, instigator, owner, magnitude);
            definition.OnExecuted(in parameters);
        }

        public void ExecuteEach(IReadOnlyList<GameplayTag> tags, AbilitySystemComponent instigator, float magnitude)
        {
            for (int i = 0; i < tags.Count; i++)
                Execute(tags[i], instigator, magnitude);
        }

        // ---------- Lasting ----------

        public void Add(GameplayTag tag, AbilitySystemComponent instigator = null)
        {
            int index = IndexOf(tag);
            if (index >= 0)
            {
                ActiveCue existing = active[index];
                existing.Count++;
                active[index] = existing;
                return;
            }

            if (!TryGetDefinition(tag, out GameplayCueDefinition definition))
                return;

            var parameters = new GameplayCueParameters(tag, instigator, owner, 0f);
            object state = definition.OnAdded(in parameters);

            active.Add(new ActiveCue { Tag = tag, Count = 1, Definition = definition, State = state });
        }

        public void AddEach(IReadOnlyList<GameplayTag> tags, AbilitySystemComponent instigator)
        {
            for (int i = 0; i < tags.Count; i++)
                Add(tags[i], instigator);
        }

        public void Remove(GameplayTag tag)
        {
            int index = IndexOf(tag);
            if (index < 0)
                return;

            ActiveCue cue = active[index];
            if (--cue.Count > 0)
            {
                active[index] = cue;
                return;
            }

            // It leaves the list before the cue runs, so nothing it does can see it half removed.
            active.RemoveAt(index);

            var parameters = new GameplayCueParameters(cue.Tag, null, owner, 0f);
            cue.Definition.OnRemoved(in parameters, cue.State);
        }

        public void RemoveEach(IReadOnlyList<GameplayTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
                Remove(tags[i]);
        }

        /// <summary>Ends every lasting cue. Used when the character is destroyed.</summary>
        internal void RemoveAllActive()
        {
            while (active.Count > 0)
            {
                int last = active.Count - 1;
                ActiveCue cue = active[last];
                active.RemoveAt(last);

                var parameters = new GameplayCueParameters(cue.Tag, null, owner, 0f);
                cue.Definition.OnRemoved(in parameters, cue.State);
            }
        }

        // ---------- Internal ----------

        private int IndexOf(GameplayTag tag)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Tag == tag)
                    return i;
            }
            return -1;
        }

        private bool TryGetDefinition(GameplayTag tag, out GameplayCueDefinition definition)
        {
            definition = null;
            return library != null && library.TryGetCue(tag, out definition);
        }
    }
}
