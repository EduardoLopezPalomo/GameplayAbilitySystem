using NUnit.Framework;
using UnityEngine;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Cues;
using supercachola.AbilitySystem.Effects;

namespace supercachola.AbilitySystem.Tests
{
    public class CueTests : AbilityTestBase
    {
       private RecordingCue NewCue(string tag)
        {
            var cue = Track(ScriptableObject.CreateInstance<RecordingCue>());
            cue.Set("cueTag", Tag(tag));
            return cue;
        }

        private AbilitySystemComponent CharacterWithCue(RecordingCue cue)
        {
            var library = Track(ScriptableObject.CreateInstance<GameplayCueLibrary>());
            library.SetList("cues", cue);

            return Character("C", AttributeSetOf(AttributeDef(Health)), c => c.Set("cueLibrary", library));
        }

        [Test]
        public void InstantEffect_ExecutesTheCue_WithTheMagnitude()
        {
            var cue = NewCue("Test.Cue.Hit");
            var asc = CharacterWithCue(cue);

            var hit = Effect(EffectDurationPolicy.Instant, 0f, Modifier(Health, ModifierOperation.Add, -30f))
                .SetList("cueTags", Tag("Test.Cue.Hit"));

            asc.TryApplyEffect(hit, out _);

            Assert.AreEqual(1, cue.Executed);
            Assert.AreEqual(-30f, cue.LastMagnitude, 0.001f);
        }

        [Test]
        public void LastingCue_IsSharedBetweenEffects_AndRemovedWithTheLastOne()
        {
            var cue = NewCue("Test.Cue.Burning");
            var asc = CharacterWithCue(cue);

            var burn = Effect(EffectDurationPolicy.Infinite).SetList("cueTags", Tag("Test.Cue.Burning"));

            asc.TryApplyEffect(burn, out ActiveEffectHandle first);
            asc.TryApplyEffect(burn, out ActiveEffectHandle second);

            Assert.AreEqual(1, cue.Added, "Two effects, one single instance.");
            Assert.AreEqual(1, asc.Cues.ActiveCount);
            Assert.AreEqual(2, asc.Cues.GetActiveCountAt(0));

            asc.RemoveEffect(first);
            Assert.AreEqual(0, cue.Removed);

            asc.RemoveEffect(second);
            Assert.AreEqual(1, cue.Removed);
            Assert.AreEqual(0, asc.Cues.ActiveCount);
        }

        [Test]
        public void SuspendedEffect_DoesNotPlayItsCue()
        {
            var cue = NewCue("Test.Cue.Aura");
            var asc = CharacterWithCue(cue);

            var aura = Effect(EffectDurationPolicy.Infinite)
                .Set("ongoingRequiredTags", Tags("Test.Enraged"))
                .SetList("cueTags", Tag("Test.Cue.Aura"));

            asc.TryApplyEffect(aura, out _);
            Assert.AreEqual(0, cue.Added);

            asc.OwnedTags.AddTag(Tag("Test.Enraged"));
            Assert.AreEqual(1, cue.Added);

            asc.OwnedTags.RemoveTag(Tag("Test.Enraged"));
            Assert.AreEqual(1, cue.Removed);
        }

        [Test]
        public void RemoveAllActive_EndsEveryLastingCue()
        {
            var cue = NewCue("Test.Cue.Burning");
            var asc = CharacterWithCue(cue);

            asc.TryApplyEffect(
                Effect(EffectDurationPolicy.Infinite).SetList("cueTags", Tag("Test.Cue.Burning")), out _);
            Assert.AreEqual(1, asc.Cues.ActiveCount);

            asc.Cues.RemoveAllActive();

            Assert.AreEqual(1, cue.Removed);
            Assert.AreEqual(0, asc.Cues.ActiveCount);
        }
    }
}
