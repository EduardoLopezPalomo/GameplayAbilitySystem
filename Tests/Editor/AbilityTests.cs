using NUnit.Framework;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Events;
using UnityEngine;

namespace supercachola.AbilitySystem.Tests
{
    public class AbilityTests : AbilityTestBase
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Ability_PaysTheCost_AndGoesOnCooldown()
        {
            var asc = Character("Caster", AttributeSetOf(AttributeDef(Mana, 50f)));

            var cost = Effect(EffectDurationPolicy.Instant, 0f, Modifier(Mana, ModifierOperation.Add, -20f));
            var cooldown = Effect(EffectDurationPolicy.Duration, 2f).Set("grantedTags", Tags("Test.Cooldown.Spell"));
            asc.GrantAbility(BasicAbility("Test.Ability.Spell", cost, cooldown));

            Assert.IsTrue(asc.TryActivateAbility(Tag("Test.Ability.Spell")));
            Assert.AreEqual(30f, Value(asc, Mana), Tolerance);
            Assert.IsTrue(asc.HasTag(Tag("Test.Cooldown.Spell")));

            Assert.IsFalse(asc.TryActivateAbility(Tag("Test.Ability.Spell")), "It is on cooldown.");

            Advance(asc, 2.2f);
            Assert.IsFalse(asc.HasTag(Tag("Test.Cooldown.Spell")));
            Assert.IsTrue(asc.TryActivateAbility(Tag("Test.Ability.Spell")));
            Assert.AreEqual(10f, Value(asc, Mana), Tolerance);

            Advance(asc, 2.2f);
            Assert.IsFalse(asc.TryActivateAbility(Tag("Test.Ability.Spell")), "10 mana is not enough for a cost of 20.");
        }

        [Test]
        public void Ability_BlockedByTags()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            var definition = BasicAbility("Test.Ability.Punch")
                .Set("activationBlockedTags", Tags("Test.Stunned"));
            asc.GrantAbility(definition);

            asc.OwnedTags.AddTag(Tag("Test.Stunned"));
            Assert.IsFalse(asc.TryActivateAbility(Tag("Test.Ability.Punch")));

            asc.OwnedTags.RemoveTag(Tag("Test.Stunned"));
            Assert.IsTrue(asc.TryActivateAbility(Tag("Test.Ability.Punch")));
        }

        [Test]
        public void Ability_TriggeredByAnEvent()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            asc.TryApplyEffect(Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, -50f)), out _);

            var heal = Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, 10f));
            var definition = BasicAbility("Test.Ability.Heal")
                .Set("triggerEventTags", Tags("Test.Event.Heal"))
                .SetList("effectsToSelf", heal);
            asc.GrantAbility(definition);

            asc.SendGameplayEvent(Tag("Test.Event.Heal"));

            Assert.AreEqual(60f, Value(asc, Health), Tolerance);
        }

        [Test]
        public void EventAbility_EndsWhenTheEndEventArrives()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            var definition = Track(ScriptableObject.CreateInstance<EventAbilityDefinition>());
            definition.Set("abilityTag", Tag("Test.Ability.Wait"))
                      .Set("endEvent", Tag("Test.Event.End"))
                      .Set("timeout", 5f);
            asc.GrantAbility(definition);

            Assert.IsTrue(asc.TryActivateAbility(Tag("Test.Ability.Wait")));
            Assert.IsTrue(asc.Abilities.IsAbilityActive(Tag("Test.Ability.Wait")));

            asc.SendGameplayEvent(Tag("Test.Event.End"));

            Assert.IsFalse(asc.Abilities.IsAbilityActive(Tag("Test.Ability.Wait")));
        }

        [Test]
        public void EventAbility_EndsByTimeout()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            var definition = Track(ScriptableObject.CreateInstance<EventAbilityDefinition>());
            definition.Set("abilityTag", Tag("Test.Ability.Wait"))
                      .Set("endEvent", Tag("Test.Event.End"))
                      .Set("timeout", 5f);
            asc.GrantAbility(definition);

            asc.TryActivateAbility(Tag("Test.Ability.Wait"));

            Advance(asc, 4f);
            Assert.IsTrue(asc.Abilities.IsAbilityActive(Tag("Test.Ability.Wait")));

            Advance(asc, 1.5f);
            Assert.IsFalse(asc.Abilities.IsAbilityActive(Tag("Test.Ability.Wait")));
        }

        [Test]
        public void Events_ParentListenerHearsChildEvents()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            int heard = 0;
            System.Action<GameplayEventData> listener = data => heard++;

            asc.RegisterGameplayEvent(Tag("Test.Event.Hit"), listener);

            asc.SendGameplayEvent(Tag("Test.Event.Hit.Critical"));
            asc.SendGameplayEvent(Tag("Test.Event.Other"));
            Assert.AreEqual(1, heard);

            asc.UnregisterGameplayEvent(Tag("Test.Event.Hit"), listener);
            asc.SendGameplayEvent(Tag("Test.Event.Hit"));
            Assert.AreEqual(1, heard);
        }
    }
}
