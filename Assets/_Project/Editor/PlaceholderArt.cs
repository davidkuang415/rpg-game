using System.IO;
using UnityEditor;
using UnityEngine;

namespace RPG.EditorTools
{
    /// <summary>
    /// Generates the project's placeholder sprites as PNGs.
    ///
    /// Phase 1 drew a 64px square, a 128px circle and a 256px ring. On a modern phone the
    /// player's circle covers roughly 170 screen pixels, so those were being upscaled and
    /// looked soft. Phase 12 regenerates them at four times the size - the files are
    /// overwritten IN PLACE, so every prefab and scene that references them keeps working -
    /// and adds the shapes the polish pass needs: a soft shadow, a floor tile, a rounded
    /// rectangle for the UI, and a crescent for the sword swing.
    ///
    /// Pixels Per Unit always equals the sprite's pixel size (or half, for shapes whose
    /// RADIUS is the unit), so a sprite is exactly one world unit across and every Transform
    /// scale in the project can still be read as "size in metres".
    /// </summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/_Project/Art/Placeholder";

        public const string SquarePath = Folder + "/Square.png";
        public const string CirclePath = Folder + "/Circle.png";
        public const string RingPath = Folder + "/Ring.png";
        public const string ShadowPath = Folder + "/SoftShadow.png";
        public const string FloorTilePath = Folder + "/FloorTile.png";
        public const string RoundedRectPath = Folder + "/RoundedRect.png";
        public const string SlashPath = Folder + "/Slash.png";
        public const string VignettePath = Folder + "/Vignette.png";

        public enum Shape { Square, Circle, Ring, SoftShadow, FloorTile, RoundedRect, Slash, Vignette }

        /// <summary>Regenerates every placeholder sprite at Phase 12 resolution. Idempotent.</summary>
        public static void RegenerateAll()
        {
            Directory.CreateDirectory(Folder);

            Generate(SquarePath, Shape.Square, 256, pixelsPerUnit: 256);
            Generate(CirclePath, Shape.Circle, 512, pixelsPerUnit: 512);
            Generate(RingPath, Shape.Ring, 1024, pixelsPerUnit: 1024);
            Generate(ShadowPath, Shape.SoftShadow, 256, pixelsPerUnit: 256);
            Generate(FloorTilePath, Shape.FloorTile, 256, pixelsPerUnit: 256, fullRect: true);
            Generate(RoundedRectPath, Shape.RoundedRect, 96, pixelsPerUnit: 100, border: 30);
            Generate(SlashPath, Shape.Slash, 512, pixelsPerUnit: 256);   // radius = 1 unit
            Generate(VignettePath, Shape.Vignette, 256, pixelsPerUnit: 256);

            AssetDatabase.SaveAssets();
        }

        public static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        // ------------------------------------------------------------------ generation

        private static void Generate(string path, Shape shape, int size, int pixelsPerUnit,
            int border = 0, bool fullRect = false)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = Alpha(shape, x + 0.5f, y + 0.5f, size);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = fullRect ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = border > 0 ? new Vector4(border, border, border, border) : Vector4.zero;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        /// <summary>Coverage of one pixel centre, 0..1, for each shape.</summary>
        private static float Alpha(Shape shape, float x, float y, int size)
        {
            float half = size * 0.5f;
            float outer = half - 1f;
            var p = new Vector2(x, y);
            var c = new Vector2(half, half);
            float d = Vector2.Distance(p, c);

            switch (shape)
            {
                case Shape.Square:
                    return 1f;

                case Shape.Circle:
                    return Mathf.Clamp01(outer - d);                      // 1px antialiased edge

                case Shape.Ring:
                {
                    float inner = outer * 0.82f;
                    return Mathf.Min(Mathf.Clamp01(outer - d), Mathf.Clamp01(d - inner));
                }

                case Shape.SoftShadow:
                {
                    // Dense in the middle, feathering to nothing at the edge.
                    float t = Mathf.Clamp01(1f - d / outer);
                    return t * t * (3f - 2f * t);
                }

                case Shape.Vignette:
                {
                    // Transparent centre, dark corners. Drawn over the whole screen at low alpha.
                    float t = Mathf.Clamp01(d / half);
                    return Mathf.Pow(t, 2.2f);
                }

                case Shape.FloorTile:
                {
                    // A one-unit tile: a faint grid line on two edges (the neighbouring tile
                    // supplies the other two) and a barely-there checker so the ground has
                    // some grain to read movement against. Alpha does the shading: the camera
                    // background shows through the thin parts and darkens them.
                    float line = 0.55f;
                    if (x < 2f || y < 2f) return line;

                    bool checker = ((int)(x / (size * 0.5f)) + (int)(y / (size * 0.5f))) % 2 == 0;
                    float grain = 0.985f + 0.015f * Mathf.PerlinNoise(x * 0.11f, y * 0.11f);
                    return (checker ? 1f : 0.94f) * grain;
                }

                case Shape.RoundedRect:
                {
                    float radius = size * 0.25f;
                    float dx = Mathf.Max(0f, Mathf.Abs(x - half) - (half - radius));
                    float dy = Mathf.Max(0f, Mathf.Abs(y - half) - (half - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    return Mathf.Clamp01(radius - dist);
                }

                case Shape.Slash:
                {
                    // A crescent: a 120 degree annular sector centred on +X, thick in the
                    // middle and tapering to points at both ends, with a soft inner edge.
                    float angle = Mathf.Atan2(y - half, x - half) * Mathf.Rad2Deg;   // -180..180
                    float halfArc = 60f;
                    float angular = 1f - Mathf.Clamp01(Mathf.Abs(angle) / halfArc);  // 1 at centre, 0 at ends
                    if (angular <= 0f) return 0f;

                    float thickness = Mathf.Lerp(0.06f, 0.34f, Mathf.Sqrt(angular));
                    float innerR = outer * (1f - thickness);
                    float edgeIn = Mathf.Clamp01((d - innerR) / (outer * 0.08f));
                    float edgeOut = Mathf.Clamp01(outer - d);
                    return Mathf.Min(edgeIn, edgeOut) * Mathf.Lerp(0.55f, 1f, angular);
                }

                default:
                    return 1f;
            }
        }
    }
}
