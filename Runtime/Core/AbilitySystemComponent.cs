using System.Collections.Generic;
using System;
using UnityEngine;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Events;
using supercachola.AbilitySystem.Cues;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("supercachola.AbilitySystem.Tests")]
[assembly: InternalsVisibleTo("supercachola.AbilitySystem.Editor")]

namespace supercachola.AbilitySystem
{
    /// <summary>
    /// Puts together the attributes, tags, effects and abilities of one character.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Ability System/Ability System Component")]
    public class AbilitySystemComponent : MonoBehaviour
    {
        [Header("Attributes")]
        [Tooltip("Sets of attributes this character has.")]
        [SerializeField] private List<AttributeSetDefinition> attributeSets = new List<AttributeSetDefinition>();

        [Tooltip("Overrides the starting value of some attributes, only for this character.")]
        [SerializeField] private List<AttributeInitialValue> initialValues = new List<AttributeInitialValue>();
        
        [Tooltip("Optional. The reactions of each attribute")]
        [SerializeField] private List<AttributeReaction> reactions = new List<AttributeReaction>();
        
        [Header("Starting Effects")]
        [Tooltip("Effects applied when the character starts.")]
        [SerializeField] private List<GameplayEffectDefinition> startingEffects = new List<GameplayEffectDefinition>();
        
        [Header("Abilities")]
        [Tooltip("Abilities the character has from the start.")]
        [SerializeField] private List<GameplayAbilityDefinition> startingAbilities = new List<GameplayAbilityDefinition>();
        
        [Header("Cues")]
        [Tooltip("Optional. Without a library this character plays no cues (servers, tests...).")]
        [SerializeField] private GameplayCueLibrary cueLibrary;
        
        [Tooltip("Writes every gameplay event this character receives in the Console. For debugging.")]
        [SerializeField] private bool logGameplayEvents;
        
        
        private AttributeSet attributes;
        private GameplayTagCountContainer ownedTags;
        private ActiveEffectContainer effects;
        private AbilityContainer abilities;
        private GameplayEventDispatcher gameplayEvents;
        private GameplayCueRunner cues;
        private AttributeReactionRunner reactionRunner;
        private bool initialized;
        
#if UNITY_EDITOR
        private static readonly List<AbilitySystemComponent> instances = new List<AbilitySystemComponent>();
        
        /// <summary>Enabled Ability System components in the loaded scenes. For tools and debugging: don't modify it.</summary>
        internal static IReadOnlyList<AbilitySystemComponent> Instances => instances;

        private void OnEnable() => instances.Add(this);

        private void OnDisable() => instances.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstances() => instances.Clear();
#endif

        public AttributeSet Attributes
        {
            get { EnsureInitialized(); return attributes; }
        }

        public GameplayTagCountContainer OwnedTags
        {
            get { EnsureInitialized(); return ownedTags; }
        }

        public ActiveEffectContainer Effects
        {
            get { EnsureInitialized(); return effects; }
        }
        
        public AbilityContainer Abilities
        {
            get { EnsureInitialized(); return abilities; }
        }
        
        public GameplayCueRunner Cues
        {
            get { EnsureInitialized(); return cues; }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            effects.Tick(deltaTime);
            abilities.Tick(deltaTime);
        }
        
        private void OnDestroy()
        {
            // Lets the running abilities clean up (OnEnd).
            // Their components may already be destroyed at this point, so OnEnd should check for null.
            abilities?.CancelAll();
            cues?.RemoveAllActive();
        }
        
        //---------- Public ----------
        
        /// <summary>Creates a spec of the effect. This character is the instigator.</summary>
        public GameplayEffectSpec MakeSpec(GameplayEffectDefinition effect)
        {
            return new GameplayEffectSpec(effect, this);
        }
        
        /// <summary>Applies an effect to this character. This character is also the instigator.</summary>
        public ActiveEffectHandle ApplyEffect(GameplayEffectDefinition effect)
        {
            TryApplyEffect(effect, out ActiveEffectHandle handle);
            return handle;
        }

        public bool TryApplyEffect(GameplayEffectDefinition effect, out ActiveEffectHandle handle)
        {
            EnsureInitialized();
            return effects.TryApplySpec(MakeSpec(effect), out handle);
        }
        
        /// <summary>Applies an effect to another character. This character is the instigator.</summary>
        public bool TryApplyEffectTo(AbilitySystemComponent target, GameplayEffectDefinition effect, out ActiveEffectHandle handle)
        {
            handle = ActiveEffectHandle.None;

            if (target == null)
                return false;

            return target.TryApplySpec(MakeSpec(effect), out handle);
        }

        /// <summary>Applies a spec to this character. The instigator travels inside the spec.</summary>
        public bool TryApplySpec(in GameplayEffectSpec spec, out ActiveEffectHandle handle)
        {
            EnsureInitialized();
            return effects.TryApplySpec(spec, out handle);
        }

        public bool RemoveEffect(ActiveEffectHandle handle)
        {
            EnsureInitialized();
            return effects.RemoveEffect(handle);
        }
        
        /// <summary>Removes the active effects that match any of these tags (a cleanse). Returns how many.</summary>
        public int RemoveEffectsWithTags(GameplayTagContainer tags)
        {
            EnsureInitialized();
            return effects.RemoveEffectsWithTags(tags);
        }

