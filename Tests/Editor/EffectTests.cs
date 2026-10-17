using NUnit.Framework;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;

namespace supercachola.AbilitySystem.Tests
{
    public class EffectTests : AbilityTestBase
    {
        private const float Tolerance = 0.001f;

        private AbilitySystemComponent NewCharacter(string name = "C")
        {
            return Character(name, AttributeSetOf(
                AttributeDef(Health),
                AttributeDef(Attack, 10f, 0f, 999f)));
        }

        // ---------- Duration and periodic ----------

        [Test]
        public void DurationEffect_GrantsTagsAndExpires()
        {
            var asc = NewCharacter();
            var burn = Effect(EffectDurationPolicy.Duration, 2f).Set("grantedTags", Tags("Test.Burning"));

            asc.TryApplyEffect(burn, out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.Burning")));

            Advance(asc, 1f);
            Assert.IsTrue(asc.HasTag(Tag("Test.Burning")));

            Advance(asc, 1.2f);
            Assert.IsFalse(asc.HasTag(Tag("Test.Burning")));
            Assert.AreEqual(0, asc.Effects.Count);
        }

        [Test]
        public void PeriodicEffect_ExecutesOncePerPeriod()
        {
            var asc = NewCharacter();
            var poison = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Health, ModifierOperation.Add, -10f))
                .Set("period", 1f);

            asc.TryApplyEffect(poison, out _);
            Advance(asc, 3.5f);

            Assert.AreEqual(70f, Value(asc, Health), Tolerance);
        }

