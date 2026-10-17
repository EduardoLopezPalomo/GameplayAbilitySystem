using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    [CustomPropertyDrawer(typeof(GameplayTagContainer))]
    public class GameplayTagContainerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty names = property.FindPropertyRelative("tagNames");
            int lines = 1 + names.arraySize;
            return lines * EditorGUIUtility.singleLineHeight
                 + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty names = property.FindPropertyRelative("tagNames");
            int count = names.arraySize;

            EditorGUI.BeginProperty(position, label, property);

            Rect line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            Rect field = EditorGUI.PrefixLabel(line, label);
            string summary = count == 0 ? "None" : (count == 1 ? "1 tag" : count + " tags");

            if (EditorGUI.DropdownButton(field, new GUIContent(summary), FocusType.Keyboard))
            {
                var current = new List<string>(count);
                for (int i = 0; i < count; i++)
                    current.Add(names.GetArrayElementAtIndex(i).stringValue);

                PopupWindow.Show(field,
                    new GameplayTagContainerPopup(property.serializedObject, property.propertyPath, current));
            }

            Color oldColor = GUI.color;
            for (int i = 0; i < count; i++)
            {
                line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                string name = names.GetArrayElementAtIndex(i).stringValue;
                bool missing = !TagEditorUtility.Exists(name);

                Rect r = line;
                r.x = field.x;
                r.width = field.width;

                GUI.color = missing ? new Color(1f, 0.55f, 0.55f) : oldColor;
                EditorGUI.LabelField(r, missing ? name + " (missing)" : name);
            }
            GUI.color = oldColor;

            EditorGUI.EndProperty();
        }
    }
}
