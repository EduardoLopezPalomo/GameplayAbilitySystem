using NUnit.Framework;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;

namespace supercachola.AbilitySystem.Tests
{
    public class AttributeTests : AbilityTestBase
    {
       private const float Tolerance = 0.001f;

        [Test]
        public void InstantEffect_ChangesTheBaseValue()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));
            var damage = Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, -30f));

            asc.TryApplyEffect(damage, out _);

            Assert.AreEqual(70f, Value(asc, Health), Tolerance);
        }

        [Test]
        public void InstantHeal_NeverGoesAboveTheMaximum()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Health)));

            asc.TryApplyEffect(Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, -50f)), out _);
            asc.TryApplyEffect(Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, 80f)), out _);

            Assert.AreEqual(100f, Value(asc, Health), Tolerance);
        }

        [Test]
        public void DurationModifier_AppliesWhileActiveAndEnds()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Attack, 10f, 0f, 999f)));
            var buff = Effect(EffectDurationPolicy.Duration, 5f, Modifier(Attack, ModifierOperation.Multiply, 2f));

            asc.TryApplyEffect(buff, out _);
            Assert.AreEqual(20f, Value(asc, Attack), Tolerance);

            Advance(asc, 5.5f);
            Assert.AreEqual(10f, Value(asc, Attack), Tolerance);
        }

        [Test]
        public void Override_TheLastOneWins_AndTheOlderOneReturnsWhenRemoved()
        {
            var asc = Character("C", AttributeSetOf(AttributeDef(Attack, 10f, 0f, 999f)));
            var first = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Override, 30f));
            var second = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Override, 20f));

            asc.TryApplyEffect(first, out ActiveEffectHandle firstHandle);
            asc.TryApplyEffect(second, out ActiveEffectHandle secondHandle);
            Assert.AreEqual(20f, Value(asc, Attack), Tolerance);

            asc.RemoveEffect(secondHandle);
            Assert.AreEqual(30f, Value(asc, Attack), Tolerance);

            asc.RemoveEffect(firstHandle);
            Assert.AreEqual(10f, Value(asc, Attack), Tolerance);
        }

        [Test]
        public void DynamicMaximum_FollowsTheMaxAttribute()
        {
            var set = AttributeSetOf(AttributeDef(MaxHealth, 100f, 0f, 9999f),
                AttributeDef(Health, 100f, 0f, 100f, maxAttribute: MaxHealth));

            var asc = Character("C", set);

            var buff = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(MaxHealth, ModifierOperation.Add, 50f));
            asc.TryApplyEffect(buff, out ActiveEffectHandle buffHandle);

            Assert.AreEqual(150f, Value(asc, MaxHealth), Tolerance);
            Assert.AreEqual(100f, Value(asc, Health), Tolerance, "A higher maximum doesn't heal.");

            asc.TryApplyEffect(Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, 50f)), out _);
            Assert.AreEqual(150f, Value(asc, Health), Tolerance);

            asc.RemoveEffect(buffHandle);
            Assert.AreEqual(100f, Value(asc, MaxHealth), Tolerance);
            Assert.AreEqual(100f, Value(asc, Health), Tolerance, "A lower maximum trims the value.");
        }

        [Test]
        public void DynamicMaximum_WithLoop_IsIgnoredWithoutHanging()
        {
            var set = AttributeSetOf(AttributeDef(Health, 100f, 0f, 100f, maxAttribute: MaxHealth),
                AttributeDef(MaxHealth, 100f, 0f, 100f, maxAttribute: Health));

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("limit each other"));

            var asc = Character("C", set);

            Assert.AreEqual(100f, Value(asc, Health), Tolerance);
        }
    }
}
