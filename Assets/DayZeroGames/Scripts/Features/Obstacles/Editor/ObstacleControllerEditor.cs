using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    [CustomEditor(typeof(ObstacleController))]
    public class ObstacleControllerEditor : UnityEditor.Editor
    {
        private const string PerformersFieldName = "_performers";
        private const string ExecutionModeFieldName = "_executionMode";
        private const float ElementPadding = 4f;
        private const float GroupContentLeftOffset = 12f;
        private const float ActionListLeftOffset = 10f;
        private const float ActionFieldLeftOffset = 24f;
        private const float DismissButtonWidth = 22f;
        private const float AddButtonWidth = 60f;

        private static readonly GUIContent AddPerformerLabel = new(
            "Add Performer",
            "A GameObject that will execute the actions listed in its performer block.");

        private static readonly GUIContent RemoveGroupLabel = new(
            "x",
            "Remove this performer block.");

        private SerializedProperty _performersProperty;
        private SerializedProperty _executionModeProperty;
        private ReorderableList _groupList;
        private GameObject _pendingPerformer;
        private int _groupPendingRemoval = -1;
        private string _addMessage;
        private MessageType _addMessageType = MessageType.None;
        private readonly Dictionary<string, ReorderableList> _actionListsByPath = new();


        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        private void OnEnable()
        {
            _performersProperty = serializedObject.FindProperty(PerformersFieldName);
            _executionModeProperty = serializedObject.FindProperty(ExecutionModeFieldName);

            _pendingPerformer = null;
            _groupPendingRemoval = -1;
            _addMessage = null;
            _addMessageType = MessageType.None;
            _actionListsByPath.Clear();

            _groupList = CreateGroupList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            if (_executionModeProperty != null)
                EditorGUILayout.PropertyField(_executionModeProperty);

            DrawAddPerformerRow();

            EditorGUILayout.HelpBox(
                "Each performer runs its own action list from top to bottom. Sequential mode runs performer blocks one after another; parallel mode starts all performer blocks together.",
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

        private void DrawAddPerformerRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var performer = (GameObject)EditorGUILayout.ObjectField(
                    AddPerformerLabel, _pendingPerformer, typeof(GameObject), true);

                if (EditorGUI.EndChangeCheck())
                {
                    _pendingPerformer = performer;
                    ClearMessage();
                }

                using (new EditorGUI.DisabledScope(_pendingPerformer == null))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(AddButtonWidth)))
                        AddGroupFor(_pendingPerformer);
                }
            }

            if (!string.IsNullOrEmpty(_addMessage))
                EditorGUILayout.HelpBox(_addMessage, _addMessageType);
        }

        private void AddGroupFor(GameObject performer)
        {
            if (performer == null) return;

            serializedObject.Update();

            if (ContainsPerformer(performer))
            {
                _addMessage = $"'{performer.name}' is already listed below.";
                _addMessageType = MessageType.Info;
                return;
            }

            var index = _performersProperty.arraySize;
            _performersProperty.arraySize++;

            var groupProperty = _performersProperty.GetArrayElementAtIndex(index);
            groupProperty
                .FindPropertyRelative(ObstaclePerformerActions.PerformerFieldName)
                .objectReferenceValue = performer;
            groupProperty
                .FindPropertyRelative(ObstaclePerformerActions.ActionsFieldName)
                .arraySize = 0;

            serializedObject.ApplyModifiedProperties();
            _actionListsByPath.Clear();

            ClearMessage();
            _pendingPerformer = null;
            GUI.FocusControl(null);
        }

        private bool ContainsPerformer(GameObject performer)
        {
            for (var i = 0; i < _performersProperty.arraySize; i++)
            {
                var performerProperty = _performersProperty
                    .GetArrayElementAtIndex(i)
                    .FindPropertyRelative(ObstaclePerformerActions.PerformerFieldName);

                if (performerProperty.objectReferenceValue == performer)
                    return true;
            }

            return false;
        }

        private void RemoveGroup(int index)
        {
            if (index < 0 || index >= _performersProperty.arraySize) return;

            _performersProperty.DeleteArrayElementAtIndex(index);
            _groupList.index = -1;
            serializedObject.ApplyModifiedProperties();
            _actionListsByPath.Clear();
            Repaint();
        }

        private void ClearMessage()
        {
            _addMessage = null;
            _addMessageType = MessageType.None;
        }

        private ReorderableList CreateGroupList()
        {
            var list = new ReorderableList(serializedObject, _performersProperty, true, true, false, false);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Performers");
            list.elementHeightCallback = GetGroupHeight;
            list.drawElementCallback = DrawGroupElement;
            list.onReorderCallback = _ =>
            {
                serializedObject.ApplyModifiedProperties();
                _actionListsByPath.Clear();
            };

            return list;
        }

        private float GetGroupHeight(int index)
        {
            if (index < 0 || index >= _performersProperty.arraySize) return LineHeight;

            var groupProperty = _performersProperty.GetArrayElementAtIndex(index);
            var actionsProperty = groupProperty.FindPropertyRelative(ObstaclePerformerActions.ActionsFieldName);
            var actionList = GetActionList(actionsProperty);

            return ElementPadding + LineHeight + Spacing + actionList.GetHeight() + ElementPadding;
        }

        private void DrawGroupElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _performersProperty.arraySize) return;

            var groupProperty = _performersProperty.GetArrayElementAtIndex(index);
            var performerProperty = groupProperty.FindPropertyRelative(ObstaclePerformerActions.PerformerFieldName);
            var actionsProperty = groupProperty.FindPropertyRelative(ObstaclePerformerActions.ActionsFieldName);

            var contentX = rect.x + GroupContentLeftOffset;
            var contentWidth = rect.width - GroupContentLeftOffset;
            var y = rect.y + ElementPadding;
            var performerRect = new Rect(contentX, y, contentWidth - DismissButtonWidth - Spacing, LineHeight);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(performerRect, performerProperty, new GUIContent("Performer"));
            }

            var dismissRect = new Rect(rect.xMax - DismissButtonWidth, y, DismissButtonWidth, LineHeight);
            if (GUI.Button(dismissRect, RemoveGroupLabel))
                _groupPendingRemoval = index;

            y += LineHeight + Spacing;
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
            if (_actionListsByPath.TryGetValue(propertyPath, out var actionList))
                return actionList;

            actionList = CreateActionList(actionsProperty);
            _actionListsByPath.Add(propertyPath, actionList);
            return actionList;
        }

        private ReorderableList CreateActionList(SerializedProperty actionsProperty) =>
            ObstacleActionListUI.CreateActionList(actionsProperty, "Actions");

    }
}
