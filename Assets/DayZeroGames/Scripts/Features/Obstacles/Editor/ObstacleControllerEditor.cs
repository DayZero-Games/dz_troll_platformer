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

        private static readonly Dictionary<string, int> ActionMenuOrder = new()
        {
            { nameof(MoveToPointAction), 0 },
            { nameof(ReturnBackAction), 1 },
            { nameof(WaitAction), 2 },
            { nameof(DisableObjectAction), 3 },
            { nameof(CameraShakeAction), 4 }
        };

        private SerializedProperty _performersProperty;
        private SerializedProperty _executionModeProperty;
        private ReorderableList _groupList;
        private GameObject _pendingPerformer;
        private int _groupPendingRemoval = -1;
        private string _addMessage;
        private MessageType _addMessageType = MessageType.None;
        private readonly Dictionary<string, ReorderableList> _actionListsByPath = new();

        private static List<Type> _actionTypes;

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

        private ReorderableList CreateActionList(SerializedProperty actionsProperty)
        {
            var list = new ReorderableList(serializedObject, actionsProperty, true, true, true, true);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Actions");
            list.elementHeightCallback = index => GetActionHeight(actionsProperty, index);
            list.drawElementCallback = (rect, index, _, _) => DrawActionElement(actionsProperty, rect, index);
            list.onAddDropdownCallback = (buttonRect, _) => ShowActionMenu(buttonRect, actionsProperty.propertyPath);
            list.onReorderCallback = _ => serializedObject.ApplyModifiedProperties();
            list.onRemoveCallback = removedFrom =>
            {
                if (removedFrom.index < 0 || removedFrom.index >= actionsProperty.arraySize) return;

                actionsProperty.DeleteArrayElementAtIndex(removedFrom.index);
                serializedObject.ApplyModifiedProperties();
            };

            return list;
        }

        private static float GetActionHeight(SerializedProperty actionsProperty, int index)
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

        private void ShowActionMenu(Rect buttonRect, string actionsPropertyPath)
        {
            var menu = new GenericMenu();
            var actionTypes = GetActionTypes();

            if (actionTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No action types found"));
                menu.DropDown(buttonRect);
                return;
            }

            foreach (var actionType in actionTypes)
            {
                var typeToAdd = actionType;
                menu.AddItem(
                    new GUIContent(MakeActionTypeLabel(typeToAdd)),
                    false,
                    () => AddAction(actionsPropertyPath, typeToAdd));
            }

            menu.DropDown(buttonRect);
        }

        private void AddAction(string actionsPropertyPath, Type actionType)
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
            Repaint();
        }

        private static List<Type> GetActionTypes()
        {
            if (_actionTypes != null) return _actionTypes;

            _actionTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ObstacleAction>())
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                _actionTypes.Add(type);
            }

            _actionTypes.Sort(CompareActionTypes);

            return _actionTypes;
        }

        private static int CompareActionTypes(Type left, Type right)
        {
            var leftOrder = GetActionMenuOrder(left);
            var rightOrder = GetActionMenuOrder(right);
            if (leftOrder != rightOrder)
                return leftOrder.CompareTo(rightOrder);

            return string.Compare(
                MakeActionTypeLabel(left),
                MakeActionTypeLabel(right),
                StringComparison.Ordinal);
        }

        private static int GetActionMenuOrder(Type actionType)
        {
            return ActionMenuOrder.TryGetValue(actionType.Name, out var order)
                ? order
                : int.MaxValue;
        }

        private static GUIContent MakeActionInstanceLabel(SerializedProperty actionProperty)
        {
            if (actionProperty.managedReferenceValue is ObstacleAction action)
                return new GUIContent(action.Describe());

            return new GUIContent("Missing action");
        }

        private static string MakeActionTypeLabel(Type actionType)
        {
            if (actionType.Name == nameof(DisableObjectAction))
                return "Disable";

            var name = actionType.Name;
            if (name.EndsWith("Action", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Action".Length);

            return ObjectNames.NicifyVariableName(name);
        }
    }
}
