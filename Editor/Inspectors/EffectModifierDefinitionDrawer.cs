using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Effects;

namespace supercachola.AbilitySystem.EditorTools
{
    /// <summary>
    /// Draws a modifier compactly: the first row is always the same,
    /// the other rows depend on the magnitude type.
    /// </summary>
    [CustomPropertyDrawer(typeof(EffectModifierDefinition))]
    public class EffectModifierDefinitionDrawer : PropertyDrawer
    {
        private const float Gap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = GetType(property) == MagnitudeType.AttributeBased ? 3 : 2;
            return lines * EditorGUIUtility.singleLineHeight
                   + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float step = line + EditorGUIUtility.standardVerticalSpacing;
            var row = new Rect(position.x, position.y, position.width, line);

            // Row 1: attribute | operation | type
            Split3(row, 0.5f, 0.25f, out Rect a, out Rect b, out Rect c);
            Field(a, property, "attributeTag");
            Field(b, property, "operation");
            Field(c, property, "magnitudeType");

            row.y += step;

            switch (GetType(property))
            {
                case MagnitudeType.Fixed:
                    LabeledField(row, property, "magnitude", "Value", 45f);
                    break;

                case MagnitudeType.AttributeBased:
                    // Row 2: whose attribute | which attribute
                    Split2(row, 0.35f, out Rect from, out Rect attribute);
                    Field(from, property, "captureFrom");
                    Field(attribute, property, "capturedAttribute");

                    // Row 3: coefficient | flat amount
                    row.y += step;
                    Split2(row, 0.5f, out Rect coefficient, out Rect flat);
                    LabeledField(coefficient, property, "coefficient", "Coefficient", 70f);
                    LabeledField(flat, property, "magnitude", "Flat", 35f);
                    break;

                case MagnitudeType.SetByCaller:
                    // Row 2: tag | default value
                    Split2(row, 0.65f, out Rect tag, out Rect fallback);
                    Field(tag, property, "setByCallerTag");
                    LabeledField(fallback, property, "magnitude", "Default", 50f);
                    break;
            }

            EditorGUI.EndProperty();
        }

        private static MagnitudeType GetType(SerializedProperty property)
        {
            return (MagnitudeType)property.FindPropertyRelative("magnitudeType").enumValueIndex;
        }

        private static void Field(Rect rect, SerializedProperty property, string name)
        {
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(name), GUIContent.none);
        }

        private static void LabeledField(Rect rect, SerializedProperty property, string name, string label, float labelWidth)
        {
            float old = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(name), new GUIContent(label));
            EditorGUIUtility.labelWidth = old;
        }

        private static void Split2(Rect row, float firstFraction, out Rect first, out Rect second)
        {
            float w = row.width * firstFraction;
            first = new Rect(row.x, row.y, w - Gap, row.height);
            second = new Rect(row.x + w, row.y, row.width - w, row.height);
        }

        private static void Split3(Rect row, float firstFraction, float secondFraction,
            out Rect first, out Rect second, out Rect third)
        {
            float w1 = row.width * firstFraction;
            float w2 = row.width * secondFraction;
            first = new Rect(row.x, row.y, w1 - Gap, row.height);
            second = new Rect(row.x + w1, row.y, w2 - Gap, row.height);
            third = new Rect(row.x + w1 + w2, row.y, row.width - w1 - w2, row.height);
        }
    }
}
