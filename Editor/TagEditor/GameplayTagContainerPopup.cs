using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    /// <summary>
    /// Window that shows a list of all the tags in the project and allows the user to select multiple tags. The selected tags are stored in a SerializedProperty of type GameplayTagContainer.
    /// </summary>
    internal class GameplayTagContainerPopup : PopupWindowContent
    {
        private readonly SerializedObject serializedObject;
        private readonly string propertyPath;
        private readonly List<string> tags = new List<string>();
        private readonly HashSet<string> selected = new HashSet<string>();
        private readonly bool hasSettings;

        private Vector2 scroll;

        public GameplayTagContainerPopup(SerializedObject serializedObject, string propertyPath, List<string> current)
        {
            this.serializedObject = serializedObject;
            this.propertyPath = propertyPath;

            for (int i = 0; i < current.Count; i++)
                selected.Add(current[i]);

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
            return new Vector2(300, 380);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Select Tags", EditorStyles.boldLabel);
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

                bool isOn = selected.Contains(tag);
                bool newValue = EditorGUILayout.ToggleLeft(new GUIContent(label, tag), isOn);

                if (newValue != isOn)
                {
                    if (newValue) selected.Add(tag);
                    else selected.Remove(tag);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                selected.Clear();

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
            var result = new List<string>(selected);
            result.Sort(TagEditorUtility.Compare);

            serializedObject.Update();
            SerializedProperty array = serializedObject.FindProperty(propertyPath + ".tagNames");

            array.arraySize = result.Count;
            for (int i = 0; i < result.Count; i++)
                array.GetArrayElementAtIndex(i).stringValue = result[i];

            serializedObject.ApplyModifiedProperties();
        }
    }
}
