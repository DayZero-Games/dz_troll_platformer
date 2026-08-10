using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
    /// <summary>
    /// The same bake as the SpriteCombiner inspector, driven from a window so a group can be combined by
    /// dropping in the parent - the component is only added when Combine is actually pressed.
    /// </summary>
    public class SpriteCombinerWindow : EditorWindow
    {
        private Transform _parent;
        private readonly SpriteCombineSettings _settings = new SpriteCombineSettings();
        private SpriteCombineBaker.Report _preview;

        [MenuItem("Tools/DayZero/Sprite Combiner")]
        public static void Open() => Open(null);

        [MenuItem("GameObject/DayZero/Combine Child Sprites", false, 30)]
        private static void OpenFromHierarchy(MenuCommand command)
        {
            if (command.context is GameObject target) Open(target.transform);
        }

        [MenuItem("GameObject/DayZero/Combine Child Sprites", true)]
        private static bool ValidateOpenFromHierarchy(MenuCommand command) => command.context is GameObject;

        private static void Open(Transform parent)
        {
            var window = GetWindow<SpriteCombinerWindow>("Sprite Combiner");
            window.minSize = new Vector2(360f, 260f);
            if (parent != null) window.SetParent(parent);
            window.Show();
        }

        /// <summary>Adopts whatever the object is already set up with, so the window never contradicts the
        /// inspector of the same object.</summary>
        private void SetParent(Transform parent)
        {
            _parent = parent;

            var existing = parent != null ? parent.GetComponent<SpriteCombiner>() : null;
            _settings.CopyFrom(existing != null ? existing.Settings : new SpriteCombineSettings());
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            var picked = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Parent", "The GameObject whose children hold the sprites to combine."),
                _parent, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) SetParent(picked);

            // No owner: these settings are the window's own until Combine copies them onto the component.
            SpriteCombinerGui.DrawSettings(_settings, _parent != null ? _parent.name : string.Empty, null);

            if (Event.current.type == EventType.Layout)
                _preview = _parent != null ? SpriteCombineBaker.Preview(_parent, _settings) : null;

            EditorGUILayout.Space();
            if (_parent == null) EditorGUILayout.HelpBox("Assign the parent GameObject to combine.", MessageType.Info);
            else SpriteCombinerGui.DrawPreview(_preview);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_preview == null || !_preview.Success))
            {
                if (GUILayout.Button("Combine", GUILayout.Height(30f))) Combine();
            }
        }

        private void Combine()
        {
            // The bake stores its output on the component, so the window and the inspector stay in step.
            var combiner = _parent.GetComponent<SpriteCombiner>();
            if (combiner == null) combiner = Undo.AddComponent<SpriteCombiner>(_parent.gameObject);

            Undo.RecordObject(combiner, "Combine Sprites");
            combiner.Settings.CopyFrom(_settings);

            SpriteCombinerGui.ReportResult(SpriteCombineBaker.Combine(combiner), _parent);
        }
    }
}
