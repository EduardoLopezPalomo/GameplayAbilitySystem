
namespace supercachola.AbilitySystem.Effects
{
    public enum MagnitudeType
    {
        /// <summary>A plain number.</summary>
        Fixed,

        /// <summary>Attribute value x coefficient + flat amount.</summary>
        AttributeBased,

        /// <summary>A value sent by whoever applies the effect, identified by a tag.</summary>
        SetByCaller
    }
}
