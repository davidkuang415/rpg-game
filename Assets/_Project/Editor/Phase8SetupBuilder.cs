using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.DebugTools;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Loot;
using RPG.Player;
using RPG.Stages;
using RPG.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 8 setup: wallet, inventory, equipment manager, reward claiming, and the bag screen.
    /// Safe to re-run; the bag UI is rebuilt, data assets are reused.
    /// </summary>
    public static class Phase8SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string InventoryFolder = "Assets/_Project/Data/Inventory";
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string StageFolder = "Assets/_Project/Data/Stages";

        [MenuItem("RPG/Phase 8/Add Inventory And Equipment", priority = 160)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("HUD");
            GameObject systems = GameObject.Find("GameSystems");
            if (player == null || hud == null || systems == null)
            {
                Debug.LogError("[Phase 8] Missing Player, HUD or GameSystems. Run the earlier phase tools first.");
                return;
            }

            var config = EditorSetupUtility.CreateOrLoadAsset<InventoryConfig>(
                InventoryFolder + "/InventoryConfig.asset", out _);
            var itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemFolder + "/ItemRegistry.asset");
            var rarityTable = AssetDatabase.LoadAssetAtPath<RarityTable>(ItemFolder + "/RarityTable.asset");
            var stageEvents = AssetDatabase.LoadAssetAtPath<StageEventChannel>(StageFolder + "/StageEventChannel.asset");

            if (itemRegistry == null || rarityTable == null)
            {
                Debug.LogError("[Phase 8] ItemRegistry or RarityTable missing. Run the Phase 7 tool first.");
                return;
            }

            // --- Account systems ---
            var wallet = EditorSetupUtility.EnsureComponent<CurrencyWallet>(systems);

            var inventory = EditorSetupUtility.EnsureComponent<InventoryManager>(systems);
            EditorSetupUtility.SetPrivateField(inventory, "config", config);
            EditorSetupUtility.SetPrivateField(inventory, "wallet", wallet);

            var collector = systems.GetComponent<StageRewardCollector>();
            var claimer = EditorSetupUtility.EnsureComponent<RewardClaimer>(systems);
            EditorSetupUtility.SetPrivateField(claimer, "collector", collector);
            EditorSetupUtility.SetPrivateField(claimer, "inventory", inventory);
            EditorSetupUtility.SetPrivateField(claimer, "wallet", wallet);
            EditorSetupUtility.SetPrivateField(claimer, "stageEvents", stageEvents);

            // --- Player ---
            var equipment = EditorSetupUtility.EnsureComponent<EquipmentManager>(player);
            EditorSetupUtility.SetPrivateField(equipment, "itemRegistry", itemRegistry);
            EditorSetupUtility.SetPrivateField(equipment, "rarityTable", rarityTable);
            EditorSetupUtility.SetPrivateField(equipment, "inventory", inventory);

            // --- UI ---
            InventoryPanel panel = BuildInventoryPanel(hud, player, inventory, equipment,
                itemRegistry, rarityTable, wallet);
            BuildBagHudButton(hud, panel);

            WireDebugOverlay(inventory, wallet, claimer, panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 8]</b> Inventory and equipment installed. Clear a stage, then open " +
                      "the BAG (top-right) and tap an item to equip it.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 8] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ inventory panel

        private static InventoryPanel BuildInventoryPanel(GameObject hud, GameObject player,
            InventoryManager inventory, EquipmentManager equipment, ItemRegistry registry,
            RarityTable rarityTable, CurrencyWallet wallet)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "InventoryPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Image panelImage = EditorSetupUtility.CreateUiImage("InventoryPanel", hud.transform,
                null, new Color(0.04f, 0.05f, 0.08f, 0.96f));
            var panelRect = (RectTransform)panelImage.transform;
            EditorSetupUtility.StretchFull(panelRect);
            panelRect.SetAsLastSibling();

            // Header: bag count and currencies.
            Text header = EditorSetupUtility.CreateUiText("Header", panelRect, "BAG", 40, TextAnchor.MiddleCenter);
            TopAnchored((RectTransform)header.transform, new Vector2(1000f, 80f), -110f);

            // Equipped slots row.
            Text slotTitle = EditorSetupUtility.CreateUiText("SlotTitle", panelRect, "EQUIPPED", 28,
                TextAnchor.MiddleLeft);
            TopAnchored((RectTransform)slotTitle.transform, new Vector2(1000f, 40f), -200f);

            GameObject slotContainer = EditorSetupUtility.CreateUiObject("SlotContainer", panelRect);
            var slotRect = (RectTransform)slotContainer.transform;
            TopAnchored(slotRect, new Vector2(1000f, 150f), -305f);

            var slotLayout = slotContainer.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 12f;
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlWidth = true;
            slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth = true;
            slotLayout.childForceExpandHeight = true;

            Button slotTemplate = CreateTileButton("SlotButtonTemplate", slotRect, 22);

            // Bag grid.
            Text bagTitle = EditorSetupUtility.CreateUiText("BagTitle", panelRect, "BAG", 28, TextAnchor.MiddleLeft);
            TopAnchored((RectTransform)bagTitle.transform, new Vector2(1000f, 40f), -420f);

            GameObject bagContainer = EditorSetupUtility.CreateUiObject("BagContainer", panelRect);
            var bagRect = (RectTransform)bagContainer.transform;
            TopAnchored(bagRect, new Vector2(1000f, 640f), -770f);

            var grid = bagContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(190f, 140f);
            grid.spacing = new Vector2(12f, 12f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            Button bagTemplate = CreateTileButton("BagButtonTemplate", bagRect, 22);

            // Details.
            Text details = EditorSetupUtility.CreateUiText("Details", panelRect, "", 26, TextAnchor.UpperLeft);
            TopAnchored((RectTransform)details.transform, new Vector2(1000f, 380f), -1310f);

            // Bottom buttons.
            Button close = CreateLabeledButton("CloseButton", panelRect, "CLOSE", 34,
                new Color(0.3f, 0.3f, 0.36f), new Vector2(380f, 120f));
            BottomAnchored((RectTransform)close.transform, new Vector2(-260f, 140f));

            Button expand = CreateLabeledButton("ExpandButton", panelRect, "+5 slots", 28,
                new Color(0.2f, 0.32f, 0.26f), new Vector2(380f, 120f));
            BottomAnchored((RectTransform)expand.transform, new Vector2(260f, 140f));

            var panel = panelImage.gameObject.AddComponent<InventoryPanel>();
            EditorSetupUtility.SetPrivateField(panel, "inventory", inventory);
            EditorSetupUtility.SetPrivateField(panel, "equipment", equipment);
            EditorSetupUtility.SetPrivateField(panel, "itemRegistry", registry);
            EditorSetupUtility.SetPrivateField(panel, "rarityTable", rarityTable);
            EditorSetupUtility.SetPrivateField(panel, "wallet", wallet);
            EditorSetupUtility.SetPrivateField(panel, "slotContainer", slotRect);
            EditorSetupUtility.SetPrivateField(panel, "slotButtonTemplate", slotTemplate);
            EditorSetupUtility.SetPrivateField(panel, "bagContainer", bagRect);
            EditorSetupUtility.SetPrivateField(panel, "bagButtonTemplate", bagTemplate);
            EditorSetupUtility.SetPrivateField(panel, "detailsLabel", details);
            EditorSetupUtility.SetPrivateField(panel, "headerLabel", header);
            EditorSetupUtility.SetPrivateField(panel, "expandButton", expand);
            EditorSetupUtility.SetPrivateField(panel, "expandLabel", expand.GetComponentInChildren<Text>());
            EditorSetupUtility.SetPrivateField(panel, "panelRoot", panelImage.gameObject);
            EditorSetupUtility.SetPrivateField(panel, "playerController", player.GetComponent<PlayerController>());

            // Close is a persistent listener so it survives as scene data, not just runtime code.
            UnityEventTools.AddPersistentListener(close.onClick, new UnityAction(panel.Hide));

            return panel;
        }

        private static void BuildBagHudButton(GameObject hud, InventoryPanel panel)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "BagButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Button button = CreateLabeledButton("BagButton", hud.transform, "BAG", 30,
                new Color(0.2f, 0.24f, 0.32f, 0.9f), new Vector2(200f, 100f));

            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);

            UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(panel.Show));

            // Keep the button beneath the modal panels so it cannot be pressed through them.
            rect.SetSiblingIndex(Mathf.Max(0, hud.transform.childCount - 4));
        }

        // ------------------------------------------------------------------ ui helpers

        private static Button CreateTileButton(string name, Transform parent, int fontSize)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, new Color(0.16f, 0.17f, 0.2f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, "-", fontSize, TextAnchor.MiddleCenter);
            var labelRect = (RectTransform)label.transform;
            EditorSetupUtility.StretchFull(labelRect);
            labelRect.offsetMin = new Vector2(6f, 6f);
            labelRect.offsetMax = new Vector2(-6f, -6f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = fontSize;

            image.gameObject.SetActive(false);
            return button;
        }

        private static Button CreateLabeledButton(string name, Transform parent, string text, int fontSize,
            Color color, Vector2 size)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, color);
            ((RectTransform)image.transform).sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, text, fontSize, TextAnchor.MiddleCenter);
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

        private static void BottomAnchored(RectTransform rect, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        private static void WireDebugOverlay(InventoryManager inventory, CurrencyWallet wallet,
            RewardClaimer claimer, InventoryPanel panel)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "inventory", inventory);
            EditorSetupUtility.SetPrivateField(overlay, "wallet", wallet);
            EditorSetupUtility.SetPrivateField(overlay, "rewardClaimer", claimer);
            EditorSetupUtility.SetPrivateField(overlay, "inventoryPanel", panel);
        }
    }
}
