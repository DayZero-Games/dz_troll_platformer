using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DZ.Tools
{
    /// <summary>
    /// Flattens every SpriteRenderer under a parent into a single image and hands the parent one
    /// SpriteRenderer that draws it.
    ///
    /// Everything is baked in the parent's local space, so the parent can still be moved, rotated and
    /// scaled afterwards and the result lines up with the original group. Compositing runs on the CPU
    /// with straight-alpha "over" blending in the project's colour space - rendering the group through a
    /// camera into a RenderTexture instead would square the alpha of soft edges and leave dark halos.
    /// </summary>
    public static class SpriteCombineBaker
    {
        /// <summary>Transparent slack on every side, so pixel-grid snapping can never clip an edge.</summary>
        private const int Margin = 1;

        // What Unity's texture importer will accept, so a hand-edited Max Size cannot produce a broken import.
        private const int MinTextureSize = 32;
        private const int MaxTextureSize = 16384;

        private const float AxisEpsilon = 0.0005f;
        private const float ScaleEpsilon = 0.0005f;
        private const float MinDeterminant = 1e-8f;

        /// <summary>Below this the parent already sits on the centre, so re-centring it is just noise.</summary>
        private const float RecenterEpsilon = 0.0001f;

        public sealed class Report
        {
            public bool Success;
            public string Error;
            public readonly List<string> Warnings = new List<string>();
            public int SourceCount;
            public int Width;
            public int Height;
            public float PixelsPerUnit;
            public string AssetPath;
            public Sprite Sprite;
        }

        // ---------------------------------------------------------------- public API

        /// <summary>Works out what a bake would produce, without touching textures or the scene.</summary>
        public static Report Preview(Transform parent, SpriteCombineSettings settings)
        {
            var report = new Report();
            SpriteCombiner owner = parent != null ? parent.GetComponent<SpriteCombiner>() : null;
            if (!TryPlan(parent, settings, owner, report, out _, out _)) return report;
            report.Success = true;
            return report;
        }

        /// <summary>Bakes the image, imports it as a sprite and puts a renderer for it on the parent.</summary>
        public static Report Combine(SpriteCombiner combiner)
        {
            var report = new Report();
            if (combiner == null)
            {
                report.Error = "No Sprite Combiner given.";
                return report;
            }

            Transform parent = combiner.transform;
            SpriteCombineSettings settings = combiner.Settings;
            if (!TryPlan(parent, settings, combiner, report, out var entries, out var layout)) return report;

            if (!TryResolveAssetPath(combiner, out string assetPath, out string error))
            {
                report.Error = error;
                return report;
            }

            Color32[] pixels;
            try
            {
                pixels = Rasterize(entries, layout, report);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (!TryWriteTexture(assetPath, pixels, layout, settings, out Sprite sprite, out error))
            {
                report.Error = error;
                return report;
            }

            ApplyToScene(combiner, sprite, entries, layout);

            report.Success = true;
            report.AssetPath = assetPath;
            report.Sprite = sprite;
            return report;
        }

        // ---------------------------------------------------------------- planning

        /// <summary>One source sprite, resolved down to the numbers the rasteriser needs.</summary>
        private sealed class Entry
        {
            public SpriteRenderer Renderer;
            public Texture2D Texture;

            /// <summary>Sprite local space -> parent local space, with flipX/flipY folded in.</summary>
            public Matrix4x4 SpriteToParent;

            public Color Tint;
            public Rect TexRect;         // region of Texture holding this sprite's pixels
            public Vector2 PivotPx;      // pivot in pixels, from the sprite rect's bottom left
            public Vector2 RectOffsetPx; // bottom left of TexRect relative to the sprite rect
            public float SpritePpu;
            public Vector2 LocalMin;     // drawn quad in sprite local space
            public Vector2 LocalMax;
            public int HierarchyIndex;
        }

        private readonly struct Layout
        {
            public readonly Vector2 Origin; // parent-local position of the bottom-left corner of pixel (0,0)
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

        private static bool TryPlan(Transform parent, SpriteCombineSettings settings, SpriteCombiner owner,
            Report report, out List<Entry> entries, out Layout layout)
        {
            entries = null;
            layout = default;

            if (parent == null)
            {
                report.Error = "No parent assigned.";
                return false;
            }

            entries = Collect(parent, settings, PreviousSources(owner), report);
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

            WarnIfOutputExists(parent, settings, owner, report);
            return true;
        }

        /// <summary>The objects a previous bake switched off, so they are not mistaken for hand-hidden ones.</summary>
        private static HashSet<GameObject> PreviousSources(SpriteCombiner owner)
        {
            var previous = new HashSet<GameObject>();
            if (owner == null) return previous;

            foreach (GameObject source in owner.BakedSources)
                if (source != null) previous.Add(source);

            return previous;
        }

        private static List<Entry> Collect(Transform parent, SpriteCombineSettings settings,
            HashSet<GameObject> previous, Report report)
        {
            var entries = new List<Entry>();

            // Disabled renderers are always included: switching one off is not a way of hiding a sprite that
            // this tool would ever have produced. GetComponentsInChildren walks depth-first, so the index is
            // hierarchy order.
            SpriteRenderer[] renderers = parent.GetComponentsInChildren<SpriteRenderer>(true);
            Matrix4x4 worldToParent = parent.worldToLocalMatrix;

            var skippedDrawModes = new HashSet<string>();
            var compressed = new HashSet<Texture2D>();

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];

                // The parent's own renderer is the bake target, not a source.
                if (renderer.transform == parent) continue;

                // Anything the last bake switched off is still a source, whatever the setting says - it is
                // only inactive because this tool made it so.
                if (!settings.IncludeInactiveChildren && !renderer.gameObject.activeInHierarchy &&
                    !previous.Contains(renderer.gameObject)) continue;

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
                    // Flipping mirrors around the renderer's own origin, which is exactly a local -1 scale.
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

                if (IsCompressed(sprite.texture)) compressed.Add(sprite.texture);
            }

            if (skippedDrawModes.Count > 0)
            {
                report.Warnings.Add($"{skippedDrawModes.Count} renderer(s) use Sliced/Tiled draw mode, which this " +
                                    "tool cannot reproduce, and were skipped.");
            }

            if (compressed.Count > 0)
            {
                report.Warnings.Add($"{compressed.Count} source texture(s) are compressed. The bake reads them " +
                                    "decompressed, so it inherits their compression artefacts.");
            }

            SortByDrawOrder(entries, report);
            return entries;
        }

        /// <summary>Sorts back-to-front the way Unity's 2D transparent queue would.</summary>
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

                // A 2D camera looks down +Z, so larger Z is further away and draws first.
                float zA = a.SpriteToParent.GetColumn(3).z;
                float zB = b.SpriteToParent.GetColumn(3).z;
                if (!Mathf.Approximately(zA, zB)) return zB.CompareTo(zA);

                // List.Sort is unstable, so fall back to hierarchy order to keep bakes reproducible.
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

                // A sprite scaled down needs more atlas pixels per source texel to stay lossless.
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

            int width = Mathf.CeilToInt(size.x * ppu) + Margin * 2;
            int height = Mathf.CeilToInt(size.y * ppu) + Margin * 2;

            if (width > maxSize || height > maxSize)
            {
                float fitted = ppu * (maxSize / (float)Mathf.Max(width, height));
                report.Warnings.Add($"{width}x{height} exceeds the {maxSize} px Max Size, so pixels per unit was " +
                                    $"dropped from {ppu:0.##} to {fitted:0.##}. The bake will be softer than the sources.");
                ppu = fitted;
                width = Mathf.Min(maxSize, Mathf.CeilToInt(size.x * ppu) + Margin * 2);
                height = Mathf.Min(maxSize, Mathf.CeilToInt(size.y * ppu) + Margin * 2);
            }

            layout = new Layout(new Vector2(min.x - Margin / ppu, min.y - Margin / ppu), ppu, width, height);
            return true;
        }

        // ---------------------------------------------------------------- rasterising

        // Byte -> float, so the per-pixel path never calls GammaToLinearSpace.
        private static readonly float[] LinearLut = BuildLut(true);
        private static readonly float[] ByteLut = BuildLut(false);

        private static float[] BuildLut(bool linear)
        {
            var lut = new float[256];
            for (int i = 0; i < 256; i++) lut[i] = linear ? Mathf.GammaToLinearSpace(i / 255f) : i / 255f;
            return lut;
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

            // Only walk the pixels the sprite can actually reach.
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

            // Sprite local -> texel: texel = local * ppu + pivot - rectOffset + texRect.min
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

                    // Straight-alpha "over". Weighting the destination by its own alpha is what keeps
                    // semi-transparent edges from darkening on an empty canvas.
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

        /// <summary>
        /// An unrotated sprite at 1:1 texel density can be copied without resampling, but only if its
        /// texels line up with whole image pixels. Shift it by up to half a pixel so they do - without
        /// this, pixel art picks up doubled and dropped rows.
        /// </summary>
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
                if (c.a <= 0f) continue; // Color32 defaults to fully transparent black.

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

        /// <summary>
        /// Reads a texture without touching its importer. Non-readable textures go via a GPU copy, which
        /// costs nothing permanent and avoids leaving Read/Write enabled on the source art.
        /// </summary>
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

        // ---------------------------------------------------------------- asset output

        /// <summary>
        /// Works out where the image would be written. Kept apart from TryResolveAssetPath so the preview
        /// can ask the same question without the side effects of committing to a bake.
        /// </summary>
        private static bool TryBuildAssetPath(SpriteCombineSettings settings, string objectName,
            out string assetPath, out string error)
        {
            assetPath = null;
            error = null;

            string folder = (settings.OutputFolder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!folder.StartsWith("Assets/") && folder != "Assets")
            {
                error = $"Output folder must live inside the project, e.g. {SpriteCombineSettings.DefaultOutputFolder}.";
                return false;
            }

            string name = settings.ResolveFileName(objectName);
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            if (name.Length == 0)
            {
                error = "Could not derive a file name for the combined image.";
                return false;
            }

            assetPath = $"{folder}/{name}.png";
            return true;
        }

        /// <summary>True when the file at this path is the one this combiner last wrote.</summary>
        private static bool IsOwnOutput(SpriteCombiner owner, string assetPath) =>
            owner != null && owner.BakedSprite != null &&
            AssetDatabase.GetAssetPath(owner.BakedSprite) == assetPath;

        /// <summary>Says up front that combining will land on top of a file that is already there.</summary>
        private static void WarnIfOutputExists(Transform parent, SpriteCombineSettings settings,
            SpriteCombiner owner, Report report)
        {
            // A bad folder or name is the bake's error to report, not something to warn about here.
            if (!TryBuildAssetPath(settings, parent.name, out string assetPath, out _)) return;
            if (!File.Exists(ToAbsolute(assetPath))) return;

            report.Warnings.Add(IsOwnOutput(owner, assetPath)
                ? $"{assetPath} already exists and will be replaced by this bake."
                : $"{assetPath} already exists and was not made by this combiner. Combining asks before " +
                  "replacing it.");
        }

        private static bool TryResolveAssetPath(SpriteCombiner combiner, out string assetPath, out string error)
        {
            if (!TryBuildAssetPath(combiner.Settings, combiner.name, out assetPath, out error)) return false;

            string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');

            // Re-baking over our own output is the normal case; clobbering unrelated art is not.
            if (!IsOwnOutput(combiner, assetPath) && File.Exists(ToAbsolute(assetPath)) &&
                !EditorUtility.DisplayDialog("Sprite Combiner",
                    $"{assetPath} already exists and was not created by this combiner.\n\nOverwrite it?",
                    "Overwrite", "Cancel"))
            {
                error = "Cancelled - the output file already exists.";
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

            // Only what the bake itself depends on is touched, so anything else set by hand in the texture
            // importer survives a re-bake. Compression stays off whatever the settings say: the image is a
            // lossless composite of its sources, and block compression would throw away exactly the
            // crispness the bake went to the trouble of preserving.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = settings.Filter;
            importer.maxTextureSize = ClampSize(settings);

            var imported = new TextureImporterSettings();
            importer.ReadTextureSettings(imported);
            imported.spritePixelsPerUnit = layout.PixelsPerUnit;

            // A plain centre pivot: ApplyToScene moves the parent's origin onto the centre of the image, so
            // a renderer sitting at the parent still reproduces the original layout.
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

        // ---------------------------------------------------------------- scene wiring

        private static void ApplyToScene(SpriteCombiner combiner, Sprite sprite, List<Entry> entries, Layout layout)
        {
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Combine Sprites");

            GameObject parent = combiner.gameObject;
            SpriteRenderer target = parent.GetComponent<SpriteRenderer>();
            if (target == null) target = Undo.AddComponent<SpriteRenderer>(parent);

            Undo.RecordObject(target, "Combine Sprites");
            target.sprite = sprite;
            target.color = Color.white;

            // The front-most source is what the combined image visually stands in for.
            Entry front = entries[entries.Count - 1];
            target.sharedMaterial = front.Renderer.sharedMaterial;
            target.sortingLayerID = front.Renderer.sortingLayerID;
            target.sortingOrder = front.Renderer.sortingOrder;

            CenterPivot(parent.transform, layout);

            // Leaving the sources on would draw the group twice and save nothing. The whole object goes off,
            // not just its renderer, so its colliders and scripts stop costing anything too.
            var sources = new List<GameObject>(entries.Count);
            foreach (Entry entry in entries)
            {
                GameObject source = entry.Renderer.gameObject;
                sources.Add(source);

                if (!source.activeSelf) continue;
                Undo.RecordObject(source, "Combine Sprites");
                source.SetActive(false);
            }

            Undo.RecordObject(combiner, "Combine Sprites");
            combiner.BakedSprite = sprite;
            combiner.SetBakedSources(sources);
            EditorUtility.SetDirty(combiner);

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Where the centre of the baked image sits in the parent's local space.</summary>
        private static Vector2 CenterOffset(Layout layout) => layout.Origin + layout.SizeInUnits * 0.5f;

        /// <summary>
        /// Moves the parent's origin onto the centre of the baked image and steps every direct child back
        /// by the same amount, so nothing moves on screen. This is what lets the image import with a plain
        /// centre pivot: afterwards it spans exactly half its size either side of the parent's origin.
        /// </summary>
        private static void CenterPivot(Transform parent, Layout layout)
        {
            Vector2 offset = CenterOffset(layout);
            if (offset.magnitude <= RecenterEpsilon) return;

            var local = new Vector3(offset.x, offset.y, 0f);

            Undo.RecordObject(parent, "Combine Sprites");
            parent.localPosition += parent.localRotation * Vector3.Scale(local, parent.localScale);

            // Local axes are untouched, so subtracting the same vector from each child's local position
            // holds them exactly where they were - no world-space round trip to drift through.
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Undo.RecordObject(child, "Combine Sprites");
                child.localPosition -= local;
            }
        }

        // ---------------------------------------------------------------- helpers

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

        /// <summary>
        /// The Max Size setting, folded onto a power of two inside the range Unity's importer accepts. The
        /// planner and the importer both go through this, so the bake can never come out larger than the
        /// importer is willing to keep - Unity would silently halve it, and the sprite would render at half
        /// the size the layout was measured for.
        /// </summary>
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
