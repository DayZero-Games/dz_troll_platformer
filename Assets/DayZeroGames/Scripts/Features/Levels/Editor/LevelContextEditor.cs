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
            AssignDefaultPuppetIds(_puppetsProperty);
            if (TryGetDuplicatePuppetId(_puppetsProperty, out var duplicateId))
            {
                EditorGUILayout.HelpBox(
                    $"Multiple puppets use the ID '{duplicateId}'. Give every puppet a unique ID.",
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
                var idProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.IdFieldName);
                var puppetProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.PuppetFieldName);
                var puppet = puppetProperty.objectReferenceValue as PuppetController;

                if (puppet == null) continue;

                var id = string.IsNullOrWhiteSpace(idProperty.stringValue)
                    ? puppet.gameObject.name
                    : idProperty.stringValue;
                var label = id == puppet.gameObject.name ? id : $"{id} ({puppet.gameObject.name})";
                options.Add(new ControlOption(false, id, label));
            }

            return options;
        }

        private static void AssignDefaultPuppetIds(SerializedProperty puppetsProperty)
        {
            if (puppetsProperty == null || !puppetsProperty.isArray) return;

            for (var i = 0; i < puppetsProperty.arraySize; i++)
            {
                var slotProperty = puppetsProperty.GetArrayElementAtIndex(i);
                var idProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.IdFieldName);
                var puppetProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.PuppetFieldName);
                var puppet = puppetProperty.objectReferenceValue as PuppetController;

                if (puppet != null && string.IsNullOrWhiteSpace(idProperty.stringValue))
                    idProperty.stringValue = puppet.gameObject.name;
            }
        }

        private static bool TryGetDuplicatePuppetId(
            SerializedProperty puppetsProperty,
            out string duplicateId)
        {
            var ids = new HashSet<string>();
            if (puppetsProperty != null && puppetsProperty.isArray)
            {
                for (var i = 0; i < puppetsProperty.arraySize; i++)
                {
                    var slotProperty = puppetsProperty.GetArrayElementAtIndex(i);
                    var idProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.IdFieldName);
                    var puppetProperty = slotProperty.FindPropertyRelative(LevelPuppetSlot.PuppetFieldName);
                    var puppet = puppetProperty.objectReferenceValue as PuppetController;
                    if (puppet == null) continue;

                    var id = idProperty.stringValue;
                    if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    {
                        duplicateId = id;
                        return true;
                    }
                }
            }

            duplicateId = null;
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
