using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
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

        private void SetParent(Transform parent)
        {
            _parent = parent;

            var existing = parent != null ? parent.GetComponent<SpriteCombiner>() : null;
            if (existing != null) _settings.CopyFrom(existing.Settings);
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            var picked = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Parent", "The GameObject whose children hold the sprites to combine."),
                _parent, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck()) SetParent(picked);

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
            var existing = _parent.GetComponent<SpriteCombiner>();
            if (existing != null)
            {
                Undo.RecordObject(existing, "Combine Sprites");
                existing.Settings.CopyFrom(_settings);
                EditorUtility.SetDirty(existing);
            }

            SpriteCombinerGui.ReportResult(SpriteCombineBaker.Combine(_parent, _settings), _parent);
        }
    }
}
