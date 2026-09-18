using UnityEditor;
using UnityEngine;
using RPG.Classes;
using RPG.Core;
using RPG.Core.Combat;
using RPG.Enemies;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 14: swaps the shared placeholder circle for real art on the player's two classes,
    /// the three enemy types, and the boss (which reuses the Brute's), plus a real floor tile
    /// and a real button/panel texture for the hub. Everything is CC0 (Kenney).
    ///
    /// The tint pipeline from Phase 1 stays exactly as it was - EnemyData.BodyTint and
    /// ClassData.BodyTint still exist and still apply - but a character with a BodySprite set
    /// is tinted white, because the sprite now carries its identity in its own colours rather
    /// than in a multiply tint over a white circle.
    ///
    /// Safe to re-run.
    /// </summary>
    public static class Phase14SetupBuilder
    {
        private const string ArtFolder = "Assets/_Project/Art/Kenney";
        private const string ClassDataFolder = "Assets/_Project/Data/Classes";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        // A flash that multiplies a full-color sprite by plain white is invisible - texture *
        // white = texture, no visible change. Values above 1 overexpose instead, which pushes
        // real art toward white the way the old flash did on a white placeholder circle.
        private static readonly Color OverbrightFlash = new Color(2.6f, 2.6f, 2.6f);
        private static readonly Color OverbrightCritFlash = new Color(2.6f, 1.9f, 1f);

        [MenuItem("RPG/Phase 14/Wire In Kenney Art", priority = 270)]
        public static void Setup()
        {
            ConfigureCharacterSprite($"{ArtFolder}/Characters/Player_Knight.png");
            ConfigureCharacterSprite($"{ArtFolder}/Characters/Player_Archer.png");
            ConfigureCharacterSprite($"{ArtFolder}/Characters/Enemy_Grunt.png");
            ConfigureCharacterSprite($"{ArtFolder}/Characters/Enemy_Brute.png");
            ConfigureCharacterSprite($"{ArtFolder}/Characters/Enemy_Slinger.png");
            ConfigureFloorTile($"{ArtFolder}/Environment/FloorTile_Stone.png");
            ConfigureButtonPanel($"{ArtFolder}/UI/ButtonPanel.png");

            WireClass("Knight", "Player_Knight");
            WireClass("Archer", "Player_Archer");

            WireEnemy("MeleeGrunt", "Enemy_Grunt");
            WireEnemy("TankBrute", "Enemy_Brute");
            WireEnemy("RangedSlinger", "Enemy_Slinger");
            WireBossFromBrute();
            BumpPlayerHitFlash();

            AssetDatabase.SaveAssets();
            Verify();
        }

        // ------------------------------------------------------------------ import settings

        private static void ConfigureCharacterSprite(string path) =>
            ConfigureTexture(path, pixelsPerUnit: 16, wrap: TextureWrapMode.Clamp,
                meshType: SpriteMeshType.Tight, border: Vector4.zero);

        private static void ConfigureFloorTile(string path) =>
            ConfigureTexture(path, pixelsPerUnit: 16, wrap: TextureWrapMode.Repeat,
                meshType: SpriteMeshType.FullRect, border: Vector4.zero);

        /// <summary>
        /// Kenney's buttonSquare_beige.png: a 45x49 source with a ~4px bevel on three sides and
        /// a 9px bottom edge (the bevel plus its drop shadow, kept together so 9-slicing never
        /// stretches the shadow). Measured directly from the source pixels, not guessed.
        /// </summary>
        private static void ConfigureButtonPanel(string path) =>
            ConfigureTexture(path, pixelsPerUnit: 100, wrap: TextureWrapMode.Clamp,
                meshType: SpriteMeshType.FullRect, border: new Vector4(4, 9, 4, 4));

        private static void ConfigureTexture(string path, int pixelsPerUnit, TextureWrapMode wrap,
            SpriteMeshType meshType, Vector4 border)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                Debug.LogError($"[Phase 14] Missing art file: {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrap;

            // Small source pixels stretched onto large mobile screens: Point keeps every pixel
            // a crisp square (the intended pixel-art look) instead of Bilinear's soft smear.
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ player classes

        private static void WireClass(string assetName, string spriteName)
        {
            var classData = AssetDatabase.LoadAssetAtPath<ClassData>($"{ClassDataFolder}/{assetName}.asset");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Characters/{spriteName}.png");

            if (classData == null || sprite == null)
            {
                Debug.LogError($"[Phase 14] Could not wire class '{assetName}': asset or sprite missing.");
                return;
            }

            EditorSetupUtility.SetPrivateField(classData, "bodySprite", sprite);
            EditorSetupUtility.SetPrivateField(classData, "bodyTint", Color.white);
        }

        // ------------------------------------------------------------------ enemies

        private static void WireEnemy(string dataName, string spriteName)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDataFolder}/{dataName}.asset");
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Characters/{spriteName}.png");

            if (data == null || sprite == null)
            {
                Debug.LogError($"[Phase 14] Could not wire enemy '{dataName}': asset or sprite missing.");
                return;
            }

            EditorSetupUtility.SetPrivateField(data, "bodySprite", sprite);
            EditorSetupUtility.SetPrivateField(data, "bodyTint", Color.white);

            ApplyToPrefab($"{EnemyPrefabFolder}/Enemy_{dataName}.prefab", sprite);
        }

        /// <summary>
        /// The boss prefab is a standing copy of the Brute's (Phase 12), not an instance built
        /// fresh from TankBrute.asset at runtime, so wiring TankBrute's data does not reach it.
        /// Given straight from the Brute's own sprite to stay exactly what "the boss is the
        /// Brute's, scaled and tinted" already means.
        /// </summary>
        private static void WireBossFromBrute()
        {
            Sprite bruteSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Characters/Enemy_Brute.png");
            if (bruteSprite == null) return;

            var boss = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDataFolder}/BossWarlord.asset");
            if (boss != null)
            {
                EditorSetupUtility.SetPrivateField(boss, "bodySprite", bruteSprite);
                EditorSetupUtility.SetPrivateField(boss, "bodyTint", Color.white);
            }

            ApplyToPrefab($"{EnemyPrefabFolder}/Enemy_BossWarlord.prefab", bruteSprite);
        }

        /// <summary>
        /// The player's body sprite is swapped at runtime by class selection (PlayerStatsBinder),
        /// not baked here - but HitFlash's overbright fix is not runtime-conditional, so it is
        /// applied once, directly on the prefab, same as every enemy.
        /// </summary>
        private static void BumpPlayerHitFlash()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogWarning("[Phase 14] Player.prefab is missing; skipping its HitFlash fix.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            var flash = contents.GetComponent<HitFlash>();
            if (flash != null)
            {
                EditorSetupUtility.SetPrivateField(flash, "flashColor", OverbrightFlash);
                EditorSetupUtility.SetPrivateField(flash, "criticalFlashColor", OverbrightCritFlash);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static void ApplyToPrefab(string prefabPath, Sprite sprite)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"[Phase 14] Missing prefab: {prefabPath}. Run Phase 4 (and Phase 12 " +
                                "for the boss) first.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

            SpriteRenderer body = CharacterBody.FindRenderer(contents);
            if (body != null)
            {
                body.sprite = sprite;
                body.color = Color.white;
            }

            var flash = contents.GetComponent<HitFlash>();
            if (flash != null)
            {
                EditorSetupUtility.SetPrivateField(flash, "flashColor", OverbrightFlash);
                EditorSetupUtility.SetPrivateField(flash, "criticalFlashColor", OverbrightCritFlash);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // ------------------------------------------------------------------ verification

        private static void Verify()
        {
            bool ok = true;

            foreach (string name in new[] { "Knight", "Archer" })
            {
                var classData = AssetDatabase.LoadAssetAtPath<ClassData>($"{ClassDataFolder}/{name}.asset");
                if (classData == null || classData.BodySprite == null)
                {
                    Debug.LogError($"[Phase 14] VERIFY FAILED: {name} has no BodySprite.");
                    ok = false;
                }
            }

            foreach (string name in new[] { "MeleeGrunt", "TankBrute", "RangedSlinger", "BossWarlord" })
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDataFolder}/{name}.asset");
                if (data == null || data.BodySprite == null)
                {
                    Debug.LogError($"[Phase 14] VERIFY FAILED: {name} has no BodySprite.");
                    ok = false;
                }
            }

            if (ok)
            {
                Debug.Log("<b>[Phase 14]</b> Kenney art wired in and verified. Player, every enemy " +
                          "type, the floor and every hub button now use real sprites.");
            }
        }
    }
}
