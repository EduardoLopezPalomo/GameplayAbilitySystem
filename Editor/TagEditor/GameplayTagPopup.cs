using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    /// <summary>
    /// Window that shows a list of all the tags in the project, and allows the user to select one. The selected tag is returned to the caller via a SerializedObject and property path.
    /// </summary>
    internal class GameplayTagPopup : PopupWindowContent
    {
        private readonly SerializedObject serializedObject;
        private readonly string propertyPath;
        private readonly List<string> tags = new List<string>();
        private readonly bool hasSettings;

        // What tag is currently selected. This is the tag that will be applied to the property when the user clicks "Confirm".
        private string selected;
        private Vector2 scroll;

        public GameplayTagPopup(SerializedObject serializedObject, string propertyPath, string current)
        {
            this.serializedObject = serializedObject;
            this.propertyPath = propertyPath;
            selected = current ?? "";

            var settings = GameplayTagSettings.Load();
            hasSettings = settings != null;

            if (hasSettings)
            {
                var all = settings.Tags;
                for (int i = 0; i < all.Count; i++)
                    tags.Add(all[i]);

                tags.Sort(TagEditorUtility.Compare);
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 360);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Select Tag", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

            if (!hasSettings)
            {
                EditorGUILayout.HelpBox(
                    "No tags found. Open Window > Ability System > Tag Manager.",
                    MessageType.Info);
            }
            else if (tags.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no tags yet.", MessageType.Info);
            }
            else
            {
                DrawTags();
            }

            EditorGUILayout.EndScrollView();

            DrawButtons();
        }

        private void DrawTags()
        {
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];

                int dots = 0;
                for (int c = 0; c < tag.Length; c++)
                    if (tag[c] == '.') dots++;

                int lastDot = tag.LastIndexOf('.');
                string label = lastDot < 0 ? tag : tag.Substring(lastDot + 1);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(dots * 16);

                bool isOn = tag == selected;
                bool newValue = EditorGUILayout.ToggleLeft(new GUIContent(label, tag), isOn);

                if (newValue != isOn)
                    selected = newValue ? tag : "";

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                editorWindow.Close();
            }

            if (GUILayout.Button("Confirm", GUILayout.Width(80)))
            {
                Apply();
                editorWindow.Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private void Apply()
        {
            serializedObject.Update();
            SerializedProperty nameProp = serializedObject.FindProperty(propertyPath + ".name");
            nameProp.stringValue = selected;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
