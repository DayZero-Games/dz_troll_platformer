using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    [CustomEditor(typeof(LevelActionSequenceController))]
    public class LevelActionSequenceControllerEditor : UnityEditor.Editor
    {
        private const string ExecutionModeFieldName = "_executionMode";
        private const string AutoStartFieldName = "_autoStart";
        private const string OneShotFieldName = "_oneShot";
        private const string GroupsFieldName = "_groups";

        private const float ElementPadding = 4f;
        private const float GroupContentLeftOffset = 12f;
        private const float ActionListLeftOffset = 10f;
        private const float DismissButtonWidth = 22f;
        private const float MissingTargetWarningHeight = 42f;

        private static readonly GUIContent RemoveGroupLabel = new(
            "x",
            "Remove this action group.");

        private SerializedProperty _executionModeProperty;
        private SerializedProperty _autoStartProperty;
        private SerializedProperty _oneShotProperty;
        private SerializedProperty _groupsProperty;
        private ReorderableList _groupList;
        private int _groupPendingRemoval = -1;
        private readonly Dictionary<string, ReorderableList> _actionListsByPath = new();

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        private void OnEnable()
        {
            _executionModeProperty = serializedObject.FindProperty(ExecutionModeFieldName);
            _autoStartProperty = serializedObject.FindProperty(AutoStartFieldName);
            _oneShotProperty = serializedObject.FindProperty(OneShotFieldName);
            _groupsProperty = serializedObject.FindProperty(GroupsFieldName);

            _groupPendingRemoval = -1;
            _actionListsByPath.Clear();
            _groupList = CreateGroupList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            if (_autoStartProperty != null)
                EditorGUILayout.PropertyField(_autoStartProperty);

            if (_oneShotProperty != null)
                EditorGUILayout.PropertyField(_oneShotProperty);

            if (_executionModeProperty != null)
                EditorGUILayout.PropertyField(_executionModeProperty);

            EditorGUILayout.HelpBox(
                "Each group runs one action list. Assign a Target for object actions, or leave Target empty for gameplay/global actions.",
                MessageType.None);

            _groupList.DoLayoutList();

            if (_groupPendingRemoval >= 0)
            {
                RemoveGroup(_groupPendingRemoval);
                _groupPendingRemoval = -1;
            }

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

        private ReorderableList CreateGroupList()
        {
            var list = new ReorderableList(serializedObject, _groupsProperty, true, true, true, false);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Groups");
            list.elementHeightCallback = GetGroupHeight;
            list.drawElementCallback = DrawGroupElement;
            list.onAddCallback = _ => AddGroup();
            list.onReorderCallback = _ =>
            {
                serializedObject.ApplyModifiedProperties();
                _actionListsByPath.Clear();
            };

            return list;
        }

        private void AddGroup()
        {
            serializedObject.Update();

            var index = _groupsProperty.arraySize;
            _groupsProperty.arraySize++;

            var groupProperty = _groupsProperty.GetArrayElementAtIndex(index);
            groupProperty
                .FindPropertyRelative(LevelActionGroup.TargetFieldName)
                .objectReferenceValue = null;
            groupProperty
                .FindPropertyRelative(LevelActionGroup.ActionsFieldName)
                .arraySize = 0;

            serializedObject.ApplyModifiedProperties();
            _actionListsByPath.Clear();
            _groupList.index = index;
        }

        private void RemoveGroup(int index)
        {
            if (index < 0 || index >= _groupsProperty.arraySize) return;

            _groupsProperty.DeleteArrayElementAtIndex(index);
            _groupList.index = -1;
            serializedObject.ApplyModifiedProperties();
            _actionListsByPath.Clear();
            Repaint();
        }

        private float GetGroupHeight(int index)
        {
            if (index < 0 || index >= _groupsProperty.arraySize) return LineHeight;

            var groupProperty = _groupsProperty.GetArrayElementAtIndex(index);
            var targetProperty = groupProperty.FindPropertyRelative(LevelActionGroup.TargetFieldName);
            var actionsProperty = groupProperty.FindPropertyRelative(LevelActionGroup.ActionsFieldName);
            var actionList = GetActionList(actionsProperty);
            var height = ElementPadding + LineHeight + Spacing;

            if (HasMissingRequiredTarget(targetProperty, actionsProperty, out _))
                height += MissingTargetWarningHeight + Spacing;

            return height + actionList.GetHeight() + ElementPadding;
        }

        private void DrawGroupElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _groupsProperty.arraySize) return;

            var groupProperty = _groupsProperty.GetArrayElementAtIndex(index);
            var targetProperty = groupProperty.FindPropertyRelative(LevelActionGroup.TargetFieldName);
            var actionsProperty = groupProperty.FindPropertyRelative(LevelActionGroup.ActionsFieldName);

            var contentX = rect.x + GroupContentLeftOffset;
            var contentWidth = rect.width - GroupContentLeftOffset;
            var y = rect.y + ElementPadding;

            var targetRect = new Rect(contentX, y, contentWidth - DismissButtonWidth - Spacing, LineHeight);
            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"));

            var dismissRect = new Rect(rect.xMax - DismissButtonWidth, y, DismissButtonWidth, LineHeight);
            if (GUI.Button(dismissRect, RemoveGroupLabel))
            {
                _groupPendingRemoval = index;
                return;
            }

            y += LineHeight + Spacing;
            if (HasMissingRequiredTarget(targetProperty, actionsProperty, out var requiredActionNames))
            {
                var warningRect = new Rect(contentX, y, contentWidth, MissingTargetWarningHeight);
                EditorGUI.HelpBox(
                    warningRect,
                    $"Assign a Target. Required by: {requiredActionNames}.",
                    MessageType.Warning);
                y += MissingTargetWarningHeight + Spacing;
            }

            var actionList = GetActionList(actionsProperty);
            actionList.DoList(new Rect(
                contentX + ActionListLeftOffset,
                y,
                contentWidth - ActionListLeftOffset,
                actionList.GetHeight()));
        }

        private ReorderableList GetActionList(SerializedProperty actionsProperty)
        {
            var propertyPath = actionsProperty.propertyPath;

            if (_actionListsByPath.TryGetValue(propertyPath, out var actionList) &&
                actionList.serializedProperty != null &&
                actionList.serializedProperty.serializedObject == actionsProperty.serializedObject)
            {
                actionList.serializedProperty = actionsProperty;
                return actionList;
            }

            actionList = LevelActionListUI.CreateActionList(
                actionsProperty,
                "Actions",
                () =>
                {
                    _actionListsByPath.Clear();
                    Repaint();
                });
            _actionListsByPath[propertyPath] = actionList;
            return actionList;
        }

        private static bool HasMissingRequiredTarget(
            SerializedProperty targetProperty,
            SerializedProperty actionsProperty,
            out string requiredActionNames)
        {
            requiredActionNames = string.Empty;

            if (targetProperty == null ||
                targetProperty.objectReferenceValue != null ||
                actionsProperty == null)
            {
                return false;
            }

            var names = new List<string>();
            for (var i = 0; i < actionsProperty.arraySize; i++)
            {
                LevelAction action;
                try
                {
                    var actionProperty = actionsProperty.GetArrayElementAtIndex(i);
                    action = actionProperty.managedReferenceValue as LevelAction;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }

                if (action == null || !action.RequiresTarget) continue;

                names.Add(MakeActionName(action));
            }

            requiredActionNames = string.Join(", ", names);
            return names.Count > 0;
        }

        private static string MakeActionName(LevelAction action)
        {
            var name = action.GetType().Name;

            if (name.EndsWith("GameplayAction", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "GameplayAction".Length);
            else if (name.EndsWith("LevelAction", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "LevelAction".Length);
            else if (name.EndsWith("Action", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Action".Length);

            return ObjectNames.NicifyVariableName(name);
        }
    }
}
