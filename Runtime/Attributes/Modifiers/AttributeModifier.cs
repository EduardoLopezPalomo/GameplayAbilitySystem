namespace supercachola.AbilitySystem.Attributes
{
    internal readonly struct AttributeModifier
    {
        public readonly int Id;
        public readonly ModifierOperation Operation;
        public readonly float Value;

        public AttributeModifier(int id, ModifierOperation operation, float value)
        {
            Id = id;
            Operation = operation;
            Value = value;
        }
    }
}
