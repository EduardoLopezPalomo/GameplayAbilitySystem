using NUnit.Framework;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;
namespace supercachola.AbilitySystem.Tests
{
    public class ReactionTests : AbilityTestBase
    {
        private GameplayEffectDefinition Damage(float amount)
        {
            return Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, -amount));
        }

        private GameplayEffectDefinition Heal(float amount)
        {
            return Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, amount));
        }

        [Test]
        public void Death_AppliesTheEffectAndSendsTheEvent_OnlyWhenTheStateChanges()
        {
            var dead = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.Dead"));
            int deaths = 0;

            var asc = Character("Enemy", AttributeSetOf(AttributeDef(Health)),
                c => c.SetList("reactions", Reaction(Health, AttributeComparison.LessOrEqual, 0f, dead, "Test.Event.Death")));
            asc.RegisterGameplayEvent(Tag("Test.Event.Death"), data => deaths++);

            asc.TryApplyEffect(Damage(60f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.Dead")));

            asc.TryApplyEffect(Damage(60f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.Dead")));
            Assert.AreEqual(1, deaths);
            
            asc.TryApplyEffect(Damage(10f), out _);
            Assert.AreEqual(1, deaths);

            asc.TryApplyEffect(Heal(10f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.Dead")), "Not dead anymore: the effect is removed.");

            asc.TryApplyEffect(Damage(10f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.Dead")));
            Assert.AreEqual(2, deaths);
        }

        [Test]
        public void PercentThreshold_UsesTheCurrentMaximum()
        {
            var low = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.LowHealth"));

            var asc = Character("C", AttributeSetOf(AttributeDef(Health)),
                c => c.SetList("reactions", Reaction(Health, AttributeComparison.LessOrEqual, 0.25f, low, null, ThresholdMode.PercentOfMax)));

            asc.TryApplyEffect(Damage(50f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowHealth")));

            asc.TryApplyEffect(Damage(30f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.LowHealth")));
        }

        [Test]
        public void PercentThreshold_ChecksAgainWhenTheMaximumChanges()
        {
            var low = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.LowHealth"));
            var set = AttributeSetOf(
                AttributeDef(MaxHealth, 100f, 0f, 9999f),
                AttributeDef(Health, 100f, 0f, 100f, maxAttribute: MaxHealth));

            var asc = Character("C", set,
                c => c.SetList("reactions", Reaction(Health, AttributeComparison.LessOrEqual , 0.25f, low, null, ThresholdMode.PercentOfMax)));

            asc.TryApplyEffect(Damage(70f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowHealth")));
            
            var shrink = Effect(EffectDurationPolicy.Infinite, 0f, Modifier(MaxHealth, ModifierOperation.Add, -50f));
            asc.TryApplyEffect(shrink, out _);
            Assert.AreEqual(30f, Value(asc, Health), 0.001f);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowHealth")));

            asc.TryApplyEffect(Damage(20f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.LowHealth")));
        }

        [Test]
        public void Comparison_AtTheThreshold_BehaveAsExpected()
        {
            var lowerThan = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.LowerHealthThan"));
            var lowerOrEqual = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.LowerHealthOrEqual"));
            var greaterThan = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.GreaterHealthThan"));
            var greaterOrEqual = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.GreaterHealthOrEqual"));
            
            var asc = Character("Enemy", AttributeSetOf(AttributeDef(Health)),
                c => c.SetList("reactions", 
                    Reaction(Health, AttributeComparison.LessOrEqual, 10f, lowerOrEqual, "Test.Event.LowerOrEqual"),
                    Reaction(Health, AttributeComparison.LessThan, 10f, lowerThan, "Test.Event.LowerThan"),
                    Reaction(Health, AttributeComparison.GreaterThan, 20f, greaterThan, "Test.Event.GreaterThan"),
                    Reaction(Health, AttributeComparison.GreaterOrEqual, 20f, greaterOrEqual, "Test.Event.GreaterOrEqual")
                    ));
            asc.TryApplyEffect(Damage(90f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.LowerHealthOrEqual")));
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthOrEqual")));
            
            asc.TryApplyEffect(Damage(1f), out _);
            Assert.IsTrue(asc.HasTag(Tag("Test.LowerHealthOrEqual")));
            Assert.IsTrue(asc.HasTag(Tag("Test.LowerHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthOrEqual")));
            
            asc.TryApplyEffect(Heal(11f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthOrEqual")));
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthThan")));
            Assert.IsTrue(asc.HasTag(Tag("Test.GreaterHealthOrEqual")));
            
            asc.TryApplyEffect(Heal(1f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthOrEqual")));
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthThan")));
            Assert.IsTrue(asc.HasTag(Tag("Test.GreaterHealthThan")));
            Assert.IsTrue(asc.HasTag(Tag("Test.GreaterHealthOrEqual")));
            
            asc.TryApplyEffect(Damage(5f), out _);
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthOrEqual")));
            Assert.IsFalse(asc.HasTag(Tag("Test.LowerHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthThan")));
            Assert.IsFalse(asc.HasTag(Tag("Test.GreaterHealthOrEqual")));
        }
    }
}
