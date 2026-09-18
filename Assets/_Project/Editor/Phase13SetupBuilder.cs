using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Core;
using RPG.Economy;
using RPG.Enemies;
using RPG.Loot;
using RPG.Player;
using RPG.Progression;
using RPG.Save;
using RPG.Stages;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 13: wiring for the QA fix pass.
    ///
    /// The fixes themselves are in the scripts; this puts the new pieces into the scene and the
    /// prefabs that need them:
    ///   - an enemy pool, and every spawn point pointed at it
    ///   - aim targeting on the player, so attacks no longer follow the walk direction
    ///   - the first-clear gem bonus, giving gems a second source
    ///   - the save manager pointed at the reward collector, so overflow loot survives a quit
    ///   - escalating loot and gold on the early stages, which were all paying the same
    ///   - Player.prefab brought back in line with the scene object
    ///
    /// Safe to re-run.
    /// </summary>
    public static class Phase13SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string StagePrefabFolder = "Assets/_Project/Prefabs/Stages";
        private const string StageDataFolder = "Assets/_Project/Data/Stages";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [MenuItem("RPG/Phase 13/Apply QA Fix Pass", priority = 260)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject systems = GameObject.Find("GameSystems");
            GameObject player = GameObject.Find("Player");
            if (systems == null || player == null)
            {
                Debug.LogError("[Phase 13] Missing GameSystems or Player. Run the earlier tools first.");
                return;
            }

            EnemyPoolReference poolReference = SetUpEnemyPool();
            PointSpawnPointsAtPool(poolReference);
            SetUpTargeting(player);
            SetUpFirstClearBonus(systems);
            SetUpRewardPersistence(systems);
            FixStageRewardCurves();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            // Last, because it bakes the scene object's current state - including everything
            // wired above - into the prefab.
            ReconcilePlayerPrefab(player);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Verify(player, systems);
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 13] {ScenePath} is missing.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ enemy pooling

        private static EnemyPoolReference SetUpEnemyPool()
        {
            EnemyPoolReference reference = EditorSetupUtility.CreateOrLoadAsset<EnemyPoolReference>(
                EnemyDataFolder + "/EnemyPoolReference.asset", out _);

            GameObject poolObject = GameObject.Find("EnemyPool") ?? new GameObject("EnemyPool");
            var pool = EditorSetupUtility.EnsureComponent<EnemyPool>(poolObject);
            EditorSetupUtility.SetPrivateField(pool, "publishAs", reference);

            return reference;
        }

        /// <summary>
        /// Spawn points live inside stage prefabs, so they are edited at the prefab rather than
        /// in the scene - the stages are not loaded until play.
        /// </summary>
        private static void PointSpawnPointsAtPool(EnemyPoolReference reference)
        {
            if (!Directory.Exists(StagePrefabFolder)) return;

            string[] files = Directory.GetFiles(StagePrefabFolder, "*.prefab");
            int pointsWired = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                SpawnPoint[] points = contents.GetComponentsInChildren<SpawnPoint>(true);

                for (int p = 0; p < points.Length; p++)
                {
                    EditorSetupUtility.SetPrivateField(points[p], "enemyPool", reference);
                    pointsWired++;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log($"[Phase 13] Pointed {pointsWired} spawn point(s) at the enemy pool.");
        }

        // ------------------------------------------------------------------ player

        private static void SetUpTargeting(GameObject player)
        {
            var targeting = EditorSetupUtility.EnsureComponent<PlayerTargeting>(player);

            // A LayerMask field serializes as a plain int, so the mask is written as one.
            EditorSetupUtility.SetPrivateField(targeting, "targetLayers",
                LayerMask.GetMask(GameLayers.Enemy));
            EditorSetupUtility.SetPrivateField(targeting, "blockingLayers",
                LayerMask.GetMask(GameLayers.Wall));

            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                EditorSetupUtility.SetPrivateField(controller, "targeting", targeting);
            }
        }

        /// <summary>
        /// Applies the scene player's overrides back onto Player.prefab.
        ///
        /// The prefab had been frozen since Phase 1 while Health, PlayerStats, EquipmentManager,
        /// PlayerLevel and the whole visual rig existed only as overrides on the scene instance.
        /// That is one right-click ("Revert Prefab Instance") away from deleting the player, and
        /// it already caused one silent bug when a setup tool edited the prefab and found nothing
        /// there to edit.
        /// </summary>
        private static void ReconcilePlayerPrefab(GameObject player)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(player))
            {
                Debug.Log("[Phase 13] Scene Player is not a prefab instance; nothing to reconcile.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogWarning("[Phase 13] Player.prefab is missing; skipping reconcile.");
                return;
            }

            PrefabUtility.ApplyPrefabInstance(player, InteractionMode.AutomatedAction);
            Debug.Log("[Phase 13] Player.prefab now matches the scene player.");
        }

        // ------------------------------------------------------------------ systems

        private static void SetUpFirstClearBonus(GameObject systems)
        {
            var bonus = EditorSetupUtility.EnsureComponent<FirstClearBonus>(systems);

            EditorSetupUtility.SetPrivateField(bonus, "wallet", systems.GetComponent<CurrencyWallet>());
            EditorSetupUtility.SetPrivateField(bonus, "progress",
                AssetDatabase.LoadAssetAtPath<StageProgressState>(StageDataFolder + "/StageProgress.asset"));
            EditorSetupUtility.SetPrivateField(bonus, "stageEvents",
                AssetDatabase.LoadAssetAtPath<StageEventChannel>(StageDataFolder + "/StageEventChannel.asset"));
        }

        private static void SetUpRewardPersistence(GameObject systems)
        {
            var saveManager = systems.GetComponent<SaveManager>();
            var collector = systems.GetComponent<StageRewardCollector>();

            if (saveManager == null || collector == null)
            {
                Debug.LogWarning("[Phase 13] SaveManager or StageRewardCollector missing; " +
                                 "unclaimed rewards will not be saved.");
                return;
            }

            EditorSetupUtility.SetPrivateField(saveManager, "rewardCollector", collector);
        }

        /// <summary>
        /// Stages 1 to 4 all paid exactly the same loot and gold, so replaying an early stage was
        /// strictly worse than pushing forward - and pushing forward was no better paid either.
        /// </summary>
        private static void FixStageRewardCurves()
        {
            string[] files = Directory.Exists(StageDataFolder)
                ? Directory.GetFiles(StageDataFolder, "Stage*.asset")
                : new string[0];

            int fixedCount = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (stage == null) continue;

                int number = Mathf.Max(1, stage.StageNumber);

                EditorSetupUtility.SetPrivateField(stage, "lootModifier", 1f + (number - 1) * 0.08f);
                EditorSetupUtility.SetPrivateField(stage, "goldModifier", 1f + (number - 1) * 0.15f);
                fixedCount++;
            }

            Debug.Log($"[Phase 13] Gave {fixedCount} stage(s) an escalating loot and gold curve.");
        }

        // ------------------------------------------------------------------ verification

        private static void Verify(GameObject player, GameObject systems)
        {
            bool ok = true;

            if (player.GetComponent<PlayerTargeting>() == null)
            {
                Debug.LogError("[Phase 13] VERIFY FAILED: Player has no PlayerTargeting.", player);
                ok = false;
            }

            if (systems.GetComponent<FirstClearBonus>() == null)
            {
                Debug.LogError("[Phase 13] VERIFY FAILED: GameSystems has no FirstClearBonus.", systems);
                ok = false;
            }

            if (Object.FindAnyObjectByType<EnemyPool>() == null)
            {
                Debug.LogError("[Phase 13] VERIFY FAILED: no EnemyPool in the scene.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log("<b>[Phase 13]</b> QA fix pass applied and verified. Hold the attack " +
                          "button to attack continuously; attacks aim at the nearest enemy, so you " +
                          "can retreat while firing.");
            }
        }
    }
}
