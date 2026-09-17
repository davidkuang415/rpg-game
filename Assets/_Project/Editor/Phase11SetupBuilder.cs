using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.Core.Combat;
using RPG.DebugTools;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Player;
using RPG.Progression;
using RPG.Save;
using RPG.Stages;
using RPG.UI;
using RPG.UI.HUD;
using RPG.Vfx;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 11 setup: moves everything that is not fighting out of the play area, and makes
    /// hits visible.
    ///
    /// Three separate jobs, done together because they all change what is on screen during a
    /// fight:
    ///   1. A hub screen - stage list, gear and bag, one swipe apart - replaces the STATS
    ///      overlay and the BAG button that used to sit over the arena.
    ///   2. Health bars under the player and every enemy.
    ///   3. Hit sparks and damage numbers, so a landed swing or arrow is unmistakable.
    ///
    /// Safe to re-run: the hub and the effect prefabs are rebuilt from scratch each time.
    /// </summary>
    public static class Phase11SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string CoreDataFolder = "Assets/_Project/Data/Core";
        private const string UiDataFolder = "Assets/_Project/Data/UI";
        private const string VfxPrefabFolder = "Assets/_Project/Prefabs/Vfx";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";

        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";
        private const string RingSpritePath = "Assets/_Project/Art/Placeholder/Ring.png";

        private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.10f, 0.98f);
        private static readonly Color TileColor = new Color(0.16f, 0.17f, 0.2f);
        private static readonly Color HeaderColor = new Color(0.62f, 0.68f, 0.78f);

        [MenuItem("RPG/Phase 11/Build Hub UI And Combat Feedback", priority = 220)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject hud = GameObject.Find("HUD");
            GameObject systems = GameObject.Find("GameSystems");
            GameObject player = GameObject.Find("Player");
            GameObject devTools = GameObject.Find("DevTools");

            if (hud == null || systems == null || player == null)
            {
                Debug.LogError("[Phase 11] Missing HUD, GameSystems or Player. " +
                               "Run the earlier phase tools first.");
                return;
            }

            // --- 1. shared assets -------------------------------------------------
            CombatFeedbackChannel feedback = EditorSetupUtility.CreateOrLoadAsset<CombatFeedbackChannel>(
                CoreDataFolder + "/CombatFeedbackChannel.asset", out _);

            Directory.CreateDirectory(UiDataFolder);
            HealthBarStyle playerBarStyle = CreateHealthBarStyle("PlayerHealthBarStyle",
                hideWhenFull: false, fill: new Color(0.35f, 0.85f, 0.45f), width: 1.25f);
            HealthBarStyle enemyBarStyle = CreateHealthBarStyle("EnemyHealthBarStyle",
                hideWhenFull: true, fill: new Color(0.85f, 0.35f, 0.32f), width: 1f);

            // --- 2. combat feedback ----------------------------------------------
            GameObject sparkPrefab = BuildHitSparkPrefab();
            GameObject numberPrefab = BuildDamageNumberPrefab();
            BuildFeedbackPools(sparkPrefab, numberPrefab, feedback);

            AddFeedbackToEnemyPrefabs(feedback, enemyBarStyle);
            AddFeedbackToScenePlayer(player, feedback, playerBarStyle);
            AddFeedbackToLooseSceneCharacters(feedback, enemyBarStyle);

            // --- 3. the hub -------------------------------------------------------
            ClearCombatHudFurniture(hud);
            HubScreen hub = BuildHub(hud, systems, player);

            RewireGameFlow(systems, hub);
            RewireDebugOverlay(devTools, hub);
            KeepModalScreensOnTop(hud);
            BuildResetButton(hud, systems.GetComponent<GameFlowController>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Verify(player);

            Selection.activeGameObject = hub.gameObject;
            Debug.Log("<b>[Phase 11]</b> Hub UI and combat feedback installed. Press Play: pick a " +
                      "class, then swipe left from the stage list to reach GEAR and BAG. " +
                      "In a fight, health bars sit under every character and hits throw sparks " +
                      "and damage numbers.");
        }

        /// <summary>
        /// Checks the things this tool is supposed to have produced and says so, loudly, when
        /// one is missing.
        ///
        /// The first version of this tool skipped the player's health bar in complete silence,
        /// because the component it looked for was on the scene object and it was reading the
        /// prefab. A setup tool that can quietly do nothing is worse than one that fails.
        /// </summary>
        private static void Verify(GameObject player)
        {
            var playerBar = player.GetComponentInChildren<HealthBarView>(true);
            if (playerBar == null)
            {
                Debug.LogError("[Phase 11] VERIFY FAILED: the Player has no HealthBarView. " +
                               "Expected a 'HealthBar' child on the scene Player object.", player);
            }

            if (player.GetComponent<HitFeedbackEmitter>() == null)
            {
                Debug.LogError("[Phase 11] VERIFY FAILED: the Player has no HitFeedbackEmitter, " +
                               "so hits on the player will show no sparks or damage numbers.", player);
            }

            int enemiesWithBars = 0;
            int enemyPrefabs = 0;

            if (Directory.Exists(EnemyPrefabFolder))
            {
                string[] files = Directory.GetFiles(EnemyPrefabFolder, "*.prefab");
                enemyPrefabs = files.Length;

                for (int i = 0; i < files.Length; i++)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(files[i].Replace('\\', '/'));
                    if (asset != null && asset.GetComponentInChildren<HealthBarView>(true) != null)
                    {
                        enemiesWithBars++;
                    }
                }
            }

            if (enemiesWithBars < enemyPrefabs)
            {
                Debug.LogError($"[Phase 11] VERIFY FAILED: only {enemiesWithBars} of {enemyPrefabs} " +
                               "enemy prefabs have a health bar.");
            }

            if (playerBar != null && enemiesWithBars == enemyPrefabs)
            {
                Debug.Log($"<b>[Phase 11]</b> Verified: player health bar present, " +
                          $"{enemiesWithBars}/{enemyPrefabs} enemy prefabs have one.");
            }
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 11] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ================================================================== health bar styles

        private static HealthBarStyle CreateHealthBarStyle(string assetName, bool hideWhenFull,
            Color fill, float width)
        {
            HealthBarStyle style = EditorSetupUtility.CreateOrLoadAsset<HealthBarStyle>(
                $"{UiDataFolder}/{assetName}.asset", out bool created);

            // Only written on creation, so retuning a bar in the Inspector is not undone the
            // next time this tool runs.
            if (!created) return style;

            EditorSetupUtility.SetPrivateField(style, "size", new Vector2(width, 0.13f));
            EditorSetupUtility.SetPrivateField(style, "offset", new Vector2(0f, -0.68f));
            EditorSetupUtility.SetPrivateField(style, "fillColor", fill);
            EditorSetupUtility.SetPrivateField(style, "hideWhenFull", hideWhenFull);
            EditorUtility.SetDirty(style);

            return style;
        }

        // ================================================================== effect prefabs

        private static GameObject BuildHitSparkPrefab()
        {
            Directory.CreateDirectory(VfxPrefabFolder);
            string path = VfxPrefabFolder + "/HitSpark.prefab";

            var spark = new GameObject("HitSpark");
            var renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
            renderer.color = new Color(1f, 0.95f, 0.75f);

            // Above characters (order 1) and health bars, because an impact that draws behind
            // the thing it hit is worse than no impact at all.
            renderer.sortingOrder = 80;

            var component = spark.AddComponent<HitSpark>();
            EditorSetupUtility.SetPrivateField(component, "sprite", renderer);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(spark, path);
            Object.DestroyImmediate(spark);
            return saved;
        }

        private static GameObject BuildDamageNumberPrefab()
        {
            Directory.CreateDirectory(VfxPrefabFolder);
            string path = VfxPrefabFolder + "/DamageNumber.prefab";

            var number = new GameObject("DamageNumber");

            var text = number.AddComponent<TextMesh>();
            Font font = EditorSetupUtility.GetDefaultFont();
            text.font = font;
            text.text = "0";
            text.fontSize = 64;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = 1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            // A TextMesh renders nothing at all unless it is given the font's own material.
            var renderer = number.GetComponent<MeshRenderer>();
            if (renderer != null && font != null) renderer.sharedMaterial = font.material;
            if (renderer != null) renderer.sortingOrder = 90;

            number.AddComponent<DamageNumber>();
            number.transform.localScale = Vector3.one * 0.1f;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(number, path);
            Object.DestroyImmediate(number);
            return saved;
        }

        private static void BuildFeedbackPools(GameObject sparkPrefab, GameObject numberPrefab,
            CombatFeedbackChannel channel)
        {
            GameObject sparkPoolObject = GameObject.Find("HitSparkPool") ?? new GameObject("HitSparkPool");
            var sparkPool = EditorSetupUtility.EnsureComponent<HitSparkPool>(sparkPoolObject);
            EditorSetupUtility.SetPrivateField(sparkPool, "prefab", sparkPrefab.GetComponent<HitSpark>());
            EditorSetupUtility.SetPrivateField(sparkPool, "publishAs", channel);
            EditorSetupUtility.SetPrivateField(sparkPool, "prewarmCount", 24);

            GameObject numberPoolObject = GameObject.Find("DamageNumberPool")
                                          ?? new GameObject("DamageNumberPool");
            var numberPool = EditorSetupUtility.EnsureComponent<DamageNumberPool>(numberPoolObject);
            EditorSetupUtility.SetPrivateField(numberPool, "prefab", numberPrefab.GetComponent<DamageNumber>());
            EditorSetupUtility.SetPrivateField(numberPool, "publishAs", channel);
            EditorSetupUtility.SetPrivateField(numberPool, "prewarmCount", 24);
        }

        // ================================================================== characters

        /// <summary>
        /// The player is handled on the SCENE object, not on Player.prefab.
        ///
        /// That prefab was saved back in Phase 1 and still holds only the three components it
        /// had then; Health, PlayerStats, EquipmentManager and PlayerLevel were all added to the
        /// scene instance in later phases and were never applied back. Editing the prefab would
        /// therefore find no Health and silently skip the player - which is exactly what the
        /// first version of this tool did, leaving the player with no health bar.
        ///
        /// The health bar and the emitter are added as prefab-instance overrides, which is where
        /// the rest of the player's real setup already lives.
        /// </summary>
        private static void AddFeedbackToScenePlayer(GameObject player, CombatFeedbackChannel channel,
            HealthBarStyle style)
        {
            if (player.GetComponent<Health>() == null)
            {
                Debug.LogError("[Phase 11] The scene Player has no Health component, so it cannot " +
                               "have a health bar. Run the Phase 4 tool first.", player);
                return;
            }

            AddFeedbackTo(player, channel, style);
        }

        private static void AddFeedbackToEnemyPrefabs(CombatFeedbackChannel channel, HealthBarStyle style)
        {
            if (!Directory.Exists(EnemyPrefabFolder)) return;

            string[] files = Directory.GetFiles(EnemyPrefabFolder, "*.prefab");
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                AddFeedbackTo(contents, channel, style);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// Training dummies and anything else placed straight into the scene. Prefab instances
        /// are skipped, because they were already handled at the prefab.
        /// </summary>
        private static void AddFeedbackToLooseSceneCharacters(CombatFeedbackChannel channel,
            HealthBarStyle style)
        {
            Health[] all = Object.FindObjectsByType<Health>(FindObjectsInactive.Include);

            for (int i = 0; i < all.Length; i++)
            {
                GameObject target = all[i].gameObject;

                // Already has a bar: either this tool just gave it one (the player), or it
                // inherits one from its prefab (every spawned enemy). Either way, leave it.
                if (target.GetComponentInChildren<HealthBarView>(true) != null) continue;

                AddFeedbackTo(target, channel, style);
            }
        }

        private static void AddFeedbackTo(GameObject character, CombatFeedbackChannel channel,
            HealthBarStyle style)
        {
            if (character == null || character.GetComponent<Health>() == null) return;

            var emitter = EditorSetupUtility.EnsureComponent<HitFeedbackEmitter>(character);
            EditorSetupUtility.SetPrivateField(emitter, "feedback", channel);

            BuildHealthBar(character, style);
        }

        private static void BuildHealthBar(GameObject character, HealthBarStyle style)
        {
            Transform existing = EditorSetupUtility.FindChild(character.transform, "HealthBar");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

            var barRoot = new GameObject("HealthBar");
            barRoot.transform.SetParent(character.transform, false);

            SpriteRenderer background = CreateBarPart("Background", barRoot.transform, sprite);
            SpriteRenderer trail = CreateBarPart("Trail", barRoot.transform, sprite);
            SpriteRenderer fill = CreateBarPart("Fill", barRoot.transform, sprite);

            var view = barRoot.AddComponent<HealthBarView>();
            EditorSetupUtility.SetPrivateField(view, "health", character.GetComponent<Health>());
            EditorSetupUtility.SetPrivateField(view, "style", style);
            EditorSetupUtility.SetPrivateField(view, "background", background);
            EditorSetupUtility.SetPrivateField(view, "trail", trail);
            EditorSetupUtility.SetPrivateField(view, "fill", fill);

            view.ApplyStyle();
        }

        private static SpriteRenderer CreateBarPart(string name, Transform parent, Sprite sprite)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        // ================================================================== combat HUD cleanup

        /// <summary>
        /// Strips the out-of-combat furniture from the combat HUD. The joystick and the attack
        /// button are all that should remain over the arena.
        /// </summary>
        private static void ClearCombatHudFurniture(GameObject hud)
        {
            string[] doomed = { "BagButton", "InventoryPanel", "StageSelectPanel", "HubScreen" };

            for (int i = 0; i < doomed.Length; i++)
            {
                Transform found = EditorSetupUtility.FindChild(hud.transform, doomed[i]);
                if (found != null) Object.DestroyImmediate(found.gameObject);
            }
        }

        /// <summary>
        /// The screens that interrupt everything - class select, stage complete, defeat - are
        /// pushed to the end of the HUD so they draw over the hub rather than under it.
        /// </summary>
        private static void KeepModalScreensOnTop(GameObject hud)
        {
            string[] onTop = { "ClassSelectPanel", "StageCompletePanel", "StageFailedPanel" };

            for (int i = 0; i < onTop.Length; i++)
            {
                Transform found = EditorSetupUtility.FindChild(hud.transform, onTop[i]);
                if (found != null) found.SetAsLastSibling();
            }
        }
        // ================================================================== the hub

        private static HubScreen BuildHub(GameObject hud, GameObject systems, GameObject player)
        {
            Image root = EditorSetupUtility.CreateUiImage("HubScreen", hud.transform, null, PanelColor);
            var rootRect = (RectTransform)root.transform;
            EditorSetupUtility.StretchFull(rootRect);

            Text title = TopLabel(rootRect, "Title", "STAGES", 52, TextAnchor.MiddleCenter,
                new Vector2(1000f, 80f), -55f);

            RectTransform tabBar = TopRect("TabBar", rootRect, new Vector2(980f, 100f), -160f);
            var tabLayout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 14f;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            Button tabTemplate = LabeledButton("TabTemplate", tabBar, "TAB", 32,
                new Color(0.14f, 0.15f, 0.18f), new Vector2(300f, 100f));
            tabTemplate.gameObject.SetActive(false);

            // The viewport is what the pages are clipped to, and what the finger drags. It needs
            // a graphic to be hit by touches at all, hence a fully transparent Image.
            Image viewport = EditorSetupUtility.CreateUiImage("PageViewport", rootRect, null, Color.clear);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(30f, 30f);
            viewportRect.offsetMax = new Vector2(-30f, -230f);
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var pages = EditorSetupUtility.CreateUiObject("Pages", viewportRect)
                .GetComponent<RectTransform>();

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = pages;
            scroll.viewport = viewportRect;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 0f;

            var swipe = viewport.gameObject.AddComponent<SwipePageView>();

            StageSelectPanel stages = BuildStagesPage(pages);
            GearPage gear = BuildGearPage(pages, systems, player);
            InventoryPanel bag = BuildBagPage(pages, systems, player);

            var hub = root.gameObject.AddComponent<HubScreen>();
            EditorSetupUtility.SetPrivateObjectList(hub, "pages",
                new Object[] { stages, gear, bag });
            EditorSetupUtility.SetPrivateField(hub, "stagesPage", stages);
            EditorSetupUtility.SetPrivateField(hub, "pageView", swipe);
            EditorSetupUtility.SetPrivateField(hub, "titleLabel", title);
            EditorSetupUtility.SetPrivateField(hub, "tabContainer", tabBar);
            EditorSetupUtility.SetPrivateField(hub, "tabTemplate", tabTemplate);
            EditorSetupUtility.SetPrivateField(hub, "panelRoot", root.gameObject);
            EditorSetupUtility.SetPrivateField(hub, "playerController",
                player.GetComponent<PlayerController>());

            return hub;
        }

        // ------------------------------------------------------------------ page 1: stages

        private static StageSelectPanel BuildStagesPage(RectTransform pages)
        {
            RectTransform page = CreatePage("StagesPage", pages);

            Text headline = TopLabel(page, "Headline", string.Empty, 34, TextAnchor.MiddleCenter,
                new Vector2(900f, 60f), -10f);
            headline.color = new Color(0.95f, 0.85f, 0.45f);

            RectTransform list = TopRect("ButtonContainer", page, new Vector2(940f, 1300f), -90f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = false;

            Button template = LabeledButton("StageButtonTemplate", list, "Stage", 34,
                new Color(0.18f, 0.28f, 0.22f), new Vector2(900f, 150f));
            template.gameObject.SetActive(false);

            var panel = page.gameObject.AddComponent<StageSelectPanel>();
            EditorSetupUtility.SetPrivateField(panel, "pageName", "Stages");
            EditorSetupUtility.SetPrivateField(panel, "registry",
                AssetDatabase.LoadAssetAtPath<StageRegistry>("Assets/_Project/Data/Stages/StageRegistry.asset"));
            EditorSetupUtility.SetPrivateField(panel, "progress",
                AssetDatabase.LoadAssetAtPath<StageProgressState>("Assets/_Project/Data/Stages/StageProgress.asset"));
            EditorSetupUtility.SetPrivateField(panel, "headlineLabel", headline);
            EditorSetupUtility.SetPrivateField(panel, "buttonContainer", list);
            EditorSetupUtility.SetPrivateField(panel, "buttonTemplate", template);

            return panel;
        }

        // ------------------------------------------------------------------ page 2: gear

        private static GearPage BuildGearPage(RectTransform pages, GameObject systems, GameObject player)
        {
            RectTransform page = CreatePage("GearPage", pages);

            Text className = TopLabel(page, "ClassLabel", "KNIGHT", 44, TextAnchor.MiddleCenter,
                new Vector2(900f, 60f), -10f);

            Text level = TopLabel(page, "LevelLabel", "Level 1", 28, TextAnchor.MiddleCenter,
                new Vector2(900f, 40f), -80f);
            level.color = HeaderColor;

            // A Filled image needs a real sprite: with a null sprite Unity draws a plain quad
            // and ignores fillAmount entirely, so the bar would always look full.
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

            Image xpBack = EditorSetupUtility.CreateUiImage("XpBar", page, square,
                new Color(0.14f, 0.15f, 0.18f));
            PlaceTop((RectTransform)xpBack.transform, new Vector2(700f, 18f), -130f);

            Image xpFill = EditorSetupUtility.CreateUiImage("XpFill", xpBack.transform, square,
                new Color(0.45f, 0.72f, 0.95f));
            EditorSetupUtility.StretchFull((RectTransform)xpFill.transform);
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            xpFill.fillAmount = 0f;

            Text currency = TopLabel(page, "CurrencyLabel", "Gold 0     Gems 0", 28,
                TextAnchor.MiddleCenter, new Vector2(900f, 40f), -165f);
            currency.color = new Color(0.9f, 0.82f, 0.5f);

            Text equippedHeader = TopLabel(page, "EquippedHeader", "EQUIPPED", 26,
                TextAnchor.MiddleLeft, new Vector2(940f, 36f), -220f);
            equippedHeader.color = HeaderColor;

            RectTransform slots = TopRect("SlotContainer", page, new Vector2(940f, 340f), -262f);
            var slotGrid = slots.gameObject.AddComponent<GridLayoutGroup>();
            slotGrid.cellSize = new Vector2(300f, 150f);
            slotGrid.spacing = new Vector2(14f, 14f);
            slotGrid.childAlignment = TextAnchor.UpperCenter;

            Button slotTemplate = TileButton("SlotButtonTemplate", slots, 24);

            Text statsHeader = TopLabel(page, "StatsHeader", "STATS", 26, TextAnchor.MiddleLeft,
                new Vector2(940f, 36f), -620f);
            statsHeader.color = HeaderColor;

            RectTransform statList = TopRect("StatContainer", page, new Vector2(940f, 640f), -662f);
            var statLayout = statList.gameObject.AddComponent<VerticalLayoutGroup>();
            statLayout.spacing = 2f;
            statLayout.childForceExpandHeight = false;
            statLayout.childControlHeight = false;
            statLayout.childForceExpandWidth = false;
            statLayout.childControlWidth = false;

            StatRowView statTemplate = BuildStatRowTemplate(statList);

            Text details = TopLabel(page, "Details", "Tap an equipped item to take it off.", 26,
                TextAnchor.UpperLeft, new Vector2(940f, 220f), -1320f);
            details.color = new Color(0.8f, 0.84f, 0.9f);

            var gear = page.gameObject.AddComponent<GearPage>();
            EditorSetupUtility.SetPrivateField(gear, "pageName", "Gear");
            EditorSetupUtility.SetPrivateField(gear, "playerStats", player.GetComponent<PlayerStats>());
            EditorSetupUtility.SetPrivateField(gear, "playerLevel", player.GetComponent<PlayerLevel>());
            EditorSetupUtility.SetPrivateField(gear, "playerHealth", player.GetComponent<Health>());
            EditorSetupUtility.SetPrivateField(gear, "equipment", player.GetComponent<EquipmentManager>());
            EditorSetupUtility.SetPrivateField(gear, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(gear, "itemRegistry",
                AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/_Project/Data/Items/ItemRegistry.asset"));
            EditorSetupUtility.SetPrivateField(gear, "rarityTable",
                AssetDatabase.LoadAssetAtPath<RarityTable>("Assets/_Project/Data/Items/RarityTable.asset"));
            EditorSetupUtility.SetPrivateField(gear, "classLabel", className);
            EditorSetupUtility.SetPrivateField(gear, "levelLabel", level);
            EditorSetupUtility.SetPrivateField(gear, "xpFill", xpFill);
            EditorSetupUtility.SetPrivateField(gear, "currencyLabel", currency);
            EditorSetupUtility.SetPrivateField(gear, "slotContainer", slots);
            EditorSetupUtility.SetPrivateField(gear, "slotButtonTemplate", slotTemplate);
            EditorSetupUtility.SetPrivateField(gear, "statContainer", statList);
            EditorSetupUtility.SetPrivateField(gear, "statRowTemplate", statTemplate);
            EditorSetupUtility.SetPrivateField(gear, "detailsLabel", details);

            return gear;
        }

        private static StatRowView BuildStatRowTemplate(RectTransform parent)
        {
            GameObject row = EditorSetupUtility.CreateUiObject("StatRowTemplate", parent);
            var rowRect = (RectTransform)row.transform;
            rowRect.sizeDelta = new Vector2(940f, 44f);

            Text name = EditorSetupUtility.CreateUiText("Name", rowRect, "Stat", 26, TextAnchor.MiddleLeft);
            var nameRect = (RectTransform)name.transform;
            EditorSetupUtility.StretchFull(nameRect);
            nameRect.offsetMax = new Vector2(-380f, 0f);
            name.color = new Color(0.78f, 0.82f, 0.88f);

            Text value = EditorSetupUtility.CreateUiText("Value", rowRect, "0", 26, TextAnchor.MiddleRight);
            var valueRect = (RectTransform)value.transform;
            EditorSetupUtility.StretchFull(valueRect);
            valueRect.offsetMin = new Vector2(560f, 0f);

            var view = row.AddComponent<StatRowView>();
            EditorSetupUtility.SetPrivateField(view, "nameLabel", name);
            EditorSetupUtility.SetPrivateField(view, "valueLabel", value);

            row.SetActive(false);
            return view;
        }

        // ------------------------------------------------------------------ page 3: bag

        private static InventoryPanel BuildBagPage(RectTransform pages, GameObject systems, GameObject player)
        {
            RectTransform page = CreatePage("BagPage", pages);

            Text header = TopLabel(page, "BagHeader", "0 / 20", 32, TextAnchor.MiddleLeft,
                new Vector2(600f, 50f), -10f);

            Button expand = LabeledButton("ExpandButton", page, "+5 slots", 24,
                new Color(0.2f, 0.3f, 0.34f), new Vector2(240f, 90f));
            var expandRect = (RectTransform)expand.transform;
            expandRect.anchorMin = expandRect.anchorMax = new Vector2(1f, 1f);
            expandRect.pivot = new Vector2(1f, 1f);
            expandRect.anchoredPosition = new Vector2(0f, -5f);

            RectTransform grid = TopRect("BagContainer", page, new Vector2(960f, 1000f), -110f);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(228f, 150f);
            gridLayout.spacing = new Vector2(14f, 14f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            Button tile = TileButton("BagButtonTemplate", grid, 22);

            Text details = TopLabel(page, "Details", "Tap an item to equip it.", 26,
                TextAnchor.UpperLeft, new Vector2(940f, 280f), -1130f);
            details.color = new Color(0.8f, 0.84f, 0.9f);

            var bag = page.gameObject.AddComponent<InventoryPanel>();
            EditorSetupUtility.SetPrivateField(bag, "pageName", "Bag");
            EditorSetupUtility.SetPrivateField(bag, "inventory", systems.GetComponent<InventoryManager>());
            EditorSetupUtility.SetPrivateField(bag, "equipment", player.GetComponent<EquipmentManager>());
            EditorSetupUtility.SetPrivateField(bag, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(bag, "itemRegistry",
                AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/_Project/Data/Items/ItemRegistry.asset"));
            EditorSetupUtility.SetPrivateField(bag, "rarityTable",
                AssetDatabase.LoadAssetAtPath<RarityTable>("Assets/_Project/Data/Items/RarityTable.asset"));
            EditorSetupUtility.SetPrivateField(bag, "bagContainer", grid);
            EditorSetupUtility.SetPrivateField(bag, "bagButtonTemplate", tile);
            EditorSetupUtility.SetPrivateField(bag, "headerLabel", header);
            EditorSetupUtility.SetPrivateField(bag, "detailsLabel", details);
            EditorSetupUtility.SetPrivateField(bag, "expandButton", expand);
            EditorSetupUtility.SetPrivateField(bag, "expandLabel", expand.GetComponentInChildren<Text>());

            return bag;
        }

        // ================================================================== rewiring

        private static void RewireGameFlow(GameObject systems, HubScreen hub)
        {
            var flow = systems.GetComponent<GameFlowController>();
            if (flow == null)
            {
                Debug.LogWarning("[Phase 11] No GameFlowController found; the hub will not open " +
                                 "on its own. Run the Phase 6 tool first.");
                return;
            }

            EditorSetupUtility.SetPrivateField(flow, "hubScreen", hub);

            // Needed so a profile reset can restore the starting bag size from the same config
            // the game uses, rather than from a hardcoded fallback.
            var saveManager = systems.GetComponent<SaveManager>();
            if (saveManager != null)
            {
                EditorSetupUtility.SetPrivateField(saveManager, "inventoryConfig",
                    AssetDatabase.LoadAssetAtPath<InventoryConfig>(
                        "Assets/_Project/Data/Inventory/InventoryConfig.asset"));
            }
        }

        /// <summary>
        /// A small RESET button pinned to the top-right corner: wipes the profile and drops
        /// straight back to class select, without stopping and re-entering Play mode.
        ///
        /// A playtest convenience, not a game feature - delete this call when the real settings
        /// menu arrives. It is deliberately the LAST child of the HUD so it floats above every
        /// panel, including the hub, which is where you are standing when you want it.
        /// </summary>
        private static void BuildResetButton(GameObject hud, GameFlowController flow)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "DebugResetButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            if (flow == null) return;

            Button button = LabeledButton("DebugResetButton", hud.transform, "RESET", 22,
                new Color(0.42f, 0.16f, 0.18f, 0.85f), new Vector2(150f, 64f));

            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.SetAsLastSibling();

            // A persistent listener, so the wiring is scene data you can see in the Inspector
            // rather than something only this tool knows about.
            UnityEventTools.AddPersistentListener(button.onClick,
                new UnityAction(flow.RestartFromZero));
        }

        private static void RewireDebugOverlay(GameObject devTools, HubScreen hub)
        {
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "hubScreen", hub);

            // The real stat readout lives on the gear page now, so the development overlay
            // starts closed rather than sitting over the arena.
            EditorSetupUtility.SetPrivateField(overlay, "startVisible", false);
        }

        // ================================================================== UI helpers

        private static RectTransform CreatePage(string name, RectTransform parent)
        {
            GameObject page = EditorSetupUtility.CreateUiObject(name, parent);
            var rect = (RectTransform)page.transform;

            // SwipePageView re-lays these out at runtime against the real viewport width; these
            // values only make the page sane to look at in the editor.
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1020f, 0f);
            return rect;
        }

        private static RectTransform TopRect(string name, RectTransform parent, Vector2 size, float y)
        {
            GameObject go = EditorSetupUtility.CreateUiObject(name, parent);
            var rect = (RectTransform)go.transform;
            PlaceTop(rect, size, y);
            return rect;
        }

        private static void PlaceTop(RectTransform rect, Vector2 size, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static Text TopLabel(RectTransform parent, string name, string content, int fontSize,
            TextAnchor anchor, Vector2 size, float y)
        {
            Text text = EditorSetupUtility.CreateUiText(name, parent, content, fontSize, anchor);
            PlaceTop((RectTransform)text.transform, size, y);
            return text;
        }

        private static Button LabeledButton(string name, Transform parent, string content,
            int fontSize, Color color, Vector2 size)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, color);
            ((RectTransform)image.transform).sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, content, fontSize,
                TextAnchor.MiddleCenter);
            EditorSetupUtility.StretchFull((RectTransform)label.transform);

            return button;
        }

        /// <summary>An item tile: a coloured square whose label shrinks to fit a long item name.</summary>
        private static Button TileButton(string name, Transform parent, int fontSize)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, TileColor);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, "-", fontSize,
                TextAnchor.MiddleCenter);
            var labelRect = (RectTransform)label.transform;
            EditorSetupUtility.StretchFull(labelRect);
            labelRect.offsetMin = new Vector2(8f, 8f);
            labelRect.offsetMax = new Vector2(-8f, -8f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = fontSize;

            image.gameObject.SetActive(false);
            return button;
        }
    }
}
