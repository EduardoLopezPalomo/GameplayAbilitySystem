using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;
using Object = UnityEngine.Object;

namespace supercachola.AbilitySystem.Tests
{
    /// <summary>Sets the private serialized fields of assets, the same ones the Inspector fills.</summary>
    public static class ReflectionExtensions
    {
        public static FieldInfo FindField(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                    return field;
            }

            throw new MissingFieldException(type.Name, name);
        }

        public static T Set<T>(this T target, string field, object value) where T : class
        {
            FindField(target.GetType(), field).SetValue(target, value);
            return target;
        }

        /// <summary>Fills a List or array field.</summary>
        public static T SetList<T>(this T target, string field, params object[] items) where T : class
        {
            FieldInfo info = FindField(target.GetType(), field);
            Type type = info.FieldType;

            if (type.IsArray)
            {
                Array array = Array.CreateInstance(type.GetElementType(), items.Length);
                for (int i = 0; i < items.Length; i++)
                    array.SetValue(items[i], i);

                info.SetValue(target, array);
            }
            else
            {
                var list = (IList)Activator.CreateInstance(type);
                for (int i = 0; i < items.Length; i++)
                    list.Add(items[i]);

                info.SetValue(target, list);
            }

            return target;
        }
    }

    public abstract class AbilityTestBase
    {
        protected const string Health = "Test.Health";
        protected const string MaxHealth = "Test.MaxHealth";
        protected const string Attack = "Test.Attack";
        protected const string Mana = "Test.Mana";

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void DestroyCreatedObjects()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        protected T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        // ---------- Tags ----------

        protected static GameplayTag Tag(string name) => GameplayTag.Request(name);

        protected static GameplayTagContainer Tags(params string[] names)
        {
            var container = new GameplayTagContainer();
            for (int i = 0; i < names.Length; i++)
                container.AddTag(Tag(names[i]));
            return container;
        }

        // ---------- Attributes ----------

        protected GameplayAttributeDefinition AttributeDef(string tag, float baseValue = 100f, float min = 0f, float max = 100f, string maxAttribute = null)
        {
            var definition = Track(ScriptableObject.CreateInstance<GameplayAttributeDefinition>());
            definition.Set("tag", Tag(tag))
                      .Set("baseValue", baseValue)
                      .Set("minValue", min)
                      .Set("maxValue", max);

            if (maxAttribute != null)
                definition.Set("maxAttributeTag", Tag(maxAttribute));

            return definition;
        }

        protected AttributeSetDefinition AttributeSetOf(params GameplayAttributeDefinition[] definitions)
        {
            var set = Track(ScriptableObject.CreateInstance<AttributeSetDefinition>());
            set.SetList("attributes", definitions);
            return set;
        }

        // ---------- Effects ----------

        protected static EffectModifierDefinition Modifier(string attribute, ModifierOperation operation, float magnitude)
        {
            object boxed = default(EffectModifierDefinition);
            FieldSet(boxed, "attributeTag", Tag(attribute));
            FieldSet(boxed, "operation", operation);
            FieldSet(boxed, "magnitudeType", MagnitudeType.Fixed);
            FieldSet(boxed, "magnitude", magnitude);
            return (EffectModifierDefinition)boxed;
        }

        protected static EffectModifierDefinition AttributeBasedModifier(string attribute, ModifierOperation operation,
            AttributeCaptureSource from, string capturedAttribute, float coefficient)
        {
            object boxed = default(EffectModifierDefinition);
            FieldSet(boxed, "attributeTag", Tag(attribute));
            FieldSet(boxed, "operation", operation);
            FieldSet(boxed, "magnitudeType", MagnitudeType.AttributeBased);
            FieldSet(boxed, "magnitude", 0f);
            FieldSet(boxed, "captureFrom", from);
            FieldSet(boxed, "capturedAttribute", Tag(capturedAttribute));
            FieldSet(boxed, "coefficient", coefficient);
            return (EffectModifierDefinition)boxed;
        }

        protected GameplayEffectDefinition Effect(EffectDurationPolicy policy, float duration = 1f, params EffectModifierDefinition[] modifiers)
        {
            var effect = Track(ScriptableObject.CreateInstance<GameplayEffectDefinition>());
            effect.name = "TestEffect";
            effect.Set("durationPolicy", policy)
                  .Set("duration", duration)
                  .Set("modifiers", new List<EffectModifierDefinition>(modifiers));
            return effect;
        }

        // ---------- Abilities ----------

        protected BasicAbilityDefinition BasicAbility(string tag, GameplayEffectDefinition cost = null, GameplayEffectDefinition cooldown = null)
        {
            var definition = Track(ScriptableObject.CreateInstance<BasicAbilityDefinition>());
            definition.Set("abilityTag", Tag(tag));

            if (cost != null) definition.Set("costEffect", cost);
            if (cooldown != null) definition.Set("cooldownEffect", cooldown);

            return definition;
        }

        // ---------- Reactions ----------

        protected static AttributeReaction Reaction(string attribute, AttributeComparison comparison, float threshold, GameplayEffectDefinition effect = null,
            string enterEvent = null, ThresholdMode mode = ThresholdMode.Absolute)
        {
            object boxed = default(AttributeReaction);
            FieldSet(boxed, "attribute", Tag(attribute));
            FieldSet(boxed, "condition", comparison);
            FieldSet(boxed, "thresholdMode", mode);
            FieldSet(boxed, "threshold", threshold);

            if (effect != null) FieldSet(boxed, "effectWhileMet", effect);
            if (enterEvent != null) FieldSet(boxed, "eventOnEnter", Tag(enterEvent));

            return (AttributeReaction)boxed;
        }

        // ---------- Characters ----------

        /// <summary>
        /// Creates a character. 'configure' sets its serialized fields before it initializes,
        /// like the Inspector would (starting abilities, reactions, cue library...).
        /// </summary>
        protected AbilitySystemComponent Character(string name, AttributeSetDefinition set, Action<AbilitySystemComponent> configure = null)
        {
            GameObject go = Track(new GameObject(name));
            go.SetActive(false);

            var asc = go.AddComponent<AbilitySystemComponent>();
            asc.SetList("attributeSets", set);
            configure?.Invoke(asc);

            go.SetActive(true);

            // Any access initializes the component.
            _ = asc.Attributes;
            return asc;
        }

        protected static float Value(AbilitySystemComponent asc, string attribute)
        {
            return asc.Attributes.GetCurrentValue(Tag(attribute));
        }

        /// <summary>Moves time forward in small steps, like frames would.</summary>
        protected static void Advance(AbilitySystemComponent asc, float seconds, float step = 0.1f)
        {
            float left = seconds;
            while (left > 0f)
            {
                float delta = Mathf.Min(step, left);
                asc.Effects.Tick(delta);
                asc.Abilities.Tick(delta);
                left -= delta;
            }
        }

        // Sets a field on a boxed struct (a copy would lose the change).
        private static void FieldSet(object boxed, string field, object value)
        {
            ReflectionExtensions.FindField(boxed.GetType(), field).SetValue(boxed, value);
        }
    }
}
