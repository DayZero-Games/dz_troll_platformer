using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DZ.Features.EditorTools
{
    /// <summary>
    /// Draws the controller's flat action array grouped by the object each action sits on.
    /// The grouping is derived, never stored: a group is a contiguous run of actions sharing a
    /// performer, so the flat array stays the single source of truth and nothing needs migrating.
    /// </summary>
    [CustomEditor(typeof(ObstacleController))]
    public class ObstacleControllerEditor : UnityEditor.Editor
    {
        private class PerformerGroup
        {
            public GameObject Performer;
            public bool IsPending;
            public readonly List<Object> Actions = new();
            public ReorderableList List;
        }

        private const string ActionsFieldName = "_obstacleActions";
        private const string ExecutionModeFieldName = "_executionMode";
        private const float ElementPadding = 4f;
        private const float DismissButtonWidth = 22f;
        private const float AddButtonWidth = 60f;

        private static readonly GUIContent AddPerformerLabel = new(
            "Add Performer",
            "A GameObject that already has ObstacleAction components on it. " +
            "Objects without any are rejected.");

        private static readonly GUIContent RemoveGroupLabel = new(
            "×",
            "Remove this performer block. The action components stay on the GameObject.");

        private readonly List<PerformerGroup> _groups = new();

        // Performers added from the top field that have no linked action yet. They cannot live in the
        // flat array, so they only exist here until their first action is picked.
        private readonly List<GameObject> _pendingPerformers = new();

        private SerializedProperty _actionsProperty;
        private SerializedProperty _executionModeProperty;
        private ReorderableList _groupList;
        private string _cachedSignature;
        private GameObject _pendingPerformer;
        private PerformerGroup _groupPendingRemoval;
        private string _addMessage;
        private MessageType _addMessageType = MessageType.None;

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        private void OnEnable()
        {
            _actionsProperty = serializedObject.FindProperty(ActionsFieldName);
            _executionModeProperty = serializedObject.FindProperty(ExecutionModeFieldName);

            _groups.Clear();
            _pendingPerformers.Clear();
            _cachedSignature = null;
            _addMessage = null;
            _pendingPerformer = null;

            _groupList = CreateGroupList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            if (_executionModeProperty != null) EditorGUILayout.PropertyField(_executionModeProperty);

            SyncGroups();
            DrawAddPerformerRow();

            EditorGUILayout.HelpBox(
                "Grouped by the object each action sits on. Top to bottom is execution order — " +
                "drag actions within a performer, or drag a whole performer.",
                MessageType.None);

            _groupList.DoLayoutList();

            if (_groupPendingRemoval != null)
            {
                RemoveGroup(_groupPendingRemoval);
                _groupPendingRemoval = null;
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

        // ── adding a performer ──────────────────────────────────────────────────────────────────

        private void DrawAddPerformerRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var performer = (GameObject)EditorGUILayout.ObjectField(
                    AddPerformerLabel, _pendingPerformer, typeof(GameObject), true);

                if (EditorGUI.EndChangeCheck()) AcceptPendingPerformer(performer);

                using (new EditorGUI.DisabledScope(_pendingPerformer == null))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(AddButtonWidth)))
                        AddEmptyGroupFor(_pendingPerformer);
                }
            }

            if (!string.IsNullOrEmpty(_addMessage)) EditorGUILayout.HelpBox(_addMessage, _addMessageType);
        }

        // Objects with no action on them never make it into the field in the first place.
        private void AcceptPendingPerformer(GameObject performer)
        {
            if (performer == null)
            {
                _pendingPerformer = null;
                ClearMessage();
                return;
            }

            if (performer.GetComponent<ObstacleAction>() == null)
            {
                _pendingPerformer = null;
                _addMessage = $"'{performer.name}' has no ObstacleAction component. Add one to it first.";
                _addMessageType = MessageType.Warning;
                return;
            }

            _pendingPerformer = performer;
            ClearMessage();
        }

        // Adds the performer as an empty block. Its actions are picked afterwards from the block's +.
        private void AddEmptyGroupFor(GameObject performer)
        {
            if (performer == null) return;

            if (FindGroupFor(performer) != null)
            {
                _addMessage = $"'{performer.name}' is already listed below.";
                _addMessageType = MessageType.Info;
                return;
            }

            _pendingPerformers.Add(performer);
            _cachedSignature = null;
            SyncGroups();

            ClearMessage();
            _pendingPerformer = null;
            GUI.FocusControl(null);
        }

        // Unlinks the whole block from the controller. The components are left on the performer.
        private void RemoveGroup(PerformerGroup group)
        {
            _pendingPerformers.Remove(group.Performer);

            if (group.Actions.Count == 0)
            {
                _cachedSignature = null;
                Repaint();
                return;
            }

            _groups.Remove(group);
            _groupList.index = -1;

            WriteBackFlatArray();
            Repaint();
        }

        private void ClearMessage()
        {
            _addMessage = null;
            _addMessageType = MessageType.None;
        }

        private PerformerGroup FindGroupFor(GameObject performer)
        {
            foreach (var group in _groups)
            {
                if (group.Performer == performer) return group;
            }

            return null;
        }

        // ── grouping ────────────────────────────────────────────────────────────────────────────

        // Rebuilding every frame would destroy the ReorderableLists mid-drag, so only rebuild when
        // the flat array actually changed.
        private void SyncGroups()
        {
            var signature = BuildSignature();
            if (signature == _cachedSignature) return;

            _cachedSignature = signature;
            RebuildGroups();
        }

        private string BuildSignature()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _actionsProperty.arraySize; i++)
            {
                var action = _actionsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                builder.Append(action != null ? action.GetInstanceID() : 0).Append(',');
            }

            return builder.ToString();
        }

        private void RebuildGroups()
        {
            _groups.Clear();
            PerformerGroup currentGroup = null;

            for (var i = 0; i < _actionsProperty.arraySize; i++)
            {
                var actionObject = _actionsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                var action = actionObject as ObstacleAction;
                var performer = action != null ? action.gameObject : null;

                // Strictly one block per object: an action never joins a block belonging to something else.
                if (currentGroup == null || performer != currentGroup.Performer)
                {
                    currentGroup = new PerformerGroup { Performer = performer };
                    _groups.Add(currentGroup);
                }

                currentGroup.Actions.Add(actionObject);
            }

            AppendPendingGroups();

            foreach (var group in _groups)
            {
                group.List = CreateActionList(group);
            }
        }

        private void AppendPendingGroups()
        {
            for (var i = _pendingPerformers.Count - 1; i >= 0; i--)
            {
                // Dropped once its first action lands in the array — the group is real from then on.
                if (_pendingPerformers[i] == null || FindGroupFor(_pendingPerformers[i]) != null)
                    _pendingPerformers.RemoveAt(i);
            }

            foreach (var performer in _pendingPerformers)
            {
                _groups.Add(new PerformerGroup { Performer = performer, IsPending = true });
            }
        }

        private void WriteBackFlatArray()
        {
            var ordered = new List<Object>();
            foreach (var group in _groups)
            {
                ordered.AddRange(group.Actions);
            }

            _actionsProperty.arraySize = ordered.Count;
            for (var i = 0; i < ordered.Count; i++)
            {
                _actionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            }

            serializedObject.ApplyModifiedProperties();
            _cachedSignature = null;
        }

        // ── performer groups ────────────────────────────────────────────────────────────────────

        private ReorderableList CreateGroupList()
        {
            var list = new ReorderableList(_groups, typeof(PerformerGroup), true, true, false, false);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Action Performers");
            list.elementHeightCallback = GetGroupHeight;
            list.drawElementCallback = DrawGroupElement;
            list.onReorderCallback = _ => WriteBackFlatArray();

            return list;
        }

        private float GetGroupHeight(int index)
        {
            if (index < 0 || index >= _groups.Count) return LineHeight;

            return ElementPadding + LineHeight + Spacing + _groups[index].List.GetHeight() + ElementPadding;
        }

        private void DrawGroupElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _groups.Count) return;

            var group = _groups[index];
            var y = rect.y + ElementPadding;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(
                    new Rect(rect.x, y, rect.width - DismissButtonWidth - Spacing, LineHeight),
                    "Performer", group.Performer, typeof(GameObject), true);
            }

            var dismissRect = new Rect(rect.xMax - DismissButtonWidth, y, DismissButtonWidth, LineHeight);

            // Deferred: removing a block mid-draw would mutate the list the inner ReorderableList is bound to.
            if (GUI.Button(dismissRect, RemoveGroupLabel)) _groupPendingRemoval = group;

            y += LineHeight + Spacing;
            group.List.DoList(new Rect(rect.x, y, rect.width, group.List.GetHeight()));
        }

        // ── actions inside a group ──────────────────────────────────────────────────────────────

        private ReorderableList CreateActionList(PerformerGroup group)
        {
            var list = new ReorderableList(group.Actions, typeof(Object), true, true, true, true);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Actions");
            list.elementHeight = LineHeight + Spacing;
            list.drawElementCallback = (rect, index, _, _) => DrawActionElement(group, rect, index);
            list.onReorderCallback = _ => WriteBackFlatArray();

            // Captures the performer, not the group: a rebuild between opening the menu and picking
            // an entry would leave a stale group object behind.
            list.onAddDropdownCallback = (buttonRect, _) => ShowPerformerActionsMenu(buttonRect, group.Performer);

            list.onRemoveCallback = removedFrom =>
            {
                if (removedFrom.index < 0 || removedFrom.index >= group.Actions.Count) return;

                // Only unlinks it from the controller — the component stays on the performer.
                group.Actions.RemoveAt(removedFrom.index);
                WriteBackFlatArray();
            };

            return list;
        }

        private static void DrawActionElement(PerformerGroup group, Rect rect, int index)
        {
            if (index < 0 || index >= group.Actions.Count) return;

            var labelRect = new Rect(rect.x, rect.y + Spacing * 0.5f, rect.width, LineHeight);
            var label = group.Actions[index] is ObstacleAction action ? action.Describe() : "⚠ Missing action";

            EditorGUI.LabelField(labelRect, label);
        }

        // ── the action dropdown ─────────────────────────────────────────────────────────────────

        // Lists the action components on the performer. Picking the same component again repeats it.
        private void ShowPerformerActionsMenu(Rect buttonRect, GameObject performer)
        {
            var menu = new GenericMenu();

            if (performer == null)
            {
                menu.AddDisabledItem(new GUIContent("This block has no performer"));
                menu.DropDown(buttonRect);
                return;
            }

            var actions = performer.GetComponents<ObstacleAction>();
            if (actions.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent($"No actions on '{performer.name}'"));
                menu.DropDown(buttonRect);
                return;
            }

            var usedLabels = new HashSet<string>();

            foreach (var action in actions)
            {
                var label = MakeUniqueLabel(action.Describe(), usedLabels);

                var actionToLink = action;
                menu.AddItem(new GUIContent(label), false, () => LinkAction(actionToLink));
            }

            menu.DropDown(buttonRect);
        }

        private void LinkAction(ObstacleAction action)
        {
            if (action == null) return;

            serializedObject.Update();
            SyncGroups();

            var performer = action.gameObject;
            var group = FindGroupFor(performer);
            if (group == null)
            {
                group = new PerformerGroup { Performer = performer };
                group.List = CreateActionList(group);
                _groups.Add(group);
            }

            group.Actions.Add(action);
            group.IsPending = false;
            _pendingPerformers.Remove(performer);

            WriteBackFlatArray();
            ClearMessage();
            Repaint();
        }

        // GenericMenu reads '/' as a submenu separator and renders identical labels indistinguishably,
        // so both have to be dealt with before the entry goes in.
        private static string MakeUniqueLabel(string describedLabel, HashSet<string> usedLabels)
        {
            var label = describedLabel.Replace('/', '∕');
            if (usedLabels.Add(label)) return label;

            for (var suffix = 2; ; suffix++)
            {
                var candidate = $"{label} ({suffix})";
                if (usedLabels.Add(candidate)) return candidate;
            }
        }
    }
}
