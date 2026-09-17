using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.DebugTools;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Loot;
using RPG.Stages;

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

            // The bag and equipment screens are not built here. Since Phase 11 they are pages
            // inside the hub, out of the play area, and the Phase 11 tool owns them.
            WireDebugOverlay(inventory, wallet, claimer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 8]</b> Inventory and equipment installed. Clear a stage, then open " +
                      "the hub and swipe to BAG, then tap an item to equip it.");
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

        private static void WireDebugOverlay(InventoryManager inventory, CurrencyWallet wallet,
            RewardClaimer claimer)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "inventory", inventory);
            EditorSetupUtility.SetPrivateField(overlay, "wallet", wallet);
            EditorSetupUtility.SetPrivateField(overlay, "rewardClaimer", claimer);
        }
    }
}
