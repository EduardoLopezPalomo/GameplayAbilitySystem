using System;
using System.Collections.Generic;
using UnityEngine;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Events;

namespace supercachola.AbilitySystem.Attributes
{
    /// <summary>Watches the attributes of one character and runs its reactions.</summary>
    internal sealed class AttributeReactionRunner
    {
        private sealed class State
        {
            public AttributeReaction Reaction;
            public GameplayAttribute Attribute;
            public bool Met;
            public ActiveEffectHandle Handle = ActiveEffectHandle.None;
        }

        // A reaction can change attributes again (its effect, an event...). This limits the chain.
        private const int MaxPasses = 8;

        private readonly AbilitySystemComponent owner;
        private readonly List<State> states = new List<State>();
        private readonly Action<GameplayAttribute, float> onValueChanged;
        private readonly Action<GameplayAttribute> onLimitsChanged;

        private bool evaluating;
        private bool dirty;

        internal AttributeReactionRunner(AbilitySystemComponent owner, AttributeSet attributes, List<AttributeReaction> reactions)
        {
            this.owner = owner;

            // One delegate for everything, created once.
            onValueChanged = (attribute, oldValue) => Evaluate(attribute);
            onLimitsChanged = attribute => Evaluate(attribute);

            for (int i = 0; i < reactions.Count; i++)
            {
                AttributeReaction reaction = reactions[i];

                if (!attributes.TryGetAttribute(reaction.Attribute, out GameplayAttribute attribute))
                {
                    Debug.LogWarning(
                        $"AbilitySystemComponent '{owner.name}': a reaction was ignored, no attribute set has '{reaction.Attribute.Name}'.",
                        owner);
                    continue;
                }

                // Subscribe once per attribute, even if it has several reactions.
                if (!IsWatched(attribute))
                {
                    attribute.ValueChanged += onValueChanged;
                    attribute.LimitsChanged += onLimitsChanged;
                }

                states.Add(new State { Reaction = reaction, Attribute = attribute });
            }
        }

        /// <summary>Checks every reaction. Used once at the start, after the initial values.</summary>
        internal void EvaluateAll() => Evaluate(null);

        private bool IsWatched(GameplayAttribute attribute)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Attribute == attribute)
                    return true;
            }
            return false;
        }

        // 'only' limits the check to one attribute. Null checks all of them.
        private void Evaluate(GameplayAttribute only)
        {
            // Changes caused by our own reactions are handled by the loop below, not by recursion.
            if (evaluating)
            {
                dirty = true;
                return;
            }

            evaluating = true;

            int pass = 0;
            do
            {
                dirty = false;

                for (int i = 0; i < states.Count; i++)
                {
                    State state = states[i];
                    if (only != null && state.Attribute != only)
                        continue;

                    bool met = state.Reaction.IsMet(state.Attribute);
                    if (met == state.Met)
                        continue;

                    state.Met = met;

                    if (met)
                        Enter(state);
                    else
                        Exit(state);
                }

                // If something changed during the pass, check everything again.
                only = null;
            }
            while (dirty && ++pass < MaxPasses);

            evaluating = false;
        }

        private void Enter(State state)
        {
            AttributeReaction reaction = state.Reaction;

            if (reaction.EffectWhileMet != null)
                owner.TryApplyEffect(reaction.EffectWhileMet, out state.Handle);

            if (reaction.EventOnEnter.isValid)
            {
                owner.SendGameplayEvent(new GameplayEventData(
                    reaction.EventOnEnter, owner, owner, state.Attribute.currentValue));
            }
        }

        private void Exit(State state)
        {
            AttributeReaction reaction = state.Reaction;

            // Does nothing if the handle is empty or the effect is already gone.
            owner.RemoveEffect(state.Handle);
            state.Handle = ActiveEffectHandle.None;

            if (reaction.EventOnExit.isValid)
            {
                owner.SendGameplayEvent(new GameplayEventData(
                    reaction.EventOnExit, owner, owner, state.Attribute.currentValue));
            }
        }
    }
}