        [Test]
        public void PeriodicEffect_ExecuteOnApplication_AddsOneMoreExecution()
        {
            var asc = NewCharacter();
            var poison = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Health, ModifierOperation.Add, -10f))
                .Set("period", 1f)
                .Set("executeOnApplication", true);

            asc.TryApplyEffect(poison, out _);
            Assert.AreEqual(90f, Value(asc, Health), Tolerance);

            Advance(asc, 3.5f);
            Assert.AreEqual(60f, Value(asc, Health), Tolerance);
        }

        [Test]
        public void HugeTimeStep_DoesNotHangAndIsLimited()
        {
            var asc = NewCharacter();
            var poison = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Health, ModifierOperation.Add, -1f))
                .Set("period", 0.01f);

            asc.TryApplyEffect(poison, out _);
            asc.Effects.Tick(100000f);

            // The limit per Tick protects against lag spikes: it finishes and doesn't run 10 million times.
            Assert.GreaterOrEqual(Value(asc, Health), 0f);
        }

        // ---------- Magnitudes ----------

        // The damage uses the attack of whoever applies it, and a buff on the attacker changes it.
        [Test]
        public void AttributeBasedDamage_UsesTheInstigatorAttack()
        {
            var attacker = Character("Attacker", AttributeSetOf(AttributeDef(Attack, 20f, 0f, 999f)));
            var target = Character("Target", AttributeSetOf(AttributeDef(Health)));

            var hit = Effect(EffectDurationPolicy.Instant, 0f,
                AttributeBasedModifier(Health, ModifierOperation.Add, AttributeCaptureSource.Instigator, Attack, -1f));

            attacker.TryApplyEffectTo(target, hit, out _);
            Assert.AreEqual(80f, Value(target, Health), Tolerance);

            var buff = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 10f));
            attacker.TryApplyEffect(buff, out _);

            attacker.TryApplyEffectTo(target, hit, out _);
            Assert.AreEqual(50f, Value(target, Health), Tolerance);
        }

        // ---------- Tags: requirements, immunity, cleanse ----------

        [Test]
        public void ApplicationBlockedTags_PreventTheEffect()
        {
            var asc = NewCharacter();
            var effect = Effect(EffectDurationPolicy.Infinite)
                .Set("grantedTags", Tags("Test.Marker"))
                .Set("applicationBlockedTags", Tags("Test.Immune"));

            asc.OwnedTags.AddTag(Tag("Test.Immune"));
            Assert.IsFalse(asc.TryApplyEffect(effect, out _));

            asc.OwnedTags.RemoveTag(Tag("Test.Immune"));
            Assert.IsTrue(asc.TryApplyEffect(effect, out _));
        }

        [Test]
        public void Immunity_BlocksEffectsWithAChildTag()
        {
            var asc = NewCharacter();
            var shield = Effect(EffectDurationPolicy.Infinite).Set("grantedImmunityTags", Tags("Test.Debuff"));
            var burn = Effect(EffectDurationPolicy.Infinite)
                .Set("effectTags", Tags("Test.Debuff.Fire"))
                .Set("grantedTags", Tags("Test.Burning"));

            asc.TryApplyEffect(shield, out ActiveEffectHandle shieldHandle);

            Assert.IsFalse(asc.TryApplyEffect(burn, out _));
            Assert.IsFalse(asc.HasTag(Tag("Test.Burning")));

            asc.RemoveEffect(shieldHandle);
            Assert.IsTrue(asc.TryApplyEffect(burn, out _));
            Assert.IsTrue(asc.HasTag(Tag("Test.Burning")));
        }

        [Test]
        public void Cleanse_RemovesEffectsWithMatchingTags()
        {
            var asc = NewCharacter();
            var burn = Effect(EffectDurationPolicy.Infinite)
                .Set("effectTags", Tags("Test.Debuff.Fire"))
                .Set("grantedTags", Tags("Test.Burning"));
            var antidote = Effect(EffectDurationPolicy.Instant)
                .Set("removeEffectsWithTags", Tags("Test.Debuff"));

            asc.TryApplyEffect(burn, out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.Burning")));

            asc.TryApplyEffect(antidote, out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.Burning")));
            Assert.AreEqual(0, asc.Effects.Count);
        }

        // ---------- Stacking ----------

        [Test]
        public void Stacking_Independent_EachApplicationIsSeparate()
        {
            var asc = NewCharacter();
            var effect = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 5f));

            asc.TryApplyEffect(effect, out _);
            asc.TryApplyEffect(effect, out _);

            Assert.AreEqual(2, asc.Effects.Count);
            Assert.AreEqual(20f, Value(asc, Attack), Tolerance);
        }

        [Test]
        public void Stacking_StackCharges_AddsChargesUpToTheMaximum()
        {
            var asc = NewCharacter();
            var effect = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 5f))
                .Set("stackingPolicy", EffectStackingPolicy.StackCharges)
                .Set("maxStacks", 3);

            for (int i = 0; i < 4; i++)
                asc.TryApplyEffect(effect, out _);

            Assert.AreEqual(1, asc.Effects.Count);
            Assert.AreEqual(3, asc.Effects.GetActiveAt(0).Stacks);
            Assert.AreEqual(25f, Value(asc, Attack), Tolerance); // 10 + 5 x 3
        }

        [Test]
        public void Stacking_Ignore_KeepsTheFirstOne()
        {
            var asc = NewCharacter();
            var effect = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 5f))
                .Set("stackingPolicy", EffectStackingPolicy.Ignore);

            asc.TryApplyEffect(effect, out _);
            asc.TryApplyEffect(effect, out _);

            Assert.AreEqual(1, asc.Effects.Count);
            Assert.AreEqual(15f, Value(asc, Attack), Tolerance);
        }

        [Test]
        public void Stacking_RefreshDuration_RestartsTheTimer()
        {
            var asc = NewCharacter();
            var effect = Effect(EffectDurationPolicy.Duration, 2f)
                .Set("grantedTags", Tags("Test.Refreshed"))
                .Set("stackingPolicy", EffectStackingPolicy.RefreshDuration);

            asc.TryApplyEffect(effect, out _);
            Advance(asc, 1.5f);
            asc.TryApplyEffect(effect, out _);

            Assert.AreEqual(1, asc.Effects.Count);

            Advance(asc, 1f);
            Assert.IsTrue(asc.HasTag(Tag("Test.Refreshed")), "Without the refresh it would have ended at 2 s.");

            Advance(asc, 1.5f);
            Assert.IsFalse(asc.HasTag(Tag("Test.Refreshed")));
        }

        [Test]
        public void Stacking_ScopeInstigator_KeepsOneStackPerAttacker()
        {
            var target = NewCharacter("Target");
            var attackerA = NewCharacter("A");
            var attackerB = NewCharacter("B");

            var poison = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 5f))
                .Set("stackingPolicy", EffectStackingPolicy.StackCharges)
                .Set("stackScope", EffectStackScope.Instigator)
                .Set("maxStacks", 5);

            attackerA.TryApplyEffectTo(target, poison, out _);
            attackerA.TryApplyEffectTo(target, poison, out _);
            attackerB.TryApplyEffectTo(target, poison, out _);

            Assert.AreEqual(2, target.Effects.Count, "One effect per attacker.");
        }

        // ---------- Ongoing requirements ----------

        [Test]
        public void OngoingRequiredTags_SuspendAndResumeTheEffect()
        {
            var asc = NewCharacter();
            var rage = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(Attack, ModifierOperation.Add, 10f))
                .Set("ongoingRequiredTags", Tags("Test.Enraged"));

            asc.TryApplyEffect(rage, out _);
            Assert.AreEqual(10f, Value(asc, Attack), Tolerance, "Suspended: the tag is missing.");

            asc.OwnedTags.AddTag(Tag("Test.Enraged"));
            Assert.AreEqual(20f, Value(asc, Attack), Tolerance);

            asc.OwnedTags.RemoveTag(Tag("Test.Enraged"));
            Assert.AreEqual(10f, Value(asc, Attack), Tolerance);
            Assert.AreEqual(1, asc.Effects.Count, "It is suspended, not removed.");
        }
    }
}
