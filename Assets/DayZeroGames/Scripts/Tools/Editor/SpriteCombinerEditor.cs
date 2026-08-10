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

            if (Event.current.type == EventType.Layout)
                _preview = SpriteCombineBaker.Preview(combiner.transform, combiner.Settings);

            EditorGUILayout.Space();
            SpriteCombinerGui.DrawPreview(_preview);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_preview == null || !_preview.Success))
            {
                if (GUILayout.Button("Combine", GUILayout.Height(30f)))
                {
                    SpriteCombinerGui.ReportResult(
                        SpriteCombineBaker.Combine(combiner.transform, combiner.Settings), combiner.transform);
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Remove Script", GUILayout.Height(30f)))
            {
                Undo.DestroyObjectImmediate(combiner);
                GUIUtility.ExitGUI();
            }
        }
    }

    internal static class SpriteCombinerGui
    {
        private static readonly int[] MaxSizes = { 512, 1024, 2048, 4096, 8192 };

        private static readonly GUIContent[] MaxSizeLabels =
        {
            new GUIContent("512"), new GUIContent("1024"), new GUIContent("2048"),
            new GUIContent("4096"), new GUIContent("8192"),
        };

        private static readonly GUIContent IncludeInactiveLabel = new GUIContent(
            "Include Inactive Children", "Whether children that are switched off are baked in. Combining " +
                                         "switches its sources off, so re-baking a group needs this ticked " +
                                         "or the children turned back on.");

        private static readonly GUIContent DeactivateLabel = new GUIContent(
            "Deactivate Combined Children", "Switch the source objects off once they are baked in. Leaving " +
                                            "them on draws the group twice and saves no draw calls.");

        private static readonly GUIContent DestroyLabel = new GUIContent(
            "Destroy Combined Children", "Delete the source objects once they are baked in. Undo brings " +
                                         "them back; a re-bake cannot.");

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

        private static readonly GUIContent CompressionLabel = new GUIContent(
            "Compression", "Compression for the combined image. None keeps it pixel-exact but costs 4 bytes " +
                           "a pixel in memory - 16 MB at 2048x2048 - which is worth weighing on mobile. The " +
                           "sources are always read uncompressed either way.");

        private static GUIContent _folderIcon;

        private static GUIContent FolderIcon => _folderIcon ?? (_folderIcon = new GUIContent(
            EditorGUIUtility.IconContent("Folder Icon").image, "Pick a folder inside the project."));

        public static void DrawSettings(SpriteCombineSettings settings, string objectName, Object owner)
        {
            if (owner != null) Undo.RecordObject(owner, "Sprite Combiner Settings");

            EditorGUI.BeginChangeCheck();

            DrawOutputFolder(settings, owner);
            DrawFileName(settings, objectName);

            settings.Filter = (FilterMode)EditorGUILayout.EnumPopup(FilterLabel, settings.Filter);
            settings.MaxSize = EditorGUILayout.IntPopup(MaxSizeLabel, settings.MaxSize, MaxSizeLabels, MaxSizes);
            settings.Compression = (SpriteCombineCompression)EditorGUILayout.EnumPopup(
                CompressionLabel, settings.Compression);

            settings.IncludeInactiveChildren =
                EditorGUILayout.Toggle(IncludeInactiveLabel, settings.IncludeInactiveChildren);

            using (new EditorGUI.DisabledScope(settings.DestroyCombinedChildren))
            {
                settings.DeactivateCombinedChildren =
                    EditorGUILayout.Toggle(DeactivateLabel, settings.DeactivateCombinedChildren);
            }

            settings.DestroyCombinedChildren =
                EditorGUILayout.Toggle(DestroyLabel, settings.DestroyCombinedChildren);

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

            GUI.FocusControl(null);
        }

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
                if (!result.Cancelled) Debug.LogError($"Sprite Combiner: {result.Error}", parent);
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
