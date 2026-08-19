using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DZ.Features.EditorTools
{
    /// <summary>
    /// Inspector for a LoopAction's body. Unity ships no type picker for [SerializeReference]
    /// lists, so without this the nested list can only be resized into null entries. Uses the
    /// same reorderable list as a performer's actions - drag to reorder, +/- to add and remove -
    /// and recurses for loops nested inside loops.
    /// </summary>
    [CustomPropertyDrawer(typeof(LoopAction))]
    public class LoopActionDrawer : PropertyDrawer
    {
        private const string ActionsFieldName = "_actions";
        private const string IterationsFieldName = "_iterations";
        private const float ListLeftOffset = 10f;

        // One drawer instance serves every LoopAction on the inspected object, so the lists are
        // cached per property path. A ReorderableList rebuilt each frame cannot be dragged.
        private readonly Dictionary<string, ReorderableList> _listsByPath = new();

        private static float LineHeight => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return LineHeight;

            var actions = property.FindPropertyRelative(ActionsFieldName);
            var height = LineHeight + Spacing            // foldout
                       + LineHeight + Spacing;           // iterations

            if (actions != null)
                height += GetActionList(actions).GetHeight() + Spacing;

            return height + ObstacleActionListUI.ElementPadding;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var row = new Rect(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            var iterations = property.FindPropertyRelative(IterationsFieldName);
            var actions = property.FindPropertyRelative(ActionsFieldName);

            row.y += LineHeight + Spacing;
            if (iterations != null)
                EditorGUI.PropertyField(row, iterations, new GUIContent("Iterations", iterations.tooltip));

            if (actions == null)
            {
                EditorGUI.EndProperty();
                return;
            }

            row.y += LineHeight + Spacing;
            var list = GetActionList(actions);
            list.DoList(new Rect(
                row.x + ListLeftOffset,
                row.y,
                row.width - ListLeftOffset,
                list.GetHeight()));

            EditorGUI.EndProperty();
        }

        private ReorderableList GetActionList(SerializedProperty actionsProperty)
        {
            var propertyPath = actionsProperty.propertyPath;

            if (_listsByPath.TryGetValue(propertyPath, out var list) &&
                list.serializedProperty != null &&
                list.serializedProperty.serializedObject == actionsProperty.serializedObject)
            {
                // Refresh the handle: reordering the owning array leaves the cached
                // SerializedProperty pointing at stale data even though the path still matches.
                list.serializedProperty = actionsProperty;
                return list;
            }

            list = ObstacleActionListUI.CreateActionList(actionsProperty, "Actions");
            _listsByPath[propertyPath] = list;
            return list;
        }
    }
}
