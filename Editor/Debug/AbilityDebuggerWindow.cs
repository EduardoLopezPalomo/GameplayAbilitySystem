using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using supercachola.AbilitySystem.Abilities;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem.Effects;
using supercachola.AbilitySystem.Tags;
using supercachola.AbilitySystem.Cues;

namespace supercachola.AbilitySystem.EditorTools
{
    /// <summary>
    /// Editor-only window that shows the attributes, abilities and tags of an Ability System while the game runs.
    /// By default it shows the player (found by tag and layer). "Next" jumps to the next Ability System in the scene.
    /// Nothing here is included in builds.
    /// </summary>
    public class AbilityDebuggerWindow : EditorWindow
    {
       // How often (seconds) it looks for the default target while it has none.
        private const double SearchInterval = 1.0;
 
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private LayerMask playerLayer = ~0;
        [SerializeField] private bool followSelection;
        [SerializeField] private bool showSettings;
 
        private readonly List<GameplayTag> tagBuffer = new List<GameplayTag>();
        private readonly List<AbilitySystemComponent> candidates = new List<AbilitySystemComponent>();
        private static readonly System.Comparison<AbilitySystemComponent> CandidateComparison = CompareCandidates;
 
        private SerializedObject serialized;
        private AbilitySystemComponent target;
        private Vector2 scroll;
        private double nextSearchTime;
 
        // ---------- Opening ----------
 
        [MenuItem("Window/Ability System/Debugger")]
        public static void Open()
        {
            GetWindow<AbilityDebuggerWindow>("Ability Debugger");
        }
 
        // Rebind them in Edit > Shortcuts if the key doesn't work with your keyboard layout.
        [Shortcut("Ability System/Toggle Debugger", KeyCode.LeftBracket)]
        private static void ToggleShortcut()
        {
            if (HasOpenInstances<AbilityDebuggerWindow>())
                GetWindow<AbilityDebuggerWindow>().Close();
            else
                Open();
        }
 
        // ---------- Lifecycle ----------
 
