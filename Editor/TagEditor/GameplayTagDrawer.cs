using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    [CustomPropertyDrawer(typeof(GameplayTag))]
    public class GameplayTagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProp = property.FindPropertyRelative("name");

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);

            string current = nameProp.stringValue;
            bool isEmpty = string.IsNullOrEmpty(current);
            bool isMissing = !isEmpty && !TagEditorUtility.Exists(current);

            string text = isEmpty ? "None" : (isMissing ? current + " (missing)" : current);

            Color oldColor = GUI.color;
            if (isMissing)
                GUI.color = new Color(1f, 0.55f, 0.55f);

            if (EditorGUI.DropdownButton(fieldRect, new GUIContent(text), FocusType.Keyboard))
            {
                PopupWindow.Show(fieldRect,
                    new GameplayTagPopup(property.serializedObject, property.propertyPath, current));
            }

            GUI.color = oldColor;

            EditorGUI.EndProperty();
        }
    }
}
