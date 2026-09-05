using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
    public static class SpriteCombineBaker
    {
        private const int MinTextureSize = 32;
        private const int MaxTextureSize = 16384;

        private const float AxisEpsilon = 0.0005f;
        private const float ScaleEpsilon = 0.0005f;
        private const float MinDeterminant = 1e-8f;

        private const float RecenterEpsilon = 0.0001f;

        public sealed class Report
        {
            public bool Success;
            public bool Cancelled;
            public string Error;
            public readonly List<string> Warnings = new List<string>();
            public int SourceCount;
            public int Width;
            public int Height;
            public float PixelsPerUnit;
            public string AssetPath;
            public Sprite Sprite;
        }

        public static Report Preview(Transform parent, SpriteCombineSettings settings)
        {
            var report = new Report();
            if (!TryPlan(parent, settings, report, out _, out _)) return report;
            report.Success = true;
            return report;
        }

        public static Report Combine(Transform parent, SpriteCombineSettings settings)
        {
            var report = new Report();
            if (parent == null || settings == null)
            {
                report.Error = "No parent assigned.";
                return report;
            }

            if (!TryPlan(parent, settings, report, out var entries, out var layout)) return report;

            if (!TryResolveAssetPath(parent, settings, report, out string assetPath)) return report;

            Color32[] pixels;
            List<CompressionState> uncompressed = null;
            try
            {
                uncompressed = Decompress(entries);
                pixels = Rasterize(entries, layout, report);
            }
            finally
            {
                Restore(uncompressed);
                EditorUtility.ClearProgressBar();
            }

            if (!TryTrimTransparentEdges(pixels, layout, out pixels, out layout))
            {
                report.Error = "The source sprites produced a fully transparent combined image.";
                return report;
            }

            report.Width = layout.Width;
            report.Height = layout.Height;
            report.PixelsPerUnit = layout.PixelsPerUnit;

            if (!TryWriteTexture(assetPath, pixels, layout, settings, out Sprite sprite, out string error))
            {
                report.Error = error;
                return report;
            }

            ApplyToScene(parent, settings, sprite, entries, layout);

            report.Success = true;
            report.AssetPath = assetPath;
            report.Sprite = sprite;
            return report;
        }

        private sealed class Entry
        {
            public SpriteRenderer Renderer;
            public Texture2D Texture;

            public Matrix4x4 SpriteToParent;

            public Color Tint;
            public Rect TexRect;
            public Vector2 PivotPx;
            public Vector2 RectOffsetPx;
            public float SpritePpu;
            public Vector2 LocalMin;
            public Vector2 LocalMax;
            public int HierarchyIndex;
        }

        private readonly struct Layout
        {
            public readonly Vector2 Origin;
            public readonly float PixelsPerUnit;
            public readonly int Width;
            public readonly int Height;

            public Layout(Vector2 origin, float pixelsPerUnit, int width, int height)
            {
                Origin = origin;
                PixelsPerUnit = pixelsPerUnit;
                Width = width;
                Height = height;
            }

            public Vector2 SizeInUnits => new Vector2(Width / PixelsPerUnit, Height / PixelsPerUnit);
        }

        private static bool TryPlan(Transform parent, SpriteCombineSettings settings, Report report,
            out List<Entry> entries, out Layout layout)
        {
            entries = null;
            layout = default;

            if (parent == null)
            {
                report.Error = "No parent assigned.";
                return false;
            }

            entries = Collect(parent, settings, report);
            report.SourceCount = entries.Count;
            if (entries.Count == 0)
            {
                report.Error = $"'{parent.name}' has no sprite children to combine.";
                return false;
            }

            if (!TryComputeLayout(entries, ClampSize(settings), report, out layout)) return false;

            report.Width = layout.Width;
            report.Height = layout.Height;
            report.PixelsPerUnit = layout.PixelsPerUnit;

            Vector3 scale = parent.lossyScale;
            if (Mathf.Abs(scale.x - 1f) > 0.001f || Mathf.Abs(scale.y - 1f) > 0.001f)
            {
                report.Warnings.Add($"Parent world scale is ({scale.x:0.###}, {scale.y:0.###}). The bake is done " +
                                    "in local space, so on-screen texel density is scaled by that.");
            }

            return true;
        }

        private static List<Entry> Collect(Transform parent, SpriteCombineSettings settings, Report report)
        {
            var entries = new List<Entry>();

            SpriteRenderer[] renderers = parent.GetComponentsInChildren<SpriteRenderer>(true);
            Matrix4x4 worldToParent = parent.worldToLocalMatrix;

            var skippedDrawModes = new HashSet<string>();

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                if (renderer.transform == parent) continue;
                if (!settings.IncludeInactiveChildren && !renderer.gameObject.activeInHierarchy) continue;

                Sprite sprite = renderer.sprite;
                if (sprite == null || sprite.texture == null) continue;

                if (renderer.drawMode != SpriteDrawMode.Simple)
                {
                    skippedDrawModes.Add(renderer.name);
                    continue;
                }

                if (!TryGetTextureRect(sprite, out Rect texRect, out Vector2 rectOffset))
                {
                    report.Warnings.Add($"'{renderer.name}' is packed into a tight sprite atlas, which has no " +
                                        "usable texture rect. Skipped.");
                    continue;
                }

                Matrix4x4 spriteToParent = worldToParent * renderer.transform.localToWorldMatrix;
                if (renderer.flipX || renderer.flipY)
                {
                    spriteToParent *= Matrix4x4.Scale(new Vector3(renderer.flipX ? -1f : 1f,
                        renderer.flipY ? -1f : 1f, 1f));
                }

                if (Mathf.Abs(Determinant2D(spriteToParent)) < MinDeterminant)
                {
                    report.Warnings.Add($"'{renderer.name}' has a zero scale and was skipped.");
                    continue;
                }

                float ppu = sprite.pixelsPerUnit;
                Vector2 pivotPx = sprite.pivot;

                entries.Add(new Entry
                {
                    Renderer = renderer,
                    Texture = sprite.texture,
                    SpriteToParent = spriteToParent,
                    Tint = renderer.color,
                    TexRect = texRect,
                    PivotPx = pivotPx,
                    RectOffsetPx = rectOffset,
                    SpritePpu = ppu,
                    LocalMin = (rectOffset - pivotPx) / ppu,
                    LocalMax = (rectOffset + texRect.size - pivotPx) / ppu,
                    HierarchyIndex = i,
                });
            }

            if (skippedDrawModes.Count > 0)
            {
                report.Warnings.Add($"{skippedDrawModes.Count} renderer(s) use Sliced/Tiled draw mode, which this " +
                                    "tool cannot reproduce, and were skipped.");
            }

            SortByDrawOrder(entries, report);
            return entries;
        }

        private static void SortByDrawOrder(List<Entry> entries, Report report)
        {
            var layers = new HashSet<int>();
            foreach (Entry e in entries) layers.Add(e.Renderer.sortingLayerID);

            entries.Sort((a, b) =>
            {
                int layerA = SortingLayer.GetLayerValueFromID(a.Renderer.sortingLayerID);
                int layerB = SortingLayer.GetLayerValueFromID(b.Renderer.sortingLayerID);
                if (layerA != layerB) return layerA.CompareTo(layerB);

                if (a.Renderer.sortingOrder != b.Renderer.sortingOrder)
                    return a.Renderer.sortingOrder.CompareTo(b.Renderer.sortingOrder);

                float zA = a.SpriteToParent.GetColumn(3).z;
                float zB = b.SpriteToParent.GetColumn(3).z;
                if (!Mathf.Approximately(zA, zB)) return zB.CompareTo(zA);

                return a.HierarchyIndex.CompareTo(b.HierarchyIndex);
            });

            if (layers.Count > 1)
            {
                report.Warnings.Add($"Sources span {layers.Count} sorting layers. Anything that used to render " +
                                    "between them will now be entirely in front of or behind the combined sprite.");
            }
        }

        private static bool TryComputeLayout(List<Entry> entries, int maxSize, Report report, out Layout layout)
        {
            layout = default;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            float ppu = 0f;

            foreach (Entry e in entries)
            {
                foreach (Vector2 corner in Corners(e.LocalMin, e.LocalMax))
                {
                    Vector3 p = e.SpriteToParent.MultiplyPoint3x4(corner);
                    min.x = Mathf.Min(min.x, p.x);
                    min.y = Mathf.Min(min.y, p.y);
                    max.x = Mathf.Max(max.x, p.x);
                    max.y = Mathf.Max(max.y, p.y);
                }

                float scale = Mathf.Min(e.SpriteToParent.MultiplyVector(Vector3.right).magnitude,
                    e.SpriteToParent.MultiplyVector(Vector3.up).magnitude);
                if (scale > MinDeterminant) ppu = Mathf.Max(ppu, e.SpritePpu / scale);
            }

            Vector2 size = max - min;
            if (ppu <= 0f || size.x <= 0f || size.y <= 0f)
            {
                report.Error = "The source sprites have no area to bake.";
                return false;
            }

            int width = Mathf.Max(1, Mathf.CeilToInt(size.x * ppu));
            int height = Mathf.Max(1, Mathf.CeilToInt(size.y * ppu));

            if (width > maxSize || height > maxSize)
            {
                float fitted = ppu * (maxSize / (float)Mathf.Max(width, height));
                report.Warnings.Add($"{width}x{height} exceeds the {maxSize} px Max Size, so pixels per unit was " +
                                    $"dropped from {ppu:0.##} to {fitted:0.##}. The bake will be softer than the sources.");
                ppu = fitted;
                width = Mathf.Min(maxSize, Mathf.Max(1, Mathf.CeilToInt(size.x * ppu)));
                height = Mathf.Min(maxSize, Mathf.Max(1, Mathf.CeilToInt(size.y * ppu)));
            }

            layout = new Layout(min, ppu, width, height);
            return true;
        }

        private static readonly float[] LinearLut = BuildLut(true);
        private static readonly float[] ByteLut = BuildLut(false);

        private static float[] BuildLut(bool linear)
        {
            var lut = new float[256];
            for (int i = 0; i < 256; i++) lut[i] = linear ? Mathf.GammaToLinearSpace(i / 255f) : i / 255f;
            return lut;
        }

        private readonly struct CompressionState
        {
            public readonly string Path;
            public readonly TextureImporterCompression Setting;

            public CompressionState(string path, TextureImporterCompression setting)
            {
                Path = path;
                Setting = setting;
            }
        }

        private static List<CompressionState> Decompress(List<Entry> entries)
        {
            var changed = new List<CompressionState>();
            var seen = new HashSet<string>();

            foreach (Entry entry in entries)
            {
                if (entry.Texture == null || !IsCompressed(entry.Texture)) continue;

                string path = AssetDatabase.GetAssetPath(entry.Texture);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null || importer.textureCompression == TextureImporterCompression.Uncompressed)
                    continue;

                EditorUtility.DisplayProgressBar("Combining Sprites",
                    $"Uncompressing {entry.Texture.name}", 0f);

                changed.Add(new CompressionState(path, importer.textureCompression));
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            if (changed.Count > 0)
            {
                foreach (Entry entry in entries)
                {
                    Sprite sprite = entry.Renderer.sprite;
                    if (sprite != null && sprite.texture != null) entry.Texture = sprite.texture;
                }
            }

            return changed;
        }

        private static void Restore(List<CompressionState> changed)
        {
            if (changed == null || changed.Count == 0) return;

            EditorUtility.DisplayProgressBar("Combining Sprites", "Restoring source compression", 1f);

            foreach (CompressionState previous in changed)
            {
                var importer = AssetImporter.GetAtPath(previous.Path) as TextureImporter;
                if (importer == null) continue;

                importer.textureCompression = previous.Setting;
                importer.SaveAndReimport();
            }
        }

        private static Color32[] Rasterize(List<Entry> entries, Layout layout, Report report)
        {
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            var canvas = new Color[layout.Width * layout.Height];
            var cache = new Dictionary<Texture2D, Color32[]>();

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                EditorUtility.DisplayProgressBar("Combining Sprites",
                    $"{e.Renderer.name} ({i + 1}/{entries.Count})", (i + 1) / (float)(entries.Count + 1));

                if (!cache.TryGetValue(e.Texture, out Color32[] source))
                {
                    source = ReadPixels(e.Texture);
                    cache[e.Texture] = source;
                }

                Draw(canvas, source, e, layout, linear);
            }

            EditorUtility.DisplayProgressBar("Combining Sprites", "Encoding", 1f);
            return Encode(canvas, linear);
        }

        private static void Draw(Color[] canvas, Color32[] source, Entry e, Layout layout, bool linear)
        {
            Matrix4x4 spriteToParent = e.SpriteToParent;
            SnapToPixelGrid(ref spriteToParent, e, layout);
            Matrix4x4 parentToSprite = spriteToParent.inverse;

            var minPx = new Vector2(float.MaxValue, float.MaxValue);
            var maxPx = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector2 corner in Corners(e.LocalMin, e.LocalMax))
            {
                Vector3 p = spriteToParent.MultiplyPoint3x4(corner);
                Vector2 px = (new Vector2(p.x, p.y) - layout.Origin) * layout.PixelsPerUnit;
                minPx = Vector2.Min(minPx, px);
                maxPx = Vector2.Max(maxPx, px);
            }

            int x0 = Mathf.Max(0, Mathf.FloorToInt(minPx.x) - 1);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minPx.y) - 1);
            int x1 = Mathf.Min(layout.Width - 1, Mathf.CeilToInt(maxPx.x) + 1);
            int y1 = Mathf.Min(layout.Height - 1, Mathf.CeilToInt(maxPx.y) + 1);
            if (x1 < x0 || y1 < y0) return;

            Color tint = linear ? e.Tint.linear : e.Tint;
            float[] lut = linear ? LinearLut : ByteLut;

            int texWidth = e.Texture.width;
            float texMinX = e.TexRect.x;
            float texMinY = e.TexRect.y;
            float texMaxX = e.TexRect.xMax;
            float texMaxY = e.TexRect.yMax;

            float toTexelX = texMinX + e.PivotPx.x - e.RectOffsetPx.x;
            float toTexelY = texMinY + e.PivotPx.y - e.RectOffsetPx.y;
            float invPpu = 1f / layout.PixelsPerUnit;

            for (int py = y0; py <= y1; py++)
            {
                float localY = layout.Origin.y + (py + 0.5f) * invPpu;
                int row = py * layout.Width;

                for (int px = x0; px <= x1; px++)
                {
                    float localX = layout.Origin.x + (px + 0.5f) * invPpu;
                    Vector3 inSprite = parentToSprite.MultiplyPoint3x4(new Vector3(localX, localY, 0f));

                    int sx = Mathf.FloorToInt(inSprite.x * e.SpritePpu + toTexelX);
                    int sy = Mathf.FloorToInt(inSprite.y * e.SpritePpu + toTexelY);
                    if (sx < texMinX || sx >= texMaxX || sy < texMinY || sy >= texMaxY) continue;

                    Color32 texel = source[sy * texWidth + sx];
                    Color src = new Color(lut[texel.r], lut[texel.g], lut[texel.b], ByteLut[texel.a]) * tint;

                    float srcAlpha = src.a;
                    if (srcAlpha <= 0f) continue;

                    int index = row + px;
                    Color dst = canvas[index];

                    float keep = dst.a * (1f - srcAlpha);
                    float outAlpha = srcAlpha + keep;
                    float inv = 1f / outAlpha;

                    canvas[index] = new Color(
                        (src.r * srcAlpha + dst.r * keep) * inv,
                        (src.g * srcAlpha + dst.g * keep) * inv,
                        (src.b * srcAlpha + dst.b * keep) * inv,
                        outAlpha);
                }
            }
        }

        private static void SnapToPixelGrid(ref Matrix4x4 spriteToParent, Entry e, Layout layout)
        {
            Vector3 xAxis = spriteToParent.MultiplyVector(Vector3.right);
            Vector3 yAxis = spriteToParent.MultiplyVector(Vector3.up);
            if (Mathf.Abs(xAxis.y) > AxisEpsilon || Mathf.Abs(yAxis.x) > AxisEpsilon) return;

            float scaleX = Mathf.Abs(xAxis.x) * layout.PixelsPerUnit / e.SpritePpu;
            float scaleY = Mathf.Abs(yAxis.y) * layout.PixelsPerUnit / e.SpritePpu;
            if (Mathf.Abs(scaleX - 1f) > ScaleEpsilon || Mathf.Abs(scaleY - 1f) > ScaleEpsilon) return;

            Vector3 corner = spriteToParent.MultiplyPoint3x4(e.LocalMin);
            float pixelX = (corner.x - layout.Origin.x) * layout.PixelsPerUnit;
            float pixelY = (corner.y - layout.Origin.y) * layout.PixelsPerUnit;

            spriteToParent = Matrix4x4.Translate(new Vector3(
                (Mathf.Round(pixelX) - pixelX) / layout.PixelsPerUnit,
                (Mathf.Round(pixelY) - pixelY) / layout.PixelsPerUnit,
                0f)) * spriteToParent;
        }

        private static Color32[] Encode(Color[] canvas, bool linear)
        {
            var result = new Color32[canvas.Length];
            for (int i = 0; i < canvas.Length; i++)
            {
                Color c = canvas[i];
                if (c.a <= 0f) continue;

                if (linear)
                {
                    c.r = Mathf.LinearToGammaSpace(c.r);
                    c.g = Mathf.LinearToGammaSpace(c.g);
                    c.b = Mathf.LinearToGammaSpace(c.b);
                }

                result[i] = new Color32(ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(c.a));
            }

            return result;
        }

        private static bool TryTrimTransparentEdges(Color32[] pixels, Layout layout,
            out Color32[] trimmed, out Layout trimmedLayout)
        {
            trimmed = pixels;
            trimmedLayout = layout;

            int minX = layout.Width;
            int minY = layout.Height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < layout.Height; y++)
            {
                int row = y * layout.Width;
                for (int x = 0; x < layout.Width; x++)
                {
                    if (pixels[row + x].a == 0) continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY) return false;

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            if (minX == 0 && minY == 0 && width == layout.Width && height == layout.Height) return true;

            trimmed = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                System.Array.Copy(pixels, (minY + y) * layout.Width + minX, trimmed, y * width, width);
            }

            trimmedLayout = new Layout(
                layout.Origin + new Vector2(minX / layout.PixelsPerUnit, minY / layout.PixelsPerUnit),
                layout.PixelsPerUnit,
                width,
                height);
            return true;
        }

        private static Color32[] ReadPixels(Texture2D texture)
        {
            if (texture.isReadable) return texture.GetPixels32();

            RenderTexture temp = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(texture, temp);
            RenderTexture.active = temp;

            var readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
            readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temp);

            Color32[] pixels = readable.GetPixels32();
            Object.DestroyImmediate(readable);
            return pixels;
        }

        private static bool IsAlreadyDrawing(Transform parent, string assetPath)
        {
            var renderer = parent.GetComponent<SpriteRenderer>();
            return renderer != null && renderer.sprite != null &&
                   AssetDatabase.GetAssetPath(renderer.sprite) == assetPath;
        }

        private static bool TryResolveAssetPath(Transform parent, SpriteCombineSettings settings, Report report,
            out string assetPath)
        {
            assetPath = null;

            string folder = (settings.OutputFolder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!folder.StartsWith("Assets/") && folder != "Assets")
            {
                report.Error =
                    $"Output folder must live inside the project, e.g. {SpriteCombineSettings.DefaultOutputFolder}.";
                return false;
            }

            string name = settings.ResolveFileName(parent.name);
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            if (name.Length == 0)
            {
                report.Error = "Could not derive a file name for the combined image.";
                return false;
            }

            assetPath = $"{folder}/{name}.png";

            if (!IsAlreadyDrawing(parent, assetPath) && File.Exists(ToAbsolute(assetPath)) &&
                !EditorUtility.DisplayDialog("Sprite Combiner",
                    $"{assetPath} already exists and was not created by this combiner.\n\nOverwrite it?",
                    "Overwrite", "Cancel"))
            {
                report.Cancelled = true;
                return false;
            }

            Directory.CreateDirectory(ToAbsolute(folder));
            return true;
        }

        private static bool TryWriteTexture(string assetPath, Color32[] pixels, Layout layout,
            SpriteCombineSettings settings, out Sprite sprite, out string error)
        {
            sprite = null;
            error = null;

            var texture = new Texture2D(layout.Width, layout.Height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);

            File.WriteAllBytes(ToAbsolute(assetPath), png);
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                error = $"Unity did not import {assetPath} as a texture.";
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = settings.Compression;
            importer.filterMode = settings.Filter;
            importer.maxTextureSize = ClampSize(settings);

            var imported = new TextureImporterSettings();
            importer.ReadTextureSettings(imported);
            imported.spritePixelsPerUnit = layout.PixelsPerUnit;

            imported.spriteAlignment = (int)SpriteAlignment.Center;
            imported.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(imported);

            importer.SaveAndReimport();

            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                error = $"{assetPath} imported but produced no sprite.";
                return false;
            }

            return true;
        }

        private static void ApplyToScene(Transform parent, SpriteCombineSettings settings, Sprite sprite,
            List<Entry> entries, Layout layout)
        {
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Combine Sprites");

            GameObject host = parent.gameObject;
            SpriteRenderer target = host.GetComponent<SpriteRenderer>();
            if (target == null) target = Undo.AddComponent<SpriteRenderer>(host);

            Undo.RecordObject(target, "Combine Sprites");
            target.sprite = sprite;
            target.color = Color.white;

            Entry front = entries[entries.Count - 1];
            target.sharedMaterial = front.Renderer.sharedMaterial;
            target.sortingLayerID = front.Renderer.sortingLayerID;
            target.sortingOrder = front.Renderer.sortingOrder;

            CenterPivot(parent, layout);
            if (settings.GenerateCollider) ApplyCollider(host, layout);

            DisposeOfSources(entries, settings);

            Undo.CollapseUndoOperations(group);
        }

        private static void ApplyCollider(GameObject host, Layout layout)
        {
            BoxCollider2D collider = host.GetComponent<BoxCollider2D>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider2D>(host);

            Undo.RecordObject(collider, "Combine Sprites");
            collider.offset = Vector2.zero;
            collider.size = layout.SizeInUnits;
        }

        private static void DisposeOfSources(List<Entry> entries, SpriteCombineSettings settings)
        {
            if (!settings.DeactivateCombinedChildren && !settings.DestroyCombinedChildren) return;

            foreach (Entry entry in entries)
            {
                if (entry.Renderer == null) continue;
                GameObject source = entry.Renderer.gameObject;

                if (settings.DestroyCombinedChildren)
                {
                    Undo.DestroyObjectImmediate(source);
                    continue;
                }

                if (!source.activeSelf) continue;

                Undo.RecordObject(source, "Combine Sprites");
                source.SetActive(false);
            }
        }

        private static Vector2 CenterOffset(Layout layout) => layout.Origin + layout.SizeInUnits * 0.5f;

        private static void CenterPivot(Transform parent, Layout layout)
        {
            Vector2 offset = CenterOffset(layout);
            if (offset.magnitude <= RecenterEpsilon) return;

            var local = new Vector3(offset.x, offset.y, 0f);

            Undo.RecordObject(parent, "Combine Sprites");
            parent.localPosition += parent.localRotation * Vector3.Scale(local, parent.localScale);

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Undo.RecordObject(child, "Combine Sprites");
                child.localPosition -= local;
            }
        }

        private static bool TryGetTextureRect(Sprite sprite, out Rect texRect, out Vector2 offset)
        {
            if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
            {
                texRect = default;
                offset = default;
                return false;
            }

            texRect = sprite.textureRect;
            offset = sprite.textureRectOffset;
            return true;
        }

        private static int ClampSize(SpriteCombineSettings settings) =>
            Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(1, settings.MaxSize)), MinTextureSize, MaxTextureSize);

        private static bool IsCompressed(Texture2D texture)
        {
            switch (texture.format)
            {
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.RGB24:
                case TextureFormat.RGBA64:
                case TextureFormat.RGBAHalf:
                case TextureFormat.RGBAFloat:
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                case TextureFormat.R16:
                case TextureFormat.RG16:
                case TextureFormat.RG32:
                    return false;
                default:
                    return true;
            }
        }

        private static IEnumerable<Vector2> Corners(Vector2 min, Vector2 max)
        {
            yield return min;
            yield return new Vector2(max.x, min.y);
            yield return max;
            yield return new Vector2(min.x, max.y);
        }

        private static float Determinant2D(Matrix4x4 m) => m.m00 * m.m11 - m.m01 * m.m10;

        private static byte ToByte(float value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);

        private static string ToAbsolute(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }
}
