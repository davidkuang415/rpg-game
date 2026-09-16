using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.Classes;
using RPG.Core.Events;
using RPG.DebugTools;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Loot;
using RPG.Player;
using RPG.Progression;
using RPG.Save;
using RPG.Stages;
using RPG.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 10 setup: death handling (restart the stage, keep everything, do not unlock the
    /// next one) and the local save system.
    ///
    /// Safe to re-run; the failure screen is rebuilt each time.
    /// </summary>
    public static class Phase10SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";

        [MenuItem("RPG/Phase 10/Add Death Handling And Saving", priority = 200)]
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
                Debug.LogError("[Phase 10] Missing Player, HUD or GameSystems. Run the earlier phase tools first.");
                return;
            }

            // The Phase 4 stand-in respawned the player on the spot; the real rule replaces it.
            RemoveLegacyDeathHandler(player);

            StageFailureHandler failure = SetUpFailureHandler(systems);
            StageFailedScreen failedScreen = BuildFailedScreen(hud, player, systems, failure);
            SaveManager saveManager = SetUpSaveManager(systems, player);

            WireGameFlow(systems, saveManager, player, failedScreen);
            WireDebugOverlay(saveManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 10]</b> Death handling and saving installed. " +
                      $"Profile path: {Application.persistentDataPath}/profile.json");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 10] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// PlayerDeathHandler was deleted this phase. Its component (and any missing-script
        /// entry it left behind) is stripped from both the scene object and the prefab.
        /// </summary>
        private static void RemoveLegacyDeathHandler(GameObject player)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);

            const string prefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            if (removed > 0) Debug.Log($"[Phase 10] Removed {removed} missing script reference(s).");
        }

        private static StageFailureHandler SetUpFailureHandler(GameObject systems)
        {
            var handler = EditorSetupUtility.EnsureComponent<StageFailureHandler>(systems);

            EditorSetupUtility.SetPrivateField(handler, "playerReference",
                AssetDatabase.LoadAssetAtPath<PlayerReference>("Assets/_Project/Data/Core/PlayerReference.asset"));
            EditorSetupUtility.SetPrivateField(handler, "stageManager", systems.GetComponent<StageManager>());
            EditorSetupUtility.SetPrivateField(handler, "stageEvents",
                AssetDatabase.LoadAssetAtPath<StageEventChannel>("Assets/_Project/Data/Stages/StageEventChannel.asset"));

            return handler;
        }

        private static SaveManager SetUpSaveManager(GameObject systems, GameObject player)
        {
            var saveManager = EditorSetupUtility.EnsureComponent<SaveManager>(systems);

            EditorSetupUtility.SetPrivateField(saveManager, "playerStats", player.GetComponent<PlayerStats>());
            EditorSetupUtility.SetPrivateField(saveManager, "playerLevel", player.GetComponent<PlayerLevel>());
            EditorSetupUtility.SetPrivateField(saveManager, "equipment", player.GetComponent<EquipmentManager>());
            EditorSetupUtility.SetPrivateField(saveManager, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(saveManager, "inventory", systems.GetComponent<InventoryManager>());

            EditorSetupUtility.SetPrivateField(saveManager, "stageProgress",
                AssetDatabase.LoadAssetAtPath<StageProgressState>("Assets/_Project/Data/Stages/StageProgress.asset"));
            EditorSetupUtility.SetPrivateField(saveManager, "stageEvents",
                AssetDatabase.LoadAssetAtPath<StageEventChannel>("Assets/_Project/Data/Stages/StageEventChannel.asset"));
            EditorSetupUtility.SetPrivateField(saveManager, "classRegistry",
                AssetDatabase.LoadAssetAtPath<ClassRegistry>("Assets/_Project/Data/Classes/ClassRegistry.asset"));
            EditorSetupUtility.SetPrivateField(saveManager, "itemRegistry",
                AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/_Project/Data/Items/ItemRegistry.asset"));

            return saveManager;
        }

        private static void WireGameFlow(GameObject systems, SaveManager saveManager,
            GameObject player, StageFailedScreen failedScreen)
        {
            var flow = systems.GetComponent<GameFlowController>();
            if (flow == null) return;

            EditorSetupUtility.SetPrivateField(flow, "saveManager", saveManager);
            EditorSetupUtility.SetPrivateField(flow, "playerStats", player.GetComponent<PlayerStats>());
            EditorSetupUtility.SetPrivateField(flow, "stageFailedScreen", failedScreen);

            // The flow now decides which screen opens first, because a returning player with a
            // saved class should land on the stage list instead of picking a class again.
            GameObject hud = GameObject.Find("HUD");
            Transform classPanel = hud != null
                ? EditorSetupUtility.FindChild(hud.transform, "ClassSelectPanel")
                : null;

            if (classPanel != null)
            {
                var panel = classPanel.GetComponent<ClassSelectionPanel>();
                if (panel != null) EditorSetupUtility.SetPrivateField(panel, "showOnStart", false);
            }
        }

        // ------------------------------------------------------------------ failure screen

        private static StageFailedScreen BuildFailedScreen(GameObject hud, GameObject player,
            GameObject systems, StageFailureHandler failureHandler)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "StageFailedPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Image panelImage = EditorSetupUtility.CreateUiImage("StageFailedPanel", hud.transform,
                null, new Color(0.10f, 0.03f, 0.05f, 0.96f));
            var panelRect = (RectTransform)panelImage.transform;
            EditorSetupUtility.StretchFull(panelRect);
            panelRect.SetAsLastSibling();

            Text title = EditorSetupUtility.CreateUiText("Title", panelRect, "DEFEATED", 58,
                TextAnchor.MiddleCenter);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(1000f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -420f);
            title.color = new Color(1f, 0.5f, 0.5f);

            Text summary = EditorSetupUtility.CreateUiText("Summary", panelRect,
                "Your progress is safe.", 30, TextAnchor.UpperCenter);
            var summaryRect = (RectTransform)summary.transform;
            summaryRect.anchorMin = summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
            summaryRect.sizeDelta = new Vector2(880f, 320f);
            summaryRect.anchoredPosition = new Vector2(0f, 60f);

            Button retry = CreateButton("RetryButton", panelRect, "RETRY STAGE", 36,
                new Color(0.22f, 0.4f, 0.3f), new Vector2(520f, 130f), new Vector2(0f, 320f));
            Button leave = CreateButton("LeaveButton", panelRect, "LEAVE", 34,
                new Color(0.3f, 0.24f, 0.26f), new Vector2(520f, 120f), new Vector2(0f, 160f));

            var screen = panelImage.gameObject.AddComponent<StageFailedScreen>();
            EditorSetupUtility.SetPrivateField(screen, "failureHandler", failureHandler);
            EditorSetupUtility.SetPrivateField(screen, "collector", systems.GetComponent<StageRewardCollector>());
            EditorSetupUtility.SetPrivateField(screen, "titleLabel", title);
            EditorSetupUtility.SetPrivateField(screen, "summaryLabel", summary);
            EditorSetupUtility.SetPrivateField(screen, "retryButton", retry);
            EditorSetupUtility.SetPrivateField(screen, "leaveButton", leave);
            EditorSetupUtility.SetPrivateField(screen, "panelRoot", panelImage.gameObject);
            EditorSetupUtility.SetPrivateField(screen, "playerController", player.GetComponent<PlayerController>());

            return screen;
        }

        private static Button CreateButton(string name, Transform parent, string text, int fontSize,
            Color color, Vector2 size, Vector2 anchoredPosition)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, color);
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, text, fontSize,
                TextAnchor.MiddleCenter);
            EditorSetupUtility.StretchFull((RectTransform)label.transform);

            return button;
        }

        private static void WireDebugOverlay(SaveManager saveManager)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "saveManager", saveManager);
        }
    }
}
