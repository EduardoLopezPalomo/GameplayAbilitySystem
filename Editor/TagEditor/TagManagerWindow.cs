using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    public class TagManagerWindow : EditorWindow
    {
        private const string AssetFolder = "Assets/Resources";
        private const string AssetPath = AssetFolder + "/GameplayTagSettings.asset";

        private GameplayTagSettings settings;
        private readonly List<string> sortedTags = new List<string>();
        private string newTag = "";
        private Vector2 scroll;

        [MenuItem("Window/Ability System/Tag Manager")]
        public static void Open()
        {
            GetWindow<TagManagerWindow>("Tag Manager");
        }

        private void OnEnable()
        {
            Reload();

            if (settings != null && settings.EnsureParents())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                RefreshSortedTags();
            }

            Undo.undoRedoPerformed += OnUndoRedo;
        }
        
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }
        
        private void OnFocus()
        {
            Reload();
        }

        private void OnUndoRedo()
        {
            Reload();
            Repaint();
        }
        
        private void Reload()
        {
            settings = GameplayTagSettings.Load();
            RefreshSortedTags();
        }
        
        private void RefreshSortedTags()
        {
            sortedTags.Clear();
            if (settings == null)
                return;

            var tags = settings.Tags;
            for (int i = 0; i < tags.Count; i++)
                sortedTags.Add(tags[i]);

            sortedTags.Sort(TagEditorUtility.Compare);
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                DrawCreateSettings();
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Gameplay Tags", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawAddField();

            EditorGUILayout.Space(10);

            DrawTagList();
        }

        private void DrawCreateSettings()
        {
            EditorGUILayout.HelpBox(
                "The assets doesn't exits in " + AssetPath + ".",
                MessageType.Info);

            if (GUILayout.Button("Create GameplayTagSettings"))
            {
                if (!AssetDatabase.IsValidFolder(AssetFolder))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                settings = CreateInstance<GameplayTagSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
                AssetDatabase.SaveAssets();
                RefreshSortedTags();
            }
        }

        private void DrawAddField()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("NewTagField");
            newTag = EditorGUILayout.TextField("New Tag", newTag);

            if (GUILayout.Button("Add", GUILayout.Width(60), GUILayout.Height(20)))
                TryAddTag();

            EditorGUILayout.EndHorizontal();
        }

        private void TryAddTag()
        {
            if (!GameplayTagSettings.IsValidName(newTag))
            {
                EditorUtility.DisplayDialog("Invalid tag name",
                    "The name can not be empty, have spaces, double points or start/end in a point..\n\nExample: State.Burning.Fire",
                    "OK");
                return;
            }

            Undo.RecordObject(settings, "Add Gameplay Tag");
            if (!settings.AddTag(newTag))
            {
                EditorUtility.DisplayDialog("Duplicated Tag", "This Tag already exits.", "OK");
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshSortedTags();
            newTag = "";
            GUI.FocusControl(null);
        }

        private void DrawTagList()
        {
            string toRemove = null;
            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < sortedTags.Count; i++)
            {
                string tag = sortedTags[i];

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(CountDots(tag) * 16);
                EditorGUILayout.LabelField(tag);

                if (GUILayout.Button("X", GUILayout.Width(24)))
                    toRemove = tag;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (toRemove != null)
                TryRemoveTag(toRemove);
        }

        private void TryRemoveTag(string tag)
        {
            if (!EditorUtility.DisplayDialog("Delete Tag",
                    "Delete '" + tag + "' and all its children?", "Delete", "Cancel"))
                return;

            Undo.RecordObject(settings, "Remove Gameplay Tag");
            settings.RemoveTag(tag);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            RefreshSortedTags();
        }

        private static int CountDots(string s)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == '.') count++;
            return count;
        }
    }
}
