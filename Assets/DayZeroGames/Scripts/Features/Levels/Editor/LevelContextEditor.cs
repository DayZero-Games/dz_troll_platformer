using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    [CustomEditor(typeof(LevelContext))]
    public sealed class LevelContextEditor : UnityEditor.Editor
    {
        private SerializedProperty _spawnPointProperty;
        private SerializedProperty _puppetsProperty;
        private SerializedProperty _startControlTargetProperty;
        private SerializedProperty _startPuppetIdProperty;

        private void OnEnable()
        {
            _spawnPointProperty = serializedObject.FindProperty(LevelContext.SpawnPointFieldName);
            _puppetsProperty = serializedObject.FindProperty(LevelContext.PuppetsFieldName);
            _startControlTargetProperty = serializedObject.FindProperty(LevelContext.StartControlTargetFieldName);
            _startPuppetIdProperty = serializedObject.FindProperty(LevelContext.StartPuppetIdFieldName);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            EditorGUILayout.PropertyField(_spawnPointProperty);
            EditorGUILayout.PropertyField(_puppetsProperty, new GUIContent("Puppets"), true);
            if (TryGetDuplicatePuppetName(_puppetsProperty, out var duplicateName))
            {
                EditorGUILayout.HelpBox(
                    $"Multiple puppets are named '{duplicateName}'. Rename their GameObjects so every puppet name is unique.",
                    MessageType.Error);
            }

            EditorGUILayout.Space();
            DrawControlPopup(
                _startControlTargetProperty,
                _startPuppetIdProperty,
                _puppetsProperty,
                new GUIContent("Start Control"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            var scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty == null) return;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(scriptProperty);
            }
        }

        private static void DrawControlPopup(
            SerializedProperty targetProperty,
            SerializedProperty puppetIdProperty,
            SerializedProperty puppetsProperty,
            GUIContent label)
        {
            var options = BuildControlOptions(puppetsProperty);
            var selectedIndex = FindSelectedOption(options, targetProperty, puppetIdProperty);
            var registeredOptionCount = options.Count;

            if (selectedIndex < 0)
            {
                options.Add(new ControlOption(
                    false,
                    puppetIdProperty.stringValue,
                    string.IsNullOrWhiteSpace(puppetIdProperty.stringValue)
                        ? "Missing Puppet"
                        : $"Missing: {puppetIdProperty.stringValue}"));
                selectedIndex = options.Count - 1;
            }

            var labels = new string[options.Count];
            for (var i = 0; i < options.Count; i++)
                labels[i] = options[i].Label;

            var nextIndex = EditorGUILayout.Popup(label, selectedIndex, labels);
            ApplySelection(options[nextIndex], targetProperty, puppetIdProperty);

            if (nextIndex >= registeredOptionCount)
                EditorGUILayout.HelpBox("The selected puppet is not registered in this level.", MessageType.Warning);
        }

        private static List<ControlOption> BuildControlOptions(SerializedProperty puppetsProperty)
        {
            var options = new List<ControlOption>
            {
                new(true, string.Empty, "Player")
            };

            if (puppetsProperty == null || !puppetsProperty.isArray) return options;

            for (var i = 0; i < puppetsProperty.arraySize; i++)
            {
                var slotProperty = puppetsProperty.GetArrayElementAtIndex(i);
                var puppetProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.PuppetFieldName);
                var puppet = puppetProperty.objectReferenceValue as PuppetController;

                if (puppet == null) continue;

                var puppetName = puppet.gameObject.name;
                options.Add(new ControlOption(false, puppetName, puppetName));
            }

            return options;
        }

        private static bool TryGetDuplicatePuppetName(
            SerializedProperty puppetsProperty,
            out string duplicateName)
        {
            var names = new HashSet<string>();
            if (puppetsProperty != null && puppetsProperty.isArray)
            {
                for (var i = 0; i < puppetsProperty.arraySize; i++)
                {
                    var slotProperty = puppetsProperty.GetArrayElementAtIndex(i);
                    var puppetProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.PuppetFieldName);
                    var puppet = puppetProperty.objectReferenceValue as PuppetController;
                    if (puppet == null) continue;

                    var puppetName = puppet.gameObject.name;
                    if (!names.Add(puppetName))
                    {
                        duplicateName = puppetName;
                        return true;
                    }
                }
            }

            duplicateName = null;
            return false;
        }

        private static int FindSelectedOption(
            List<ControlOption> options,
            SerializedProperty targetProperty,
            SerializedProperty puppetIdProperty)
        {
            var isPlayer = targetProperty.enumValueIndex == 0;
            for (var i = 0; i < options.Count; i++)
            {
                if (isPlayer && options[i].IsPlayer) return i;
                if (!isPlayer &&
                    !options[i].IsPlayer &&
                    options[i].PuppetId == puppetIdProperty.stringValue)
                    return i;
            }

            return -1;
        }

        private static void ApplySelection(
            ControlOption option,
            SerializedProperty targetProperty,
            SerializedProperty puppetIdProperty)
        {
            targetProperty.enumValueIndex = option.IsPlayer ? 0 : 1;
            puppetIdProperty.stringValue = option.IsPlayer ? string.Empty : option.PuppetId;
        }

        private readonly struct ControlOption
        {
            public ControlOption(bool isPlayer, string puppetId, string label)
            {
                IsPlayer = isPlayer;
                PuppetId = puppetId;
                Label = label;
            }

            public bool IsPlayer { get; }
            public string PuppetId { get; }
            public string Label { get; }
        }
    }
}
