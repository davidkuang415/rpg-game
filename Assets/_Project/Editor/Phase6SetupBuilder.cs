using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Combat.Projectiles;
using RPG.Core;
using RPG.Core.Events;
using RPG.DebugTools;
using RPG.Enemies;
using RPG.Player;
using RPG.Stages;
using RPG.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 6 setup: turns the hand-placed arena into loadable STAGE PREFABS, adds the stage
    /// manager and stage select screen, and builds two example stages - a single-room stage
    /// and a two-room stage with a door and a delayed wave.
    ///
    /// Safe to re-run: stage prefabs and data assets are rebuilt, tuning assets are reused.
    /// </summary>
    public static class Phase6SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string StageDataFolder = "Assets/_Project/Data/Stages";
        private const string StagePrefabFolder = "Assets/_Project/Prefabs/Stages";
        private const string CoreDataFolder = "Assets/_Project/Data/Core";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";

        private static Sprite _square;
        private static int _wallLayer;

        [MenuItem("RPG/Phase 6/Add Stages And Rooms", priority = 120)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            _square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            _wallLayer = LayerMask.NameToLayer(GameLayers.Wall);

            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("HUD");
            if (player == null || hud == null)
            {
                Debug.LogError("[Phase 6] Missing 'Player' or 'HUD'. Run the earlier phase tools first.");
                return;
            }

            var stageEvents = EditorSetupUtility.CreateOrLoadAsset<StageEventChannel>(
                StageDataFolder + "/StageEventChannel.asset", out _);
            var stageProgress = EditorSetupUtility.CreateOrLoadAsset<StageProgressState>(
                StageDataFolder + "/StageProgress.asset", out _);
            var stageRegistry = EditorSetupUtility.CreateOrLoadAsset<StageRegistry>(
                StageDataFolder + "/StageRegistry.asset", out _);
            var playerReference = AssetDatabase.LoadAssetAtPath<PlayerReference>(
                CoreDataFolder + "/PlayerReference.asset");

            ProjectilePoolReference enemyPoolReference = SetUpPoolReferences();
            LinkEnemyPrefabsToData(enemyPoolReference);

            EnemyData grunt = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/MeleeGrunt.asset");
            EnemyData slinger = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/RangedSlinger.asset");
            EnemyData brute = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/TankBrute.asset");

            GameObject stage1Prefab = BuildSingleRoomStage(grunt, slinger);
            GameObject stage2Prefab = BuildTwoRoomStage(grunt, slinger, brute);

            StageData stage1 = CreateStageData("Stage01", 1, "Stage 1", stage1Prefab, enemyLevel: 1);
            StageData stage2 = CreateStageData("Stage02", 2, "Stage 2", stage2Prefab, enemyLevel: 3);
            EditorSetupUtility.SetPrivateObjectList(stageRegistry, "stages",
                new Object[] { stage1, stage2 });

            RemoveLegacySceneContent();

            StageManager stageManager = SetUpStageManager(stageRegistry, stageProgress, stageEvents, playerReference);
            SetUpGameFlow(stageManager, stageEvents, hud);
            WireDebugOverlay(stageManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 6]</b> Stages installed. Play: pick a class, then a stage. " +
                      "the stage list. Stage 2 starts locked until you clear Stage 1.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 6] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ pools and prefabs

        /// <summary>
        /// Spawned enemies come from prefabs, and a prefab cannot reference a scene object, so
        /// each pool now publishes itself into a shared reference asset.
        /// </summary>
        private static ProjectilePoolReference SetUpPoolReferences()
        {
            var playerPoolRef = EditorSetupUtility.CreateOrLoadAsset<ProjectilePoolReference>(
                CoreDataFolder + "/PlayerProjectilePool.asset", out _);
            var enemyPoolRef = EditorSetupUtility.CreateOrLoadAsset<ProjectilePoolReference>(
                CoreDataFolder + "/EnemyProjectilePool.asset", out _);

            GameObject arrowPool = GameObject.Find("ArrowPool");
            if (arrowPool != null)
            {
                var pool = arrowPool.GetComponent<ProjectilePool>();
                if (pool != null) EditorSetupUtility.SetPrivateField(pool, "publishAs", playerPoolRef);
            }

            GameObject boltPool = GameObject.Find("EnemyBoltPool");
            if (boltPool != null)
            {
                var pool = boltPool.GetComponent<ProjectilePool>();
                if (pool != null) EditorSetupUtility.SetPrivateField(pool, "publishAs", enemyPoolRef);
            }

            return enemyPoolRef;
        }

        /// <summary>Points each EnemyData at its prefab, so spawn points only need the data asset.</summary>
        private static void LinkEnemyPrefabsToData(ProjectilePoolReference enemyPoolReference)
        {
            Link("MeleeGrunt", "Enemy_MeleeGrunt");
            Link("RangedSlinger", "Enemy_RangedSlinger");
            Link("TankBrute", "Enemy_TankBrute");

            void Link(string dataName, string prefabName)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDataFolder}/{dataName}.asset");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabFolder}/{prefabName}.prefab");

                if (data == null || prefab == null)
                {
                    Debug.LogWarning($"[Phase 6] Could not link '{dataName}' to '{prefabName}'.");
                    return;
                }

                EditorSetupUtility.SetPrivateField(data, "enemyPrefab", prefab);

                var ranged = prefab.GetComponent<EnemyRangedAttack>();
                if (ranged != null)
                {
                    EditorSetupUtility.SetPrivateField(ranged, "poolReference", enemyPoolReference);
                }
            }
        }

        // ------------------------------------------------------------------ stage building

        private static StageData CreateStageData(string assetName, int number, string displayName,
            GameObject prefab, int enemyLevel)
        {
            StageData stage = EditorSetupUtility.CreateOrLoadAsset<StageData>(
                $"{StageDataFolder}/{assetName}.asset", out _);

            EditorSetupUtility.SetPrivateField(stage, "stageNumber", number);
            EditorSetupUtility.SetPrivateField(stage, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(stage, "stagePrefab", prefab);
            EditorSetupUtility.SetPrivateField(stage, "enemyLevel", enemyLevel);
            return stage;
        }

        private static GameObject BuildSingleRoomStage(EnemyData grunt, EnemyData slinger)
        {
            var root = new GameObject("Stage_01_Arena");
            var controller = root.AddComponent<StageController>();

            Transform environment = CreateChild(root.transform, "Environment");
            CreateFloor(environment, Vector2.zero, new Vector2(50f, 50f));
            CreateBorder(environment, Vector2.zero, new Vector2(50f, 50f));
            CreateWall(environment, "Wall_Inner_A", new Vector2(6f, 3f), new Vector2(1f, 12f));
            CreateWall(environment, "Wall_Inner_B", new Vector2(-8f, -6f), new Vector2(14f, 1f));

            Transform spawn = CreateChild(root.transform, "PlayerSpawn");
            spawn.position = new Vector3(0f, -16f, 0f);

            GameObject roomObject = CreateChild(root.transform, "Room_Main").gameObject;
            var room = roomObject.AddComponent<RoomController>();

            var wave = new List<SpawnPoint>
            {
                CreateSpawnPoint(roomObject.transform, "Spawn_Grunt_1", new Vector2(-6f, 3f), grunt, 0),
                CreateSpawnPoint(roomObject.transform, "Spawn_Grunt_2", new Vector2(-8f, -2f), grunt, 0),
                CreateSpawnPoint(roomObject.transform, "Spawn_Grunt_3", new Vector2(-3f, 9f), grunt, 1),
                CreateSpawnPoint(roomObject.transform, "Spawn_Slinger_1", new Vector2(9f, 3f), slinger, 0),
                CreateSpawnPoint(roomObject.transform, "Spawn_Slinger_2", new Vector2(2f, 11f), slinger, 0)
            };

            ApplyWaves(room, new[] { CreateWave("Wave 1", 0f, wave) });
            EditorSetupUtility.SetPrivateField(room, "isStartRoom", true);
            EditorSetupUtility.SetPrivateField(room, "isFinalRoom", true);

            EditorSetupUtility.SetPrivateObjectList(controller, "rooms", new Object[] { room });
            EditorSetupUtility.SetPrivateField(controller, "playerSpawnPoint", spawn);
            EditorSetupUtility.SetPrivateField(controller, "cameraBoundsSize", new Vector2(50f, 50f));

            return SaveStagePrefab(root, "Stage_01_Arena");
        }

        private static GameObject BuildTwoRoomStage(EnemyData grunt, EnemyData slinger, EnemyData brute)
        {
            var root = new GameObject("Stage_02_TwoRooms");
            var controller = root.AddComponent<StageController>();

            Transform environment = CreateChild(root.transform, "Environment");
            CreateFloor(environment, Vector2.zero, new Vector2(50f, 26f));
            CreateBorder(environment, Vector2.zero, new Vector2(50f, 26f));

            // Dividing wall with a gap in the middle for the door.
            CreateWall(environment, "Divider_Top", new Vector2(0f, 7.5f), new Vector2(1f, 11f));
            CreateWall(environment, "Divider_Bottom", new Vector2(0f, -7.5f), new Vector2(1f, 11f));

            // Cover inside each room, so line of sight matters on both sides.
            CreateWall(environment, "Cover_A", new Vector2(-14f, 2f), new Vector2(1f, 8f));
            CreateWall(environment, "Cover_B", new Vector2(13f, -3f), new Vector2(9f, 1f));

            Transform spawn = CreateChild(root.transform, "PlayerSpawn");
            spawn.position = new Vector3(-20f, 0f, 0f);

            // --- Room A: the room the player starts in ---
            GameObject roomAObject = CreateChild(root.transform, "Room_A").gameObject;
            var roomA = roomAObject.AddComponent<RoomController>();

            var waveA = new List<SpawnPoint>
            {
                CreateSpawnPoint(roomAObject.transform, "A_Grunt_1", new Vector2(-8f, 6f), grunt, 0),
                CreateSpawnPoint(roomAObject.transform, "A_Grunt_2", new Vector2(-8f, -6f), grunt, 0),
                CreateSpawnPoint(roomAObject.transform, "A_Slinger_1", new Vector2(-16f, 8f), slinger, 0)
            };

            ApplyWaves(roomA, new[] { CreateWave("Wave 1", 0f, waveA) });
            EditorSetupUtility.SetPrivateField(roomA, "isStartRoom", true);

            // --- Room B: unlocked by clearing room A, two waves, ends the stage ---
            GameObject roomBObject = CreateChild(root.transform, "Room_B").gameObject;
            var roomB = roomBObject.AddComponent<RoomController>();

            var waveB1 = new List<SpawnPoint>
            {
                CreateSpawnPoint(roomBObject.transform, "B_Slinger_1", new Vector2(10f, 8f), slinger, 0),
                CreateSpawnPoint(roomBObject.transform, "B_Slinger_2", new Vector2(18f, -6f), slinger, 0)
            };

            var waveB2 = new List<SpawnPoint>
            {
                CreateSpawnPoint(roomBObject.transform, "B_Brute", new Vector2(20f, 0f), brute, 0),
                CreateSpawnPoint(roomBObject.transform, "B_Grunt_1", new Vector2(14f, 5f), grunt, 1),
                CreateSpawnPoint(roomBObject.transform, "B_Grunt_2", new Vector2(14f, -5f), grunt, 1)
            };

            ApplyWaves(roomB, new[]
            {
                CreateWave("Wave 1", 0f, waveB1),
                CreateWave("Wave 2 (delayed)", 1.5f, waveB2)
            });
            EditorSetupUtility.SetPrivateField(roomB, "isFinalRoom", true);

            // --- Door from A to B ---
            RoomExit door = CreateDoor(roomAObject.transform, "Door_A_to_B", new Vector2(0f, 0f), roomB);
            EditorSetupUtility.SetPrivateObjectList(roomA, "exits", new Object[] { door });

            EditorSetupUtility.SetPrivateObjectList(controller, "rooms", new Object[] { roomA, roomB });
            EditorSetupUtility.SetPrivateField(controller, "playerSpawnPoint", spawn);
            EditorSetupUtility.SetPrivateField(controller, "cameraBoundsSize", new Vector2(50f, 26f));

            return SaveStagePrefab(root, "Stage_02_TwoRooms");
        }

        private static GameObject SaveStagePrefab(GameObject root, string prefabName)
        {
            Directory.CreateDirectory(StagePrefabFolder);
            string path = $"{StagePrefabFolder}/{prefabName}.prefab";

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ------------------------------------------------------------------ layout helpers

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void CreateFloor(Transform parent, Vector2 center, Vector2 size)
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = center;
            floor.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.color = new Color(0.13f, 0.14f, 0.17f);
            renderer.sortingOrder = -10;
        }

        private static void CreateBorder(Transform parent, Vector2 center, Vector2 size)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            CreateWall(parent, "Wall_Top", center + new Vector2(0f, halfHeight), new Vector2(size.x, 1f));
            CreateWall(parent, "Wall_Bottom", center + new Vector2(0f, -halfHeight), new Vector2(size.x, 1f));
            CreateWall(parent, "Wall_Left", center + new Vector2(-halfWidth, 0f), new Vector2(1f, size.y));
            CreateWall(parent, "Wall_Right", center + new Vector2(halfWidth, 0f), new Vector2(1f, size.y));
        }

        private static void CreateWall(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            wall.layer = _wallLayer;

            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.color = new Color(0.45f, 0.47f, 0.52f);
            renderer.sortingOrder = 0;

            wall.AddComponent<BoxCollider2D>();
        }

        private static SpawnPoint CreateSpawnPoint(Transform parent, string name, Vector2 position,
            EnemyData enemyData, int levelOffset)
        {
            var point = new GameObject(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = position;

            var spawnPoint = point.AddComponent<SpawnPoint>();
            EditorSetupUtility.SetPrivateField(spawnPoint, "enemyData", enemyData);
            EditorSetupUtility.SetPrivateField(spawnPoint, "levelOffset", levelOffset);
            return spawnPoint;
        }

        private static RoomExit CreateDoor(Transform parent, string name, Vector2 position,
            RoomController roomToActivate)
        {
            var door = new GameObject(name);
            door.transform.SetParent(parent, false);
            door.transform.localPosition = position;
            door.transform.localScale = new Vector3(3f, 4f, 1f);

            // The trigger that detects the player walking through.
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            // The solid barrier that blocks the doorway while locked.
            var blockerObject = new GameObject("Blocker");
            blockerObject.transform.SetParent(door.transform, false);
            blockerObject.layer = _wallLayer;

            var blockerRenderer = blockerObject.AddComponent<SpriteRenderer>();
            blockerRenderer.sprite = _square;
            blockerRenderer.color = new Color(0.85f, 0.65f, 0.25f, 0.85f);
            blockerRenderer.sortingOrder = 1;

            Collider2D blocker = blockerObject.AddComponent<BoxCollider2D>();

            var exit = door.AddComponent<RoomExit>();
            EditorSetupUtility.SetPrivateField(exit, "roomToActivate", roomToActivate);
            EditorSetupUtility.SetPrivateField(exit, "blocker", blocker);
            EditorSetupUtility.SetPrivateField(exit, "lockedVisual", blockerObject);
            EditorSetupUtility.SetPrivateField(exit, "playerLayers", LayerMask.GetMask(GameLayers.Player));

            return exit;
        }

        private static RoomController.Wave CreateWave(string name, float delay, List<SpawnPoint> points)
            => new RoomController.Wave
            {
                Name = name,
                DelayBeforeSpawn = delay,
                SpawnInterval = 0.15f,
                RequiredToClear = true,
                SpawnPoints = points
            };

        /// <summary>
        /// Writes the wave list through SerializedObject. Waves are a nested serializable class,
        /// so each field is set explicitly rather than assigning the C# objects directly.
        /// </summary>
        private static void ApplyWaves(RoomController room, IList<RoomController.Wave> waves)
        {
            var serialized = new SerializedObject(room);
            SerializedProperty waveList = serialized.FindProperty("waves");
            waveList.ClearArray();

            for (int i = 0; i < waves.Count; i++)
            {
                waveList.InsertArrayElementAtIndex(i);
                SerializedProperty element = waveList.GetArrayElementAtIndex(i);

                element.FindPropertyRelative("Name").stringValue = waves[i].Name;
                element.FindPropertyRelative("DelayBeforeSpawn").floatValue = waves[i].DelayBeforeSpawn;
                element.FindPropertyRelative("SpawnInterval").floatValue = waves[i].SpawnInterval;
                element.FindPropertyRelative("RequiredToClear").boolValue = waves[i].RequiredToClear;

                SerializedProperty points = element.FindPropertyRelative("SpawnPoints");
                points.ClearArray();

                for (int p = 0; p < waves[i].SpawnPoints.Count; p++)
                {
                    points.InsertArrayElementAtIndex(p);
                    points.GetArrayElementAtIndex(p).objectReferenceValue = waves[i].SpawnPoints[p];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ scene wiring

        /// <summary>The arena and enemies now live inside stage prefabs, not loose in the scene.</summary>
        private static void RemoveLegacySceneContent()
        {
            foreach (string name in new[] { "Arena", "Enemies", "TrainingDummies" })
            {
                GameObject found = GameObject.Find(name);
                if (found != null) Object.DestroyImmediate(found);
            }
        }

        private static StageManager SetUpStageManager(StageRegistry registry, StageProgressState progress,
            StageEventChannel events, PlayerReference playerReference)
        {
            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null) systems = new GameObject("GameSystems");

            GameObject stageRoot = GameObject.Find("StageRoot");
            if (stageRoot == null) stageRoot = new GameObject("StageRoot");

            var manager = EditorSetupUtility.EnsureComponent<StageManager>(systems);
            EditorSetupUtility.SetPrivateField(manager, "registry", registry);
            EditorSetupUtility.SetPrivateField(manager, "progress", progress);
            EditorSetupUtility.SetPrivateField(manager, "stageEvents", events);
            EditorSetupUtility.SetPrivateField(manager, "playerReference", playerReference);
            EditorSetupUtility.SetPrivateField(manager, "stageRoot", stageRoot.transform);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                EditorSetupUtility.SetPrivateField(manager, "cameraFollow",
                    mainCamera.GetComponent<RPG.CameraSystem.CameraFollow2D>());
            }

            return manager;
        }

        /// <summary>
        /// Wires the flow to the systems it sequences. The stage list itself is not built here:
        /// since Phase 11 it is a page inside the hub, and the Phase 11 tool owns it.
        /// </summary>
        private static void SetUpGameFlow(StageManager stageManager,
            StageEventChannel events, GameObject hud)
        {
            GameObject systems = GameObject.Find("GameSystems");
            var flow = EditorSetupUtility.EnsureComponent<GameFlowController>(systems);

            Transform classPanel = EditorSetupUtility.FindChild(hud.transform, "ClassSelectPanel");

            EditorSetupUtility.SetPrivateField(flow, "classSelectionPanel",
                classPanel != null ? classPanel.GetComponent<ClassSelectionPanel>() : null);
            EditorSetupUtility.SetPrivateField(flow, "stageManager", stageManager);
            EditorSetupUtility.SetPrivateField(flow, "stageEvents", events);
        }

        private static void WireDebugOverlay(StageManager stageManager)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "stageManager", stageManager);
        }
    }
}
