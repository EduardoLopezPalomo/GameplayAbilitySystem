using supercachola.AbilitySystem.Events;

namespace supercachola.AbilitySystem.Abilities
{
    public sealed class EventAbility : GameplayAbility
    {
        private EventAbilityDefinition config;

        protected override void OnInitialize()
        {
            config = (EventAbilityDefinition)Definition;
        }

        protected override void OnActivate()
        {
            ApplyEffectsToSelf();

            // Whatever happens first ends the ability. All the waits are cleared when it ends.
            if (config.EndEvent.isValid)
                WaitForEvent(config.EndEvent, OnEndEvent);

            if (config.EndTag.isValid)
                WaitForTag(config.EndTag, true, End);

            if (config.Timeout > 0f)
                WaitForDelay(config.Timeout, End);
        }

        private void OnEndEvent(GameplayEventData data)
        {
            End();
        }
    }
}
