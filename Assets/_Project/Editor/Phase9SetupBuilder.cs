using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.Economy;
using RPG.Items;
using RPG.Loot;
using RPG.Player;
using RPG.Progression;
using RPG.Stages;
using RPG.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 9 setup: builds the stage completion screen and hands it the post-stage beat.
    /// Safe to re-run; the screen is rebuilt each time.
    /// </summary>
    public static class Phase9SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";

        private static readonly Color CyanFill = new Color(0.25f, 0.85f, 1f);
        private static readonly Color DarkFill = new Color(0.12f, 0.28f, 0.6f);
        private static readonly Color BarBack = new Color(0.1f, 0.11f, 0.14f);

        [MenuItem("RPG/Phase 9/Add Completion Screen", priority = 180)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject hud = GameObject.Find("HUD");
            GameObject systems = GameObject.Find("GameSystems");
            GameObject player = GameObject.Find("Player");
            if (hud == null || systems == null || player == null)
            {
                Debug.LogError("[Phase 9] Missing HUD, GameSystems or Player. Run the earlier phase tools first.");
                return;
            }

            StageCompleteScreen screen = BuildScreen(hud, player, systems);
            if (screen == null) return;

            // The screen now banks rewards itself, the moment it opens, so the automatic
            // claim-on-completion is turned off to avoid claiming twice.
            var claimer = systems.GetComponent<RewardClaimer>();
            if (claimer != null) EditorSetupUtility.SetPrivateField(claimer, "claimOnStageComplete", false);

            var flow = systems.GetComponent<GameFlowController>();
            if (flow != null) EditorSetupUtility.SetPrivateField(flow, "stageCompleteScreen", screen);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 9]</b> Completion screen installed. Clear a stage to see the XP bar fill.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 9] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static StageCompleteScreen BuildScreen(GameObject hud, GameObject player, GameObject systems)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "StageCompletePanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);

            Image panelImage = EditorSetupUtility.CreateUiImage("StageCompletePanel", hud.transform,
                null, new Color(0.03f, 0.04f, 0.07f, 0.97f));
            var panelRect = (RectTransform)panelImage.transform;
            EditorSetupUtility.StretchFull(panelRect);
            panelRect.SetAsLastSibling();

            Text title = Label(panelRect, "Title", "STAGE COMPLETE", 56, TextAnchor.MiddleCenter,
                new Vector2(1000f, 90f), -150f);

            Text levelUp = Label(panelRect, "LevelUpLabel", "LEVEL UP!", 46, TextAnchor.MiddleCenter,
                new Vector2(1000f, 70f), -300f);
            levelUp.color = new Color(1f, 0.85f, 0.3f);

            Text level = Label(panelRect, "LevelLabel", "LEVEL 1", 40, TextAnchor.MiddleCenter,
                new Vector2(1000f, 60f), -400f);

            // --- XP bar: dark "just earned" fill behind, cyan "already held" fill in front ---
            Image barBackground = EditorSetupUtility.CreateUiImage("XpBar", panelRect, square, BarBack);
            var barRect = (RectTransform)barBackground.transform;
            TopAnchored(barRect, new Vector2(880f, 54f), -470f);

            Image pendingFill = CreateFill("XpFillPending", barRect, square, DarkFill);
            Image currentFill = CreateFill("XpFillCurrent", barRect, square, CyanFill);

            Text xpValue = Label(panelRect, "XpValueLabel", "0 / 0", 28, TextAnchor.MiddleCenter,
                new Vector2(880f, 40f), -535f);

            Text xpGained = Label(panelRect, "XpGainedLabel", "+0 XP", 34, TextAnchor.MiddleCenter,
                new Vector2(880f, 50f), -595f);
            xpGained.color = CyanFill;

            Text gold = Label(panelRect, "GoldLabel", "+0 Gold", 34, TextAnchor.MiddleCenter,
                new Vector2(880f, 50f), -655f);
            gold.color = new Color(1f, 0.85f, 0.35f);

            Text itemsHeader = Label(panelRect, "ItemsHeader", "EQUIPMENT OBTAINED", 28,
                TextAnchor.MiddleCenter, new Vector2(1000f, 44f), -740f);

            GameObject itemsContainer = EditorSetupUtility.CreateUiObject("ItemsContainer", panelRect);
            var itemsRect = (RectTransform)itemsContainer.transform;
            TopAnchored(itemsRect, new Vector2(960f, 620f), -1400f);

            var grid = itemsContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 130f);
            grid.spacing = new Vector2(14f, 14f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            Button itemTile = CreateTile("ItemTileTemplate", itemsRect);

            Button continueButton = CreateButton("ContinueButton", panelRect, "CONTINUE", 38,
                new Color(0.2f, 0.42f, 0.32f), new Vector2(520f, 130f));
            var continueRect = (RectTransform)continueButton.transform;
            continueRect.anchorMin = continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0f, 150f);

            // --- Wire it up ---
            var screen = panelImage.gameObject.AddComponent<StageCompleteScreen>();

            EditorSetupUtility.SetPrivateField(screen, "stageEvents",
                AssetDatabase.LoadAssetAtPath<StageEventChannel>("Assets/_Project/Data/Stages/StageEventChannel.asset"));
            EditorSetupUtility.SetPrivateField(screen, "playerLevel", player.GetComponent<PlayerLevel>());
            EditorSetupUtility.SetPrivateField(screen, "xpCurve",
                AssetDatabase.LoadAssetAtPath<XpCurveData>("Assets/_Project/Data/Progression/XpCurve.asset"));
            EditorSetupUtility.SetPrivateField(screen, "collector", systems.GetComponent<StageRewardCollector>());
            EditorSetupUtility.SetPrivateField(screen, "claimer", systems.GetComponent<RewardClaimer>());
            EditorSetupUtility.SetPrivateField(screen, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(screen, "itemRegistry",
                AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/_Project/Data/Items/ItemRegistry.asset"));
            EditorSetupUtility.SetPrivateField(screen, "rarityTable",
                AssetDatabase.LoadAssetAtPath<RarityTable>("Assets/_Project/Data/Items/RarityTable.asset"));

            EditorSetupUtility.SetPrivateField(screen, "titleLabel", title);
            EditorSetupUtility.SetPrivateField(screen, "levelLabel", level);
            EditorSetupUtility.SetPrivateField(screen, "xpValueLabel", xpValue);
            EditorSetupUtility.SetPrivateField(screen, "xpGainedLabel", xpGained);
            EditorSetupUtility.SetPrivateField(screen, "goldLabel", gold);
            EditorSetupUtility.SetPrivateField(screen, "levelUpLabel", levelUp);
            EditorSetupUtility.SetPrivateField(screen, "itemsHeaderLabel", itemsHeader);
            EditorSetupUtility.SetPrivateField(screen, "xpFillCurrent", currentFill);
            EditorSetupUtility.SetPrivateField(screen, "xpFillPending", pendingFill);
            EditorSetupUtility.SetPrivateField(screen, "itemsContainer", itemsRect);
            EditorSetupUtility.SetPrivateField(screen, "itemTileTemplate", itemTile);
            EditorSetupUtility.SetPrivateField(screen, "continueButton", continueButton);
            EditorSetupUtility.SetPrivateField(screen, "panelRoot", panelImage.gameObject);
            EditorSetupUtility.SetPrivateField(screen, "playerController", player.GetComponent<PlayerController>());

            return screen;
        }

        // ------------------------------------------------------------------ ui helpers

        /// <summary>
        /// A horizontally filled bar segment. Image.Type.Filled needs a sprite to fill, which
        /// is why the placeholder square is assigned rather than leaving it empty.
        /// </summary>
        private static Image CreateFill(string name, RectTransform parent, Sprite sprite, Color color)
        {
            Image fill = EditorSetupUtility.CreateUiImage(name, parent, sprite, color);
            var rect = (RectTransform)fill.transform;
            EditorSetupUtility.StretchFull(rect);
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            return fill;
        }

        private static Text Label(RectTransform parent, string name, string text, int fontSize,
            TextAnchor alignment, Vector2 size, float y)
        {
            Text label = EditorSetupUtility.CreateUiText(name, parent, text, fontSize, alignment);
            TopAnchored((RectTransform)label.transform, size, y);
            return label;
        }

        private static Button CreateTile(string name, RectTransform parent)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, new Color(0.16f, 0.17f, 0.2f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, "-", 22,
                TextAnchor.MiddleCenter);
            var labelRect = (RectTransform)label.transform;
            EditorSetupUtility.StretchFull(labelRect);
            labelRect.offsetMin = new Vector2(6f, 6f);
            labelRect.offsetMax = new Vector2(-6f, -6f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 22;

            image.gameObject.SetActive(false);
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string text, int fontSize,
            Color color, Vector2 size)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, color);
            ((RectTransform)image.transform).sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, text, fontSize,
                TextAnchor.MiddleCenter);
            EditorSetupUtility.StretchFull((RectTransform)label.transform);
            return button;
        }

        private static void TopAnchored(RectTransform rect, Vector2 size, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, y);
        }
    }
}
