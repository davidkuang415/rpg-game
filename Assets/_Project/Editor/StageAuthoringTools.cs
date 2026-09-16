using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RPG.Core;
using RPG.Stages;

namespace RPG.EditorTools
{
    /// <summary>
    /// Menu tools for building stages by hand.
    ///
    /// The design spec asks that creating a level never require editing C#. These commands
    /// cover the whole workflow: make a new stage, then fill it with rooms, spawn points,
    /// walls and doors by selecting a parent and picking a menu item.
    ///
    /// Everything created here is a normal GameObject with normal Inspector fields - nothing
    /// is generated or hidden, so a stage can also be built entirely by hand if preferred.
    /// </summary>
    public static class StageAuthoringTools
    {
        private const string StageDataFolder = "Assets/_Project/Data/Stages";
        private const string StagePrefabFolder = "Assets/_Project/Prefabs/Stages";
        private const string RegistryPath = StageDataFolder + "/StageRegistry.asset";
        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";

        // ------------------------------------------------------------------ new stage

        [MenuItem("RPG/Level Tools/Create New Stage", priority = 300)]
        public static void CreateNewStage()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError("[Level Tools] No StageRegistry found. Run RPG > Phase 6 first.");
                return;
            }

            int stageNumber = registry.HighestStageNumber + 1;
            string prefabPath = $"{StagePrefabFolder}/Stage_{stageNumber:00}.prefab";

            if (File.Exists(prefabPath))
            {
                Debug.LogError($"[Level Tools] {prefabPath} already exists.");
                return;
            }

            Directory.CreateDirectory(StagePrefabFolder);

            // --- Scaffold: everything a stage needs, empty and ready to fill ---
            var root = new GameObject($"Stage_{stageNumber:00}");
            var controller = root.AddComponent<StageController>();

            var environment = new GameObject("Environment");
            environment.transform.SetParent(root.transform, false);

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(root.transform, false);

            var roomObject = new GameObject("Room_Main");
            roomObject.transform.SetParent(root.transform, false);
            var room = roomObject.AddComponent<RoomController>();

            EditorSetupUtility.SetPrivateField(room, "isStartRoom", true);
            EditorSetupUtility.SetPrivateField(room, "isFinalRoom", true);
            EditorSetupUtility.SetPrivateObjectList(controller, "rooms", new Object[] { room });
            EditorSetupUtility.SetPrivateField(controller, "playerSpawnPoint", spawn.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            // --- Matching data asset, registered so it appears on the stage select screen ---
            StageData stage = EditorSetupUtility.CreateOrLoadAsset<StageData>(
                $"{StageDataFolder}/Stage{stageNumber:00}.asset", out _);

            EditorSetupUtility.SetPrivateField(stage, "stageNumber", stageNumber);
            EditorSetupUtility.SetPrivateField(stage, "displayName", $"Stage {stageNumber}");
            EditorSetupUtility.SetPrivateField(stage, "stagePrefab", prefab);
            EditorSetupUtility.SetPrivateField(stage, "enemyLevel", stageNumber);

            var stages = new List<Object>(registry.Stages.Count + 1);
            foreach (StageData existing in registry.Stages)
            {
                if (existing != null) stages.Add(existing);
            }
            stages.Add(stage);
            EditorSetupUtility.SetPrivateObjectList(registry, "stages", stages);

            AssetDatabase.SaveAssets();
            AssetDatabase.OpenAsset(prefab);   // Opens Prefab Mode, ready to build.

            Debug.Log($"<b>[Level Tools]</b> Created Stage {stageNumber} " +
                      $"({prefabPath}). It is now in the registry and opened for editing. " +
                      "Add walls, spawn points and rooms with the other Level Tools commands.");
        }

        // ------------------------------------------------------------------ stage pieces

        [MenuItem("RPG/Level Tools/Add Spawn Point %#s", priority = 320)]
        public static void AddSpawnPoint()
        {
            GameObject parent = RequireSelection("Add Spawn Point");
            if (parent == null) return;

            var point = Create("SpawnPoint", parent);
            point.AddComponent<SpawnPoint>();

            Debug.Log("[Level Tools] Spawn point added. Assign its Enemy Data in the Inspector, " +
                      "then add it to a wave on the room's RoomController.");
        }

        [MenuItem("RPG/Level Tools/Add Wall", priority = 321)]
        public static void AddWall()
        {
            GameObject parent = RequireSelection("Add Wall");
            if (parent == null) return;

            GameObject wall = Create("Wall", parent);
            wall.layer = LayerMask.NameToLayer(GameLayers.Wall);
            wall.transform.localScale = new Vector3(4f, 1f, 1f);

            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            renderer.color = new Color(0.45f, 0.47f, 0.52f);

            wall.AddComponent<BoxCollider2D>();
        }

        [MenuItem("RPG/Level Tools/Add Room", priority = 322)]
        public static void AddRoom()
        {
            GameObject parent = RequireSelection("Add Room");
            if (parent == null) return;

            GameObject roomObject = Create("Room", parent);
            roomObject.AddComponent<RoomController>();

            Debug.Log("[Level Tools] Room added. Remember to add it to the StageController's " +
                      "Rooms list, and mark exactly one room as Is Start Room.");
        }

        [MenuItem("RPG/Level Tools/Add Door", priority = 323)]
        public static void AddDoor()
        {
            GameObject parent = RequireSelection("Add Door");
            if (parent == null) return;

            GameObject door = Create("Door", parent);
            door.transform.localScale = new Vector3(3f, 4f, 1f);

            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            var blockerObject = new GameObject("Blocker");
            blockerObject.transform.SetParent(door.transform, false);
            blockerObject.layer = LayerMask.NameToLayer(GameLayers.Wall);

            var renderer = blockerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            renderer.color = new Color(0.85f, 0.65f, 0.25f, 0.85f);
            renderer.sortingOrder = 1;

            Collider2D blocker = blockerObject.AddComponent<BoxCollider2D>();

            var exit = door.AddComponent<RoomExit>();
            EditorSetupUtility.SetPrivateField(exit, "blocker", blocker);
            EditorSetupUtility.SetPrivateField(exit, "lockedVisual", blockerObject);
            EditorSetupUtility.SetPrivateField(exit, "playerLayers", LayerMask.GetMask(GameLayers.Player));

            Debug.Log("[Level Tools] Door added. Set its Room To Activate, and add the door to " +
                      "the Exits list of the room it leads OUT of.");
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject RequireSelection(string action)
        {
            GameObject selection = Selection.activeGameObject;
            if (selection != null) return selection;

            EditorUtility.DisplayDialog(action,
                "Select the GameObject to parent this under first.\n\n" +
                "Open a stage prefab (double-click it), then select a room or the Environment object.",
                "OK");
            return null;
        }

        /// <summary>
        /// Creates a child at the Scene view's current focus point, registered with Undo so
        /// Ctrl+Z behaves the way it does for anything else in the Editor.
        /// </summary>
        private static GameObject Create(string name, GameObject parent)
        {
            var created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");

            created.transform.SetParent(parent.transform, false);

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Vector3 pivot = view.pivot;
                created.transform.position = new Vector3(pivot.x, pivot.y, 0f);
            }

            Selection.activeGameObject = created;
            EditorSceneManager.MarkSceneDirty(created.scene);
            return created;
        }
    }
}
