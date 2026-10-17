

namespace supercachola.AbilitySystem.Abilities
{
    public sealed class BasicAbility : GameplayAbility
    {
        protected override void OnActivate()
        {
            ApplyEffectsToSelf();
            End();
        }
    }
}