        private void OnEnable()
        {
            serialized = new SerializedObject(this);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }
 
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Selection.selectionChanged -= OnSelectionChanged;
        }
 
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Scene objects are new in every Play session.
            target = null;
            candidates.Clear();
            nextSearchTime = 0;
            Repaint();
        }
 
        private void OnSelectionChanged()
        {
            if (!followSelection || !Application.isPlaying)
                return;
 
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return;
 
            AbilitySystemComponent found = selected.GetComponentInParent<AbilitySystemComponent>();
 
            // The scene check skips prefab assets: touching them would initialize the asset itself.
            if (found != null && found.gameObject.scene.IsValid())
            {
                target = found;
                Repaint();
            }
        }
 
        // Called about 10 times per second. Searching and repainting here keeps OnGUI free of side effects.
        private void OnInspectorUpdate()
        {
            if (!Application.isPlaying)
                return;
 
            if (target == null && EditorApplication.timeSinceStartup >= nextSearchTime)
            {
                nextSearchTime = EditorApplication.timeSinceStartup + SearchInterval;
                target = FindDefaultTarget();
            }
 
            Repaint();
        }
 
        // ---------- Targets ----------
 
        private AbilitySystemComponent FindDefaultTarget()
        {
            RefreshCandidates();
 
            for (int i = 0; i < candidates.Count; i++)
            {
                if (MatchesDefaultTarget(candidates[i]))
                    return candidates[i];
            }
 
            return null;
        }
 
        private bool MatchesDefaultTarget(AbilitySystemComponent component)
        {
            GameObject go = component.gameObject;
            
            if (!string.IsNullOrEmpty(playerTag) && !go.CompareTag(playerTag))
                return false;
 
            if (playerLayer.value != 0 && (playerLayer.value & (1 << go.layer)) == 0)
                return false;
 
            return true;
        }
 
        internal void SelectNextTarget()
        {
            if (!Application.isPlaying)
                return;
 
            RefreshCandidates();
 
            if (candidates.Count == 0)
            {
                target = null;
                return;
            }
 
            // If the current target isn't in the list (or was destroyed), IndexOf returns -1 and we start at 0.
            int index = target != null ? candidates.IndexOf(target) : -1;
            target = candidates[(index + 1) % candidates.Count];
            Repaint();
        }
 
        private void RefreshCandidates()
        {
            candidates.Clear();
            candidates.AddRange(AbilitySystemComponent.Instances);
            candidates.Sort(CandidateComparison);
        }
        
        private static int CompareCandidates(AbilitySystemComponent a, AbilitySystemComponent b)
        {
            int byName = string.CompareOrdinal(a.name, b.name);
            if (byName != 0)
                return byName;

            int bySibling = a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            if (bySibling != 0)
                return bySibling;

            Vector3 pa = a.transform.position;
            Vector3 pb = b.transform.position;

            int byX = pa.x.CompareTo(pb.x);
            if (byX != 0)
                return byX;

            int byY = pa.y.CompareTo(pb.y);
            return byY != 0 ? byY : pa.z.CompareTo(pb.z);
        }
 
        // ---------- Drawing ----------
 
        private void OnGUI()
        {
            DrawToolbar();
            DrawSettings();
 
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see the Ability Systems.", MessageType.Info);
                return;
            }
 
            scroll = EditorGUILayout.BeginScrollView(scroll);
 
            if (target == null)
            {
                EditorGUILayout.HelpBox(
                    $"No target found. Looked for an AbilitySystemComponent with tag '{playerTag}'. " +
                    "Use Next to pick another one.",
                    MessageType.Info);
            }
            else
            {
                DrawTarget(target);
            }
 
            EditorGUILayout.EndScrollView();
        }
 
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Player", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        target = FindDefaultTarget();
 
                    if (GUILayout.Button("Next", EditorStyles.toolbarButton, GUILayout.Width(60)))
                        SelectNextTarget();
                }
 
                GUILayout.FlexibleSpace();
 
                followSelection = GUILayout.Toggle(
                    followSelection, "Follow selection", EditorStyles.toolbarButton, GUILayout.Width(110));
            }
        }
 
        private void DrawSettings()
        {
            showSettings = EditorGUILayout.Foldout(showSettings, "Default target", true);
            if (!showSettings)
                return;
 
            serialized.Update();
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.FindProperty("playerTag"));
            EditorGUILayout.PropertyField(serialized.FindProperty("playerLayer"));
            EditorGUI.indentLevel--;
            serialized.ApplyModifiedProperties();
        }
 
        private void DrawTarget(AbilitySystemComponent asc)
        {
            int index = candidates.IndexOf(asc);
            string pos = index >= 0 ? $" ({index + 1}/{candidates.Count})" : string.Empty;
            EditorGUILayout.LabelField("== " + asc.name + " ==" + pos, EditorStyles.boldLabel);
 
            AttributeSet set = asc.Attributes;
            for (int i = 0; i < set.count; i++)
                EditorGUILayout.LabelField($"{set[i].Tag.Name}: {set[i].currentValue} / {set[i].MaxValue}");
 
            EditorGUILayout.Space();
 
            AbilityContainer abilities = asc.Abilities;
            for (int i = 0; i < abilities.GrantedCount; i++)
            {
                GameplayAbility a = abilities.GetGrantedAt(i);
                EditorGUILayout.LabelField(
                    $"{a.Tag.Name} | active: {a.IsActive} | can activate: {a.CanActivate()} | cooldown: {a.CooldownRemaining:0.0}");
            }
            
            EditorGUILayout.Space();

            ActiveEffectContainer activeEffects = asc.Effects;
            EditorGUILayout.LabelField("Effects: " + activeEffects.Count);
            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveGameplayEffect e = activeEffects.GetActiveAt(i);
                string from = e.Instigator != null ? e.Instigator.name : "-";
                string time = float.IsPositiveInfinity(e.RemainingTime) ? "inf" : e.RemainingTime.ToString("0.0");
                EditorGUILayout.LabelField(
                    $"  {e.Definition.name} x{e.Stacks} | from: {from} | remaining: {time}{(e.IsSuspended ? " | SUSPENDED" : "")}");
            }
 
            EditorGUILayout.Space();
 
            tagBuffer.Clear();
            asc.OwnedTags.AppendTagsTo(tagBuffer);
 
            EditorGUILayout.LabelField("Tags: " + tagBuffer.Count);
            for (int i = 0; i < tagBuffer.Count; i++)
                EditorGUILayout.LabelField("  " + tagBuffer[i].Name);
            
            EditorGUILayout.Space();

            GameplayCueRunner cues = asc.Cues;
            EditorGUILayout.LabelField("Cues: " + cues.ActiveCount);
            for (int i = 0; i < cues.ActiveCount; i++)
                EditorGUILayout.LabelField($"  {cues.GetActiveTagAt(i).Name} x{cues.GetActiveCountAt(i)}");
        }
    }   
}
