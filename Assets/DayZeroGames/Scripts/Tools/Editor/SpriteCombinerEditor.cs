using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
    [CustomEditor(typeof(SpriteCombiner))]
    public class SpriteCombinerEditor : UnityEditor.Editor
    {
        private SpriteCombineBaker.Report _preview;

        public override void OnInspectorGUI()
        {
            var combiner = (SpriteCombiner)target;

            SpriteCombinerGui.DrawSettings(combiner.Settings, combiner.name, combiner);

            // Recomputing on Layout only keeps the preview stable for the rest of the frame's events.
            if (Event.current.type == EventType.Layout)
                _preview = SpriteCombineBaker.Preview(combiner.transform, combiner.Settings);

            EditorGUILayout.Space();
            SpriteCombinerGui.DrawPreview(_preview);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_preview == null || !_preview.Success))
            {
                if (GUILayout.Button("Combine", GUILayout.Height(30f)))
                    SpriteCombinerGui.ReportResult(SpriteCombineBaker.Combine(combiner), combiner.transform);
            }
        }
    }

    /// <summary>Shared bits between the inspector and the window.</summary>
    internal static class SpriteCombinerGui
    {
        private static readonly int[] MaxSizes = { 512, 1024, 2048, 4096, 8192 };

        private static readonly GUIContent[] MaxSizeLabels =
        {
            new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"),
            new GUIContent("4096"), new GUIContent("8192"),
        };

        private static readonly GUIContent IncludeInactiveLabel = new GUIContent(
            "Include Inactive Children", "Whether children that are switched off are baked in. Objects a " +
                                         "previous Combine switched off are always baked, so re-baking a " +
                                         "group keeps working either way.");

        private static readonly GUIContent OutputFolderLabel = new GUIContent(
            "Output Folder", "Project folder the combined image is saved to. Must be inside Assets.");

        private static readonly GUIContent FileNameLabel = new GUIContent(
            "File Name", "Without extension. Follows the GameObject's name until you type something else; " +
                         "clear it to go back to following.");

        private static readonly GUIContent FilterLabel = new GUIContent(
            "Filter Mode", "How the combined image is sampled. Point keeps pixel art crisp.");

        private static readonly GUIContent MaxSizeLabel = new GUIContent(
            "Max Size", "Largest the combined image may get. A group too big for it is baked at a lower " +
                        "pixel density rather than being cut off.");

        private static GUIContent _folderIcon;

        private static GUIContent FolderIcon => _folderIcon ?? (_folderIcon = new GUIContent(
            EditorGUIUtility.IconContent("Folder Icon").image, "Pick a folder inside the project."));

        /// <summary>
        /// Draws the bake settings. Pass the object storing them, if there is one, and every edit is
        /// recorded for undo and marked dirty - including the folder picker, which ends the GUI pass early
        /// and so has to see to itself.
        /// </summary>
        public static void DrawSettings(SpriteCombineSettings settings, string objectName, Object owner)
        {
            // Snapshotting every pass is cheap and only turns into an undo entry when a control actually
            // changes something, which keeps the settings undoable without a SerializedProperty for each.
            if (owner != null) Undo.RecordObject(owner, "Sprite Combiner Settings");

            EditorGUI.BeginChangeCheck();

            DrawOutputFolder(settings, owner);
            DrawFileName(settings, objectName);

            settings.Filter = (FilterMode)EditorGUILayout.EnumPopup(FilterLabel, settings.Filter);
            settings.MaxSize = EditorGUILayout.IntPopup(MaxSizeLabel, settings.MaxSize, MaxSizeLabels, MaxSizes);

            settings.IncludeInactiveChildren =
                EditorGUILayout.Toggle(IncludeInactiveLabel, settings.IncludeInactiveChildren);

            if (EditorGUI.EndChangeCheck() && owner != null) EditorUtility.SetDirty(owner);
        }

        private static void DrawOutputFolder(SpriteCombineSettings settings, Object owner)
        {
            bool pick;
            using (new EditorGUILayout.HorizontalScope())
            {
                settings.OutputFolder = EditorGUILayout.TextField(OutputFolderLabel, settings.OutputFolder);
                pick = GUILayout.Button(FolderIcon, EditorStyles.miniButton,
                    GUILayout.Width(26f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            // The row is closed off before the panel opens and the rest of the pass is abandoned - a modal
            // window part way through a layout leaves IMGUI counting controls it never got to draw.
            if (!pick) return;
            PickFolder(settings, owner);
            GUIUtility.ExitGUI();
        }

        private static void PickFolder(SpriteCombineSettings settings, Object owner)
        {
            string picked = EditorUtility.OpenFolderPanel("Combined Image Folder", settings.OutputFolder, "");
            if (string.IsNullOrEmpty(picked)) return;

            string assets = Application.dataPath.Replace('\\', '/');
            picked = picked.Replace('\\', '/');

            string folder = picked == assets ? "Assets"
                : picked.StartsWith(assets + "/") ? "Assets" + picked.Substring(assets.Length)
                : null;

            if (folder == null)
            {
                EditorUtility.DisplayDialog("Sprite Combiner",
                    "The combined image has to be saved inside this project's Assets folder.", "OK");
                return;
            }

            settings.OutputFolder = folder;
            if (owner != null) EditorUtility.SetDirty(owner);

            // The text field holds the old string as its edit buffer until it loses focus.
            GUI.FocusControl(null);
        }

        /// <summary>
        /// Shows the object's name until something else is typed, and stores nothing while it matches - so
        /// the usual case needs no input and a rename carries the image name with it.
        /// </summary>
        private static void DrawFileName(SpriteCombineSettings settings, string objectName)
        {
            string shown = settings.ResolveFileName(objectName);

            EditorGUI.BeginChangeCheck();
            string typed = EditorGUILayout.TextField(FileNameLabel, shown);
            if (!EditorGUI.EndChangeCheck()) return;

            settings.FileName = typed.Trim() == (objectName ?? string.Empty).Trim() ? string.Empty : typed;
        }

        public static void DrawPreview(SpriteCombineBaker.Report preview)
        {
            if (preview == null) return;

            if (!preview.Success)
            {
                EditorGUILayout.HelpBox(preview.Error ?? "Nothing to combine.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"{preview.SourceCount} sprites  ->  {preview.Width} x {preview.Height} px " +
                $"@ {preview.PixelsPerUnit:0.##} PPU\n" +
                $"Saves roughly {Mathf.Max(0, preview.SourceCount - 1)} draw calls.",
                MessageType.None);

            foreach (string warning in preview.Warnings)
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        public static void ReportResult(SpriteCombineBaker.Report result, Transform parent)
        {
            if (!result.Success)
            {
                Debug.LogError($"Sprite Combiner: {result.Error}", parent);
                return;
            }

            foreach (string warning in result.Warnings)
                Debug.LogWarning($"Sprite Combiner: {warning}", parent);

            Debug.Log($"Sprite Combiner: combined {result.SourceCount} sprites into " +
                      $"{result.AssetPath} ({result.Width}x{result.Height}).", result.Sprite);
            EditorGUIUtility.PingObject(result.Sprite);
        }
    }
}
