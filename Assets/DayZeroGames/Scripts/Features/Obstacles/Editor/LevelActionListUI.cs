using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    internal static class LevelActionListUI
    {
        public const float ElementPadding = 4f;
        public const float ActionFieldLeftOffset = 24f;

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        private static readonly Dictionary<string, int> MenuOrder = new()
        {
            { nameof(MoveToPointAction), 0 },
            { nameof(ReturnBackAction), 1 },
            { nameof(WaitAction), 2 },
            { nameof(DisableObjectAction), 3 },
            { nameof(CameraShakeAction), 4 },
            { nameof(SetInvertControlsGameplayAction), 10 },
            { nameof(SetGravityScaleGameplayAction), 11 },
            { nameof(SetJumpRulesGameplayAction), 12 },
            { nameof(SetJumpEnabledGameplayAction), 13 },
            { nameof(SwitchControlGameplayAction), 14 },
            { nameof(ApplyLevelRulesGameplayAction), 15 },
            { nameof(RestoreCatalogRulesGameplayAction), 16 },
            { nameof(LevelLoopAction), 30 }
        };

        private static List<Type> _actionTypes;

        public static ReorderableList CreateActionList(SerializedProperty actionsProperty, string header)
        {
            ReorderableList list = null;
            list = new ReorderableList(actionsProperty.serializedObject, actionsProperty, true, true, true, true);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header);
            list.elementHeightCallback = index => GetActionHeight(list.serializedProperty, index);
            list.drawElementCallback = (rect, index, _, _) => DrawActionElement(list.serializedProperty, rect, index);
            list.onAddDropdownCallback = (buttonRect, _) =>
            {
                var serializedObject = list.serializedProperty.serializedObject;
                var actionsPath = list.serializedProperty.propertyPath;
                ShowAddMenu(buttonRect, typeToAdd => AppendAction(serializedObject, actionsPath, typeToAdd));
            };
            list.onReorderCallback = _ => list.serializedProperty.serializedObject.ApplyModifiedProperties();
            list.onRemoveCallback = removedFrom =>
            {
                var actions = list.serializedProperty;
                if (removedFrom.index < 0 || removedFrom.index >= actions.arraySize) return;

                actions.DeleteArrayElementAtIndex(removedFrom.index);
                actions.serializedObject.ApplyModifiedProperties();
            };

            return list;
        }

        public static float GetActionHeight(SerializedProperty actionsProperty, int index)
        {
            if (index < 0 || index >= actionsProperty.arraySize) return LineHeight;

            var actionProperty = actionsProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(actionProperty, true) + Spacing + ElementPadding;
        }

        private static void DrawActionElement(SerializedProperty actionsProperty, Rect rect, int index)
        {
            if (index < 0 || index >= actionsProperty.arraySize) return;

            var actionProperty = actionsProperty.GetArrayElementAtIndex(index);
            var label = MakeActionInstanceLabel(actionProperty);
            var fieldRect = new Rect(
                rect.x + ActionFieldLeftOffset,
                rect.y + Spacing * 0.5f,
                rect.width - ActionFieldLeftOffset,
                rect.height - Spacing - ElementPadding);

            EditorGUI.PropertyField(fieldRect, actionProperty, label, true);
        }

        private static void ShowAddMenu(Rect buttonRect, Action<Type> onPick)
        {
            var menu = new GenericMenu();
            var actionTypes = GetActionTypes();

            if (actionTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No level action types found"));
                menu.DropDown(buttonRect);
                return;
            }

            foreach (var actionType in actionTypes)
            {
                var typeToAdd = actionType;
                menu.AddItem(new GUIContent(MakeActionTypeLabel(typeToAdd)), false, () => onPick(typeToAdd));
            }

            menu.DropDown(buttonRect);
        }

        private static List<Type> GetActionTypes()
        {
            if (_actionTypes != null) return _actionTypes;

            _actionTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<LevelAction>())
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                _actionTypes.Add(type);
            }

            _actionTypes.Sort(CompareActionTypes);

            return _actionTypes;
        }

        private static string MakeActionTypeLabel(Type actionType)
        {
            var name = actionType.Name;

            if (name == nameof(DisableObjectAction))
                return "Target/Disable";
            if (name == nameof(MoveToPointAction))
                return "Target/Move To Point";
            if (name == nameof(ReturnBackAction))
                return "Target/Return Back";
            if (name == nameof(CameraShakeAction))
                return "Feedback/Camera Shake";
            if (name == nameof(WaitAction))
                return "Flow/Wait";
            if (name == nameof(LevelLoopAction))
                return "Flow/Loop";

            if (name.StartsWith("Set", StringComparison.Ordinal))
                name = name.Substring("Set".Length);

            if (name.EndsWith("GameplayAction", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "GameplayAction".Length);
            else if (name.EndsWith("LevelAction", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "LevelAction".Length);
            else if (name.EndsWith("Action", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Action".Length);

            return $"Gameplay/{ObjectNames.NicifyVariableName(name)}";
        }

        private static GUIContent MakeActionInstanceLabel(SerializedProperty actionProperty)
        {
            if (actionProperty.managedReferenceValue is LevelAction action)
                return new GUIContent(action.Describe());

            return new GUIContent("Missing level action");
        }

        private static void AppendAction(SerializedObject serializedObject, string actionsPropertyPath, Type actionType)
        {
            serializedObject.Update();

            var actionsProperty = serializedObject.FindProperty(actionsPropertyPath);
            if (actionsProperty == null) return;

            var index = actionsProperty.arraySize;
            actionsProperty.arraySize++;

            var actionProperty = actionsProperty.GetArrayElementAtIndex(index);
            actionProperty.managedReferenceValue = Activator.CreateInstance(actionType);
            actionProperty.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            InternalEditorUtility.RepaintAllViews();
        }

        private static int CompareActionTypes(Type left, Type right)
        {
            var leftOrder = GetMenuOrder(left);
            var rightOrder = GetMenuOrder(right);
            if (leftOrder != rightOrder)
                return leftOrder.CompareTo(rightOrder);

            return string.Compare(
                MakeActionTypeLabel(left),
                MakeActionTypeLabel(right),
                StringComparison.Ordinal);
        }

        private static int GetMenuOrder(Type actionType) =>
            MenuOrder.TryGetValue(actionType.Name, out var order) ? order : int.MaxValue;
    }
}
