
namespace supercachola.AbilitySystem.Abilities
{
    public class TimedAbility : GameplayAbility
    {
        private TimedAbilityDefinition config;
        private float remaining;
        
        protected override void OnInitialize()
        {
            config = (TimedAbilityDefinition)Definition;
        }

        protected override void OnActivate()
        {
            remaining = config.Duration;
            ApplyEffectsToSelf();
        }

        protected override void OnTick(float deltaTime)
        {
            remaining -= deltaTime;
            if (remaining <= 0f)
                End();
        }
    }
}