        /// <summary>Calls the callback when the tag (or a child of it) appears or disappears. Keep the delegate to unregister it.</summary>
        public void RegisterTagEvent(GameplayTag tag, Action<GameplayTag, bool> callback)
        {
            EnsureInitialized();
            ownedTags.RegisterTagEvent(tag, callback);
        }

        public bool UnregisterTagEvent(GameplayTag tag, Action<GameplayTag, bool> callback)
        {
            EnsureInitialized();
            return ownedTags.UnregisterTagEvent(tag, callback);
        }

        public bool TryGetAttribute(GameplayTag tag, out GameplayAttribute attribute)
        {
            EnsureInitialized();
            return attributes.TryGetAttribute(tag, out attribute);
        }

        public bool HasTag(GameplayTag tag)
        {
            EnsureInitialized();
            return ownedTags.HasTag(tag);
        }

        public bool HasAnyTag(GameplayTagContainer container)
        {
            EnsureInitialized();
            return ownedTags.HasAny(container);
        }

        public bool HasAllTags(GameplayTagContainer container)
        {
            EnsureInitialized();
            return ownedTags.HasAll(container);
        }

        public bool HasExactTag(GameplayTag tag)
        {
            EnsureInitialized();
            return ownedTags.HasTagExact(tag);
        }
        
        public GameplayAbility GrantAbility(GameplayAbilityDefinition definition)
        {
            EnsureInitialized();
            return abilities.Grant(definition);
        }

        public bool TryActivateAbility(GameplayTag abilityTag)
        {
            EnsureInitialized();
            return abilities.TryActivate(abilityTag);
        }
        
        /// <summary>Sends an event with this character as instigator and target.</summary>
        public void SendGameplayEvent(GameplayTag eventTag)
        {
            SendGameplayEvent(new GameplayEventData(eventTag, this, this));
        }

        /// <summary>
        /// Sends an event to this character: to the scripts that registered for it, to its active abilities, and to the abilities that trigger on it.
        /// </summary>
        public void SendGameplayEvent(GameplayEventData data)
        {
            EnsureInitialized();

            if (!data.Tag.isValid)
            {
                Debug.LogWarning($"AbilitySystemComponent '{name}': an event without tag was ignored.", this);
                return;
            }

            if (logGameplayEvents)
            {
                string from = data.Instigator != null ? data.Instigator.name : "-";
                Debug.Log($"[{name}] event '{data.Tag.Name}' from {from}, magnitude {data.Magnitude}", this);
            }

            gameplayEvents.Send(data);
            abilities.SendGameplayEvent(data);
        }

        /// <summary>Calls the callback for the events of this tag (and its children). Keep the delegate to unregister it.</summary>
        public void RegisterGameplayEvent(GameplayTag tag, Action<GameplayEventData> callback)
        {
            EnsureInitialized();
            gameplayEvents.Register(tag, callback);
        }

        public bool UnregisterGameplayEvent(GameplayTag tag, Action<GameplayEventData> callback)
        {
            EnsureInitialized();
            return gameplayEvents.Unregister(tag, callback);
        }

        public void ExecuteCue(GameplayTag tag, float magnitude = 0f)
        {
            Cues.Execute(tag, this, magnitude);
        }

        public void AddCue(GameplayTag tag)
        {
            Cues.Add(tag, this);
        }

        public void RemoveCue(GameplayTag tag)
        {
            Cues.Remove(tag);
        }
        
        // ---------- Internal ----------

        // Awake runs it, but another script may need the component before our Awake runs.
        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            attributes = new AttributeSet(attributeSets);
            ownedTags = new GameplayTagCountContainer();
            effects = new ActiveEffectContainer(attributes, ownedTags);
            cues = new GameplayCueRunner(this, cueLibrary);
            effects.Cues = cues;
            abilities = new AbilityContainer(this);
            gameplayEvents = new GameplayEventDispatcher();

            ApplyInitialValues();
            ApplyStartingEffects();
            GrantStartingAbilities();
            
            reactionRunner = new AttributeReactionRunner(this, attributes, reactions);
            reactionRunner.EvaluateAll();
            
            if (Application.isPlaying && cueLibrary != null)
                cueLibrary.PrewarmAll();
        }

        private void ApplyInitialValues()
        {
            // Maximums first: a value is clamped to its maximum, so the maximum must already be set.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < initialValues.Count; i++)
                {
                    AttributeInitialValue initial = initialValues[i];

                    if (!attributes.TryGetAttribute(initial.Attribute, out GameplayAttribute attribute))
                    {
                        if (pass == 1)
                            Debug.LogWarning($"AbilitySystemComponent '{name}': initial value ignored, no attribute set has '{initial.Attribute.Name}'.",this);
                        continue;
                    }

                    if (attribute.IsMaxSource != (pass == 0))
                        continue;

                    attribute.SetBaseValue(initial.BaseValue);
                }
            }
        }

        private void ApplyStartingEffects()
        {
            for (int i = 0; i < startingEffects.Count; i++)
            {
                if (startingEffects[i] != null)
                    effects.TryApplySpec(MakeSpec(startingEffects[i]), out _);
            }
        }
        
        private void GrantStartingAbilities()
        {
            for (int i = 0; i < startingAbilities.Count; i++)
            {
                if (startingAbilities[i] != null)
                    abilities.Grant(startingAbilities[i]);
            }
        }
       
    }   
}
