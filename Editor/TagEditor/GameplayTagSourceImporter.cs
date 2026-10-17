using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.EditorTools
{
    /// <summary>Adds the tags of every imported Tag Source to the project's tag settings.</summary>
    internal sealed class GameplayTagSourceImporter : AssetPostprocessor
    {
        private const string SettingsFolder = "Assets/Resources";
        private const string SettingsPath = SettingsFolder + "/GameplayTagSettings.asset";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> sourcePaths = null;

            for (int i = 0; i < importedAssets.Length; i++)
            {
                string path = importedAssets[i];

                // The cheap checks go first: this runs for every imported asset.
                if (!path.EndsWith(".asset"))
                    continue;

                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(GameplayTagSource))
                    continue;

                if (sourcePaths == null)
                    sourcePaths = new List<string>();

                sourcePaths.Add(path);
            }

            if (sourcePaths == null)
                return;

            // Not in the middle of the import: changing other assets there is not safe.
            EditorApplication.delayCall += () => Merge(sourcePaths);
        }

        private static void Merge(List<string> sourcePaths)
        {
            GameplayTagSettings settings = GameplayTagSettings.Load();
            bool created = false;

            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(SettingsFolder))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                settings = ScriptableObject.CreateInstance<GameplayTagSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                created = true;
            }

            int added = 0;

            for (int s = 0; s < sourcePaths.Count; s++)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameplayTagSource>(sourcePaths[s]);
                if (source == null)
                    continue;

                for (int i = 0; i < source.Tags.Count; i++)
                    added += AddWithParents(settings, source.Tags[i]);
            }

            if (added > 0 || created)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                settings.RegisterAll();

                Debug.Log($"Ability System: {added} tag(s) added to the project tags" +
                          (created ? $" (the tag settings asset was created at {SettingsPath})." : "."));
            }
        }

        // "A.B.C" adds "A", "A.B" and "A.B.C". Returns how many were new.
        private static int AddWithParents(GameplayTagSettings settings, string name)
        {
            int added = 0;

            int dot = -1;
            while ((dot = name.IndexOf('.', dot + 1)) >= 0)
            {
                if (settings.AddTag(name.Substring(0, dot)))
                    added++;
            }

            if (settings.AddTag(name))
                added++;

            return added;
        }

        // Fills the list from the tags of the project, to avoid typing them by hand.
        [MenuItem("CONTEXT/GameplayTagSource/Fill From Project Tags")]
        private static void FillFromProject(MenuCommand command)
        {
            GameplayTagSettings settings = GameplayTagSettings.Load();
            if (settings == null)
            {
                Debug.LogWarning("There are no project tags yet. Open Window > Ability System > Tag Manager.");
                return;
            }

            var serialized = new SerializedObject(command.context);
            SerializedProperty list = serialized.FindProperty("tags");

            list.arraySize = settings.Tags.Count;
            for (int i = 0; i < settings.Tags.Count; i++)
                list.GetArrayElementAtIndex(i).stringValue = settings.Tags[i];

            serialized.ApplyModifiedProperties();
        }
    }
}
