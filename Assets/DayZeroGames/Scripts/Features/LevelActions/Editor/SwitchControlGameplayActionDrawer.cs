using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    [CustomPropertyDrawer(typeof(SwitchControlGameplayAction))]
    public sealed class SwitchControlGameplayActionDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 38f;

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = LineHeight + Spacing + LineHeight;
            if (HasMissingPuppetSelection(property))
                height += Spacing + HelpBoxHeight;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var headerRect = new Rect(position.x, y, position.width, LineHeight);
            EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);

            y += LineHeight + Spacing;

            var targetProperty = property.FindPropertyRelative(SwitchControlGameplayAction.TargetFieldName);
            var puppetIdProperty = property.FindPropertyRelative(SwitchControlGameplayAction.PuppetIdFieldName);
            var targetRect = new Rect(position.x, y, position.width, LineHeight);
            DrawControlPopup(targetRect, property, targetProperty, puppetIdProperty);

            y += LineHeight + Spacing;

            if (HasMissingPuppetSelection(property))
            {
                var helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(
                    helpRect,
                    "The selected puppet is not registered on the nearest LevelContext.",
                    MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        private static bool HasMissingPuppetSelection(SerializedProperty property)
        {
            var targetProperty = property.FindPropertyRelative(SwitchControlGameplayAction.TargetFieldName);
            if (targetProperty == null || targetProperty.enumValueIndex == 0) return false;

            var puppetIdProperty = property.FindPropertyRelative(SwitchControlGameplayAction.PuppetIdFieldName);
            return FindSelectedOption(BuildControlOptions(property), targetProperty, puppetIdProperty) < 0;
        }

        private static void DrawControlPopup(
            Rect rect,
            SerializedProperty actionProperty,
            SerializedProperty targetProperty,
            SerializedProperty puppetIdProperty)
        {
            var options = BuildControlOptions(actionProperty);
            var selectedIndex = FindSelectedOption(options, targetProperty, puppetIdProperty);

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

            var nextIndex = EditorGUI.Popup(rect, "Control", selectedIndex, labels);
            ApplySelection(options[nextIndex], targetProperty, puppetIdProperty);
        }

        private static List<ControlOption> BuildControlOptions(SerializedProperty actionProperty)
        {
            var options = new List<ControlOption>
            {
                new(true, string.Empty, "Player")
            };

            var levelContext = FindLevelContext(actionProperty.serializedObject.targetObject);
            if (levelContext == null) return options;

            foreach (var slot in levelContext.PuppetSlots)
            {
                if (slot == null || !slot.IsValid) continue;

                var id = string.IsNullOrWhiteSpace(slot.Id)
                    ? slot.Puppet.gameObject.name
                    : slot.Id;
                var label = id == slot.Puppet.gameObject.name
                    ? id
                    : $"{id} ({slot.Puppet.gameObject.name})";
                options.Add(new ControlOption(false, id, label));
            }

            return options;
        }

        private static LevelContext FindLevelContext(Object targetObject)
        {
            return targetObject switch
            {
                Component component => component.GetComponentInParent<LevelContext>(true),
                GameObject gameObject => gameObject.GetComponentInParent<LevelContext>(true),
                _ => null
            };
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
