using System.Collections.Generic;
using NUnit.Framework;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Tests
{
    public class TagTests : AbilityTestBase
    {
       [Test]
        public void Container_ParentOfStoredTag_Matches()
        {
            GameplayTagContainer container = Tags("Test.Fire.Burning");

            Assert.IsTrue(container.HasTag(Tag("Test.Fire")));
            Assert.IsTrue(container.HasTag(Tag("Test.Fire.Burning")));
        }

        [Test]
        public void Container_ChildOfStoredTag_DoesNotMatch()
        {
            GameplayTagContainer container = Tags("Test.Fire");

            Assert.IsFalse(container.HasTag(Tag("Test.Fire.Burning")));
        }

        [Test]
        public void Container_HasTagExact_IgnoresHierarchy()
        {
            GameplayTagContainer container = Tags("Test.Fire.Burning");

            Assert.IsFalse(container.HasTagExact(Tag("Test.Fire")));
            Assert.IsTrue(container.HasTagExact(Tag("Test.Fire.Burning")));
        }
        
        [Test]
        public void HasAny_ParentDoesNotSatisfyRequiredChild()
        {
            GameplayTagContainer required = Tags("Test.Damage.Fire");

            Assert.IsFalse(Tags("Test.Damage", "Test.State.Frozen").HasAny(required));
            Assert.IsTrue(Tags("Test.Damage.Fire.Burst").HasAny(required));
        }

        [Test]
        public void CountContainer_OnlyReportsChangesOfPresence()
        {
            var tags = new GameplayTagCountContainer();
            GameplayTag tag = Tag("Test.Counted");
            var changes = new List<bool>();

            tags.RegisterTagEvent(tag, (t, present) => changes.Add(present));

            tags.AddTag(tag);
            tags.AddTag(tag);
            CollectionAssert.AreEqual(new[] { true }, changes);

            tags.RemoveTag(tag);
            Assert.IsTrue(tags.HasTag(tag), "One source is still giving the tag.");
            CollectionAssert.AreEqual(new[] { true }, changes);

            tags.RemoveTag(tag);
            Assert.IsFalse(tags.HasTag(tag));
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void Characters_DoNotShareTags()
        {
            var set = AttributeSetOf(AttributeDef(Health));
            var a = Character("A", set);
            var b = Character("B", set);

            var effect = Effect(EffectDurationPolicy.Infinite).Set("grantedTags", Tags("Test.OnlyForA"));

            a.TryApplyEffect(effect, out _);

            Assert.IsTrue(a.HasTag(Tag("Test.OnlyForA")));
            Assert.IsFalse(b.HasTag(Tag("Test.OnlyForA")));
        }
        
        [Test]
        public void AssetContainers_AreFrozen_ButOwnContainersAreNot()
        {
            var effect = Track(UnityEngine.ScriptableObject.CreateInstance<GameplayEffectDefinition>());

            Assert.Throws<System.InvalidOperationException>(() => effect.GrantedTags.AddTag(Tag("Test.Forbidden")));
            Assert.IsTrue(new GameplayTagContainer().AddTag(Tag("Test.Allowed")));
        }
    }
}
