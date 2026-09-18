using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.CameraSystem;
using RPG.Core;
using RPG.Core.Combat;
using RPG.Core.Events;
using RPG.Economy;
using RPG.Enemies;
using RPG.Inventory;
using RPG.Items;
using RPG.Player;
using RPG.Player.Combat;
using RPG.Stages;
using RPG.UI;
using RPG.Vfx;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 12 setup: the polish pass.
    ///
    ///   1. A 144 fps cap, and the standalone player set up for a native-resolution window.
    ///   2. Placeholder art regenerated at four times the resolution, plus new shapes.
    ///   3. Gold gets a job: ItemEconomy - upgrade gear with gold, sell gear for gold (and
    ///      gems, for Legendary and up). The bag and gear pages grow action buttons.
    ///   4. Characters are restructured so their sprite lives on a "Body" child, with an
    ///      outline and a soft shadow, and CharacterAnimator brings them to life.
    ///   5. Ten stages, rebuilt with tiled floors and edged walls. Stage 10 is the Warlord.
    ///   6. Camera shake, a visible sword swing, rounded UI, text shadows, a vignette.
    ///
    /// Safe to re-run. It calls the Phase 11 tool itself (the hub is rebuilt from scratch
    /// there, and its pages now need the economy wiring), then applies everything above.
    /// </summary>
    public static class Phase12SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ItemDataFolder = "Assets/_Project/Data/Items";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string StageDataFolder = "Assets/_Project/Data/Stages";
        private const string LootDataFolder = "Assets/_Project/Data/Loot";
        private const string LegacyStage03Prefab = StageKit.StagePrefabFolder + "/Stage_03.prefab";

        private const int TargetFrameRate = 144;

        private static readonly Color OutlineColor = new Color(0.04f, 0.04f, 0.06f, 0.95f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.42f);

        [MenuItem("RPG/Phase 12/Build Polish, Economy And Stages", priority = 240)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            // --- 1. art and project settings --------------------------------------
            PlaceholderArt.RegenerateAll();
            ApplyProjectSettings();

            GameObject systems = GameObject.Find("GameSystems");
            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("HUD");

            if (systems == null || player == null || hud == null)
            {
                Debug.LogError("[Phase 12] Missing GameSystems, Player or HUD. Run the earlier phase tools first.");
                return;
            }

            // --- 2. frame cap and economy (before Phase 11, which wires the hub to them) -
            EditorSetupUtility.EnsureComponent<FrameRateLimiter>(systems);
            ItemEconomyConfig economyConfig = CreateEconomyConfig();
            InstallItemEconomy(systems, player, economyConfig);

            // --- 3. the hub, rebuilt with the new action buttons -----------------------
            // Saved first so the Phase 11 tool's own "save changes?" prompt has nothing to ask.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Phase11SetupBuilder.Setup();

            // The Phase 11 tool saved the scene; keep working on the same objects.
            systems = GameObject.Find("GameSystems");
            player = GameObject.Find("Player");
            hud = GameObject.Find("HUD");

            // --- 4. characters ------------------------------------------------------
            EnemyData boss = CreateBossData();
            BuildBossPrefab(boss);
            RestructureEnemyPrefabs();
            RestructureScenePlayer(player);

            // --- 5. stages ----------------------------------------------------------
            BuildStages(boss);

            // --- 6. camera and UI ---------------------------------------------------
            InstallCameraShake();
            RestyleHud(hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Verify(player);

            Debug.Log("<b>[Phase 12]</b> Polish, economy and stages installed. Press Play: ten " +
                      "stages in the list, UPGRADE and SELL on the bag page, and everything " +
                      $"moves. Frame rate is capped at {TargetFrameRate}.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 12] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ================================================================== project settings

        /// <summary>
        /// Resolution and frame pacing.
        ///
        /// The game is portrait (the canvas is designed at 1080x1920). The desktop player
        /// gets a borderless full-screen window, which is the display's own resolution with
        /// no mode switch, and a 1080x1920 window when not full screen. VSync is disabled on
        /// every quality level because, while it is on, Unity ignores the frame-rate cap.
        /// </summary>
        private static void ApplyProjectSettings()
        {
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;

            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 0;
            }
            QualitySettings.SetQualityLevel(current, false);

            Debug.Log("[Phase 12] Player settings: 1080x1920 portrait, borderless full screen, " +
                      "VSync off on every quality level.");
        }

        // ================================================================== economy

        private static ItemEconomyConfig CreateEconomyConfig()
        {
            ItemEconomyConfig config = EditorSetupUtility.CreateOrLoadAsset<ItemEconomyConfig>(
                ItemDataFolder + "/ItemEconomyConfig.asset", out _);

            // Defaults live in the class; nothing to write. Tune the asset in the Inspector.
            return config;
        }

        private static void InstallItemEconomy(GameObject systems, GameObject player, ItemEconomyConfig config)
        {
            var economy = EditorSetupUtility.EnsureComponent<ItemEconomy>(systems);

            EditorSetupUtility.SetPrivateField(economy, "config", config);
            EditorSetupUtility.SetPrivateField(economy, "itemRegistry",
                AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemDataFolder + "/ItemRegistry.asset"));
            EditorSetupUtility.SetPrivateField(economy, "rarityTable",
                AssetDatabase.LoadAssetAtPath<RarityTable>(ItemDataFolder + "/RarityTable.asset"));
            EditorSetupUtility.SetPrivateField(economy, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(economy, "inventory", systems.GetComponent<InventoryManager>());
            EditorSetupUtility.SetPrivateField(economy, "equipment", player.GetComponent<EquipmentManager>());

            // The equipment manager needs the same config so an upgraded item's stats are
            // computed with its multiplier, not as +0.
            var equipment = player.GetComponent<EquipmentManager>();
            if (equipment != null) EditorSetupUtility.SetPrivateField(equipment, "economyConfig", config);
        }

        // ================================================================== characters

        /// <summary>
        /// Moves a character's sprite from its root onto a "Body" child and adds an outline,
        /// a soft shadow and a CharacterAnimator. Components that used the root renderer are
        /// re-pointed at the body. Idempotent: a character already restructured is only
        /// re-wired.
        /// </summary>
        private static void RestructureCharacter(GameObject root)
        {
            if (root == null || root.GetComponent<Health>() == null) return;

            Sprite circle = PlaceholderArt.Load(PlaceholderArt.CirclePath);
            Sprite shadowSprite = PlaceholderArt.Load(PlaceholderArt.ShadowPath);

            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
            Transform bodyTransform = root.transform.Find(CharacterBody.BodyName);
            SpriteRenderer body;

            if (bodyTransform != null)
            {
                body = bodyTransform.GetComponent<SpriteRenderer>();
            }
            else
            {
                var bodyObject = new GameObject(CharacterBody.BodyName);
                bodyObject.transform.SetParent(root.transform, false);
                bodyObject.transform.SetAsFirstSibling();
                bodyTransform = bodyObject.transform;

                body = bodyObject.AddComponent<SpriteRenderer>();
                if (rootRenderer != null)
                {
                    body.sprite = rootRenderer.sprite;
                    body.color = rootRenderer.color;
                    body.sortingLayerID = rootRenderer.sortingLayerID;
                    body.sortingOrder = rootRenderer.sortingOrder;
                    body.flipX = rootRenderer.flipX;
                    body.flipY = rootRenderer.flipY;
                }
                else
                {
                    body.sprite = circle;
                    body.sortingOrder = 1;
                }
            }

            // The root's own renderer goes away; the collider on the root stays exactly as it was.
            if (rootRenderer != null) Object.DestroyImmediate(rootRenderer, true);

            int bodyOrder = body.sortingOrder;

            Transform outline = EnsureChildSprite(root.transform, "Outline", circle, OutlineColor,
                bodyOrder - 1, Vector3.zero, Vector3.one * 1.14f);
            Transform shadow = EnsureChildSprite(root.transform, "Shadow", shadowSprite, ShadowColor,
                bodyOrder - 2, new Vector3(0f, -0.4f, 0f), new Vector3(1.15f, 0.5f, 1f));

            // Re-point everything that used to read the root renderer.
            var flash = root.GetComponent<HitFlash>();
            if (flash != null) EditorSetupUtility.SetPrivateField(flash, "bodyRenderer", body);

            var binder = root.GetComponent<PlayerStatsBinder>();
            if (binder != null) EditorSetupUtility.SetPrivateField(binder, "bodyRenderer", body);

            var enemyAttack = root.GetComponent<EnemyAttackBase>();
            if (enemyAttack != null) EditorSetupUtility.SetPrivateField(enemyAttack, "telegraphRenderer", body);

            var animator = EditorSetupUtility.EnsureComponent<CharacterAnimator>(root);
            EditorSetupUtility.SetPrivateField(animator, "body", bodyTransform);
            EditorSetupUtility.SetPrivateField(animator, "outline", outline);
            EditorSetupUtility.SetPrivateField(animator, "shadow", shadow);
        }

        private static Transform EnsureChildSprite(Transform parent, string name, Sprite sprite, Color color,
            int sortingOrder, Vector3 localPosition, Vector3 localScale)
        {
            Transform existing = EditorSetupUtility.FindChild(parent, name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;

            // Placed right after the body so the hierarchy reads Body, Outline, Shadow.
            Transform body = parent.Find(CharacterBody.BodyName);
            if (body != null) child.transform.SetSiblingIndex(body.GetSiblingIndex() + 1);

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return child.transform;
        }

        private static void RestructureEnemyPrefabs()
        {
            if (!Directory.Exists(EnemyPrefabFolder)) return;

            string[] files = Directory.GetFiles(EnemyPrefabFolder, "*.prefab");
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                RestructureCharacter(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log($"[Phase 12] Restructured {files.Length} enemy prefab(s): Body, Outline, Shadow, CharacterAnimator.");
        }

        /// <summary>
        /// The player is handled on the SCENE object, for the reason the Phase 11 tool spells
        /// out: Player.prefab is stale, and the scene instance is the only complete player.
        /// Removing the prefab's root renderer here becomes a "removed component" override.
        /// </summary>
        private static void RestructureScenePlayer(GameObject player)
        {
            RestructureCharacter(player);
            InstallSwingArc(player);
        }

        private static void InstallSwingArc(GameObject player)
        {
            var knight = player.GetComponent<KnightSwordAttack>();
            if (knight == null) return;

            Transform existing = EditorSetupUtility.FindChild(player.transform, "SlashArc");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var arcObject = new GameObject("SlashArc");
            arcObject.transform.SetParent(player.transform, false);

            var renderer = arcObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderArt.Load(PlaceholderArt.SlashPath);
            renderer.color = new Color(1f, 0.96f, 0.8f, 0.9f);
            renderer.sortingOrder = 60;   // Above characters, below hit sparks (80).
            renderer.enabled = false;

            var visual = EditorSetupUtility.EnsureComponent<SwingArcVisual>(player);
            EditorSetupUtility.SetPrivateField(visual, "arc", renderer);
            EditorSetupUtility.SetPrivateField(visual, "spriteReach", 1f);
        }

        // ================================================================== boss

        private static EnemyData CreateBossData()
        {
            EnemyData boss = EditorSetupUtility.CreateOrLoadAsset<EnemyData>(
                EnemyDataFolder + "/BossWarlord.asset", out bool created);

            // Written only on creation, so numbers tuned by hand survive a re-run.
            if (!created) return boss;

            EditorSetupUtility.SetPrivateField(boss, "enemyId", "boss_warlord");
            EditorSetupUtility.SetPrivateField(boss, "displayName", "Warlord");
            EditorSetupUtility.SetPrivateField(boss, "archetype", EnemyArchetype.Tank);
            EditorSetupUtility.SetPrivateField(boss, "maxHealth", 700f);
            EditorSetupUtility.SetPrivateField(boss, "attack", 20f);
            EditorSetupUtility.SetPrivateField(boss, "defense", 30f);
            EditorSetupUtility.SetPrivateField(boss, "moveSpeed", 2.1f);
            EditorSetupUtility.SetPrivateField(boss, "attackSpeed", 0.55f);
            EditorSetupUtility.SetPrivateField(boss, "attackRange", 2.2f);
            EditorSetupUtility.SetPrivateField(boss, "detectionRange", 18f);
            EditorSetupUtility.SetPrivateField(boss, "memorySeconds", 6f);
            EditorSetupUtility.SetPrivateField(boss, "attackWindupSeconds", 0.55f);
            EditorSetupUtility.SetPrivateField(boss, "lootTable",
                AssetDatabase.LoadAssetAtPath<RPG.Loot.LootTableData>(LootDataFolder + "/LootTable_Boss.asset"));
            EditorSetupUtility.SetPrivateField(boss, "xpReward", 300f);
            EditorSetupUtility.SetPrivateField(boss, "goldReward", 150);
            EditorSetupUtility.SetPrivateField(boss, "bodyTint", new Color(0.95f, 0.32f, 0.5f));
            EditorSetupUtility.SetPrivateField(boss, "bodyScale", 1.9f);
            EditorUtility.SetDirty(boss);

            return boss;
        }

        /// <summary>
        /// The boss prefab is the Brute's, copied and scaled up. Same components, same
        /// behaviour; the numbers on BossWarlord.asset are what make it a boss.
        /// </summary>
        private static void BuildBossPrefab(EnemyData boss)
        {
            string source = EnemyPrefabFolder + "/Enemy_TankBrute.prefab";
            string path = EnemyPrefabFolder + "/Enemy_BossWarlord.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(source) == null)
            {
                Debug.LogError("[Phase 12] Enemy_TankBrute.prefab is missing; cannot build the boss. Run Phase 4 first.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                AssetDatabase.CopyAsset(source, path);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            contents.name = "Enemy_BossWarlord";
            contents.transform.localScale = Vector3.one * boss.BodyScale;

            var stats = contents.GetComponent<EnemyStats>();
            if (stats != null) EditorSetupUtility.SetPrivateField(stats, "data", boss);

            SpriteRenderer body = CharacterBody.FindRenderer(contents);
            if (body != null) body.color = boss.BodyTint;

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);

            EditorSetupUtility.SetPrivateField(boss, "enemyPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        // ================================================================== stages

        private static void BuildStages(EnemyData boss)
        {
            StageKit.Init();

            var enemies = new Phase12StageLayouts.Enemies
            {
                Grunt = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/MeleeGrunt.asset"),
                Slinger = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/RangedSlinger.asset"),
                Brute = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/TankBrute.asset"),
                Boss = boss
            };

            if (enemies.Grunt == null || enemies.Slinger == null || enemies.Brute == null)
            {
                Debug.LogError("[Phase 12] Enemy data assets are missing. Run the Phase 4 tool first.");
                return;
            }

            // The empty scaffold Stage_03.prefab from the Level Tools is replaced by a real layout.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyStage03Prefab) != null)
            {
                AssetDatabase.DeleteAsset(LegacyStage03Prefab);
            }

            //                 asset      number  name              prefab                             level  loot  gold
            StageData[] stages =
            {
                Stage("Stage01", 1, "Stage 1", Phase12StageLayouts.Stage01(enemies), 1, 1f, 1f),
                Stage("Stage02", 2, "Stage 2", Phase12StageLayouts.Stage02(enemies), 3, 1f, 1f),
                Stage("Stage03", 3, "Stage 3", Phase12StageLayouts.Stage03(enemies), 4, 1f, 1f),
                Stage("Stage04", 4, "Stage 4", Phase12StageLayouts.Stage04(enemies), 5, 1f, 1f),
                Stage("Stage05", 5, "Stage 5", Phase12StageLayouts.Stage05(enemies), 6, 1.05f, 1.1f),
                Stage("Stage06", 6, "Stage 6", Phase12StageLayouts.Stage06(enemies), 7, 1.05f, 1.1f),
                Stage("Stage07", 7, "Stage 7", Phase12StageLayouts.Stage07(enemies), 8, 1.1f, 1.2f),
                Stage("Stage08", 8, "Stage 8", Phase12StageLayouts.Stage08(enemies), 9, 1.1f, 1.2f),
                Stage("Stage09", 9, "Stage 9", Phase12StageLayouts.Stage09(enemies), 10, 1.15f, 1.3f),
                Stage("Stage10", 10, "Stage 10", Phase12StageLayouts.Stage10(enemies), 11, 1.5f, 1.6f)
            };

            var registry = EditorSetupUtility.CreateOrLoadAsset<StageRegistry>(
                StageDataFolder + "/StageRegistry.asset", out _);
            EditorSetupUtility.SetPrivateObjectList(registry, "stages", stages);

            Debug.Log($"[Phase 12] Built {stages.Length} stages. Stage 10 is the Warlord's arena.");
        }

        private static StageData Stage(string assetName, int number, string displayName, GameObject prefab,
            int enemyLevel, float lootModifier, float goldModifier)
        {
            StageData stage = EditorSetupUtility.CreateOrLoadAsset<StageData>(
                $"{StageDataFolder}/{assetName}.asset", out _);

            EditorSetupUtility.SetPrivateField(stage, "stageNumber", number);
            EditorSetupUtility.SetPrivateField(stage, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(stage, "stagePrefab", prefab);
            EditorSetupUtility.SetPrivateField(stage, "enemyLevel", enemyLevel);
            EditorSetupUtility.SetPrivateField(stage, "lootModifier", lootModifier);
            EditorSetupUtility.SetPrivateField(stage, "goldModifier", goldModifier);
            return stage;
        }

        // ================================================================== camera and UI

        private static void InstallCameraShake()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            var shake = EditorSetupUtility.EnsureComponent<CameraShake>(mainCamera.gameObject);
            EditorSetupUtility.SetPrivateField(shake, "playerReference",
                AssetDatabase.LoadAssetAtPath<PlayerReference>("Assets/_Project/Data/Core/PlayerReference.asset"));
            EditorSetupUtility.SetPrivateField(shake, "enemyEvents",
                AssetDatabase.LoadAssetAtPath<EnemyEventChannel>(EnemyDataFolder + "/EnemyEventChannel.asset"));

            // A touch of blue in the void outside the arena, so the world has a colour rather than
            // sitting on black.
            mainCamera.backgroundColor = new Color(0.05f, 0.055f, 0.08f);
        }

        private const string KenneyButtonPanelPath = "Assets/_Project/Art/Kenney/UI/ButtonPanel.png";

        /// <summary>
        /// Rounded corners on every button, a press animation, readable text, and a vignette.
        ///
        /// Only Images that draw nothing but a flat colour (no sprite) and belong to a Button
        /// are rounded, so full-screen panel backgrounds and the joystick keep their shape.
        /// Templates are restyled too, and the pages instantiate from those, so every item
        /// tile and stage button inherits the look.
        /// </summary>
        private static void RestyleHud(GameObject hud)
        {
            Sprite placeholderRounded = PlaceholderArt.Load(PlaceholderArt.RoundedRectPath);
            Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(KenneyButtonPanelPath) ?? placeholderRounded;

            Button[] buttons = hud.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var image = buttons[i].GetComponent<Image>();

                // Bare (never styled) or already carrying either the old procedural mask or this
                // sprite from an earlier run - so re-running always leaves a button on `rounded`,
                // whichever asset that currently resolves to, instead of getting stuck on the
                // first one a button happened to receive.
                bool unstyled = image != null &&
                    (image.sprite == null || image.sprite == rounded || image.sprite == placeholderRounded);

                if (unstyled)
                {
                    // Lighten toward white only the first time a button leaves the flat
                    // placeholder mask for real art - not on every re-run, and not on a button
                    // that was already showing the Kenney art last time this ran.
                    if (image.sprite == null || image.sprite == placeholderRounded)
                    {
                        // The old RoundedRect was a plain white mask, so a button's role colour
                        // (e.g. the dark red on SELL) WAS the button. The Kenney art is real,
                        // already-lit art, and multiplying it by a dark role colour just crushes
                        // it back to a flat blob - so it is washed toward white, not replaced.
                        image.color = Color.Lerp(image.color, Color.white, 0.6f);
                    }

                    image.sprite = rounded;
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 1f;
                }

                ColorBlock colors = buttons[i].colors;
                colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
                colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
                colors.fadeDuration = 0.06f;
                buttons[i].colors = colors;

                EditorSetupUtility.EnsureComponent<ButtonPressFeedback>(buttons[i].gameObject);
            }

            Text[] texts = hud.GetComponentsInChildren<Text>(true);
            int shadowed = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].fontSize < 26) continue;

                var shadow = EditorSetupUtility.EnsureComponent<Shadow>(texts[i].gameObject);
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = new Vector2(2f, -2f);
                shadow.useGraphicAlpha = true;
                shadowed++;
            }

            BuildVignette(hud);

            Debug.Log($"[Phase 12] Restyled {buttons.Length} buttons and {shadowed} labels.");
        }

        private static void BuildVignette(GameObject hud)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "Vignette");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Image vignette = EditorSetupUtility.CreateUiImage("Vignette", hud.transform,
                PlaceholderArt.Load(PlaceholderArt.VignettePath), new Color(0f, 0f, 0f, 0.38f));
            vignette.raycastTarget = false;
            EditorSetupUtility.StretchFull((RectTransform)vignette.transform);

            // First child: under everything, including the joystick's transparent touch area.
            vignette.transform.SetAsFirstSibling();
        }

        // ================================================================== verify

        private static void Verify(GameObject player)
        {
            bool ok = true;

            if (CharacterBody.FindAnimatable(player) == null)
            {
                Debug.LogError("[Phase 12] VERIFY FAILED: the Player has no Body child.", player);
                ok = false;
            }

            if (player.GetComponent<CharacterAnimator>() == null)
            {
                Debug.LogError("[Phase 12] VERIFY FAILED: the Player has no CharacterAnimator.", player);
                ok = false;
            }

            var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(StageDataFolder + "/StageRegistry.asset");
            if (registry == null || registry.Count < 10)
            {
                Debug.LogError($"[Phase 12] VERIFY FAILED: expected 10 stages in the registry, found {registry?.Count ?? 0}.");
                ok = false;
            }

            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null || systems.GetComponent<ItemEconomy>() == null ||
                systems.GetComponent<FrameRateLimiter>() == null)
            {
                Debug.LogError("[Phase 12] VERIFY FAILED: GameSystems is missing ItemEconomy or FrameRateLimiter.");
                ok = false;
            }

            if (ok) Debug.Log("<b>[Phase 12]</b> Verified: body children, animator, 10 stages, economy, frame cap.");
        }
    }
}
