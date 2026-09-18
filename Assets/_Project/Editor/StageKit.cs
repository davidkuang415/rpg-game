using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RPG.Core;
using RPG.Enemies;
using RPG.Stages;

namespace RPG.EditorTools
{
    /// <summary>
    /// The pieces every generated stage is built from: floors, walls, rooms, spawn points,
    /// waves and doors, plus the save step.
    ///
    /// Phase 6 had these as private helpers inside its own builder. They are pulled out here
    /// so Phase 12 can build ten stages with the same vocabulary, and so a future phase that
    /// adds a stage kind (a hazard, a breakable) has one place to add it.
    ///
    /// Everything produced is ordinary GameObjects with ordinary Inspector fields, exactly as
    /// if it had been placed by hand with the Level Tools menu - a generated stage can be
    /// opened in Prefab Mode and edited afterwards.
    /// </summary>
    public static class StageKit
    {
        public const string StagePrefabFolder = "Assets/_Project/Prefabs/Stages";

        // Real art, not an alpha mask, so it needs a near-white multiply rather than the dark
        // tint the procedural checker pattern used - a full-color stone tile crushed to 0.16
        // reads as flat black.
        public const string KenneyFloorTilePath = "Assets/_Project/Art/Kenney/Environment/FloorTile_Stone.png";

        // Look of the world. Floors are tiled so movement has something to read against,
        // walls carry a dark edge and a light top so they read as solid blocks rather than
        // flat rectangles.
        private static readonly Color FloorColor = new Color(0.88f, 0.88f, 0.9f);
        private static readonly Color FloorRimColor = new Color(0.10f, 0.11f, 0.14f);
        private static readonly Color WallColor = new Color(0.40f, 0.43f, 0.50f);
        private static readonly Color WallEdgeColor = new Color(0.07f, 0.08f, 0.10f);
        private static readonly Color WallTopColor = new Color(0.56f, 0.59f, 0.66f);
        private static readonly Color DoorColor = new Color(0.92f, 0.68f, 0.28f, 0.9f);

        private const float WallEdgeThickness = 0.14f;
        private const float WallTopThickness = 0.22f;

        private static Sprite _square;
        private static Sprite _floorTile;
        private static int _wallLayer;

        public static void Init()
        {
            _square = PlaceholderArt.Load(PlaceholderArt.SquarePath);
            _floorTile = AssetDatabase.LoadAssetAtPath<Sprite>(KenneyFloorTilePath)
                         ?? PlaceholderArt.Load(PlaceholderArt.FloorTilePath);
            _wallLayer = LayerMask.NameToLayer(GameLayers.Wall);
        }

        // ------------------------------------------------------------------ stage root

        public sealed class Stage
        {
            public GameObject Root;
            public StageController Controller;
            public Transform Environment;
            public Transform PlayerSpawn;
            public readonly List<RoomController> Rooms = new List<RoomController>();
        }

        public static Stage Begin(string name, Vector2 playerSpawn)
        {
            var stage = new Stage { Root = new GameObject(name) };
            stage.Controller = stage.Root.AddComponent<StageController>();
            stage.Environment = Child(stage.Root.transform, "Environment");
            stage.PlayerSpawn = Child(stage.Root.transform, "PlayerSpawn");
            stage.PlayerSpawn.position = playerSpawn;
            return stage;
        }

        /// <summary>Writes the controller's fields and saves the prefab. The temp object is destroyed.</summary>
        public static GameObject Finish(Stage stage, string prefabName, Vector2 boundsSize,
            Vector2 boundsCenter = default)
        {
            var rooms = new List<Object>(stage.Rooms.Count);
            for (int i = 0; i < stage.Rooms.Count; i++) rooms.Add(stage.Rooms[i]);

            EditorSetupUtility.SetPrivateObjectList(stage.Controller, "rooms", rooms);
            EditorSetupUtility.SetPrivateField(stage.Controller, "playerSpawnPoint", stage.PlayerSpawn);
            EditorSetupUtility.SetPrivateField(stage.Controller, "cameraBoundsCenter", boundsCenter);
            EditorSetupUtility.SetPrivateField(stage.Controller, "cameraBoundsSize", boundsSize);

            Directory.CreateDirectory(StagePrefabFolder);
            string path = $"{StagePrefabFolder}/{prefabName}.prefab";

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(stage.Root, path);
            Object.DestroyImmediate(stage.Root);
            return saved;
        }

        // ------------------------------------------------------------------ environment

        /// <summary>A tiled floor with a darker rim just inside the walls.</summary>
        public static void Floor(Stage stage, Vector2 center, Vector2 size)
        {
            // The rim: a plain darker square slightly smaller than the room, drawn under the
            // tiles, which are inset by the same amount. It reads as a shadow at the wall base.
            var rim = new GameObject("FloorRim");
            rim.transform.SetParent(stage.Environment, false);
            rim.transform.localPosition = center;
            rim.transform.localScale = new Vector3(size.x, size.y, 1f);
            var rimRenderer = rim.AddComponent<SpriteRenderer>();
            rimRenderer.sprite = _square;
            rimRenderer.color = FloorRimColor;
            rimRenderer.sortingOrder = -12;

            var floor = new GameObject("Floor");
            floor.transform.SetParent(stage.Environment, false);
            floor.transform.localPosition = center;

            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = _floorTile != null ? _floorTile : _square;
            renderer.color = FloorColor;
            renderer.sortingOrder = -10;

            if (_floorTile != null)
            {
                // Tiled draw mode repeats the one-unit tile across the whole room, so the
                // grid stays the same size no matter how big the arena is.
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size - Vector2.one * 1.4f;
            }
            else
            {
                floor.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }

        public static void Border(Stage stage, Vector2 center, Vector2 size)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            Wall(stage, "Wall_Top", center + new Vector2(0f, halfHeight), new Vector2(size.x + 1f, 1f));
            Wall(stage, "Wall_Bottom", center + new Vector2(0f, -halfHeight), new Vector2(size.x + 1f, 1f));
            Wall(stage, "Wall_Left", center + new Vector2(-halfWidth, 0f), new Vector2(1f, size.y + 1f));
            Wall(stage, "Wall_Right", center + new Vector2(halfWidth, 0f), new Vector2(1f, size.y + 1f));
        }

        /// <summary>A solid block: collider on the wall layer, with an edge and a lit top face.</summary>
        public static GameObject Wall(Stage stage, string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(stage.Environment, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            wall.layer = _wallLayer;

            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = _square;
            renderer.color = WallColor;
            renderer.sortingOrder = 0;

            wall.AddComponent<BoxCollider2D>();

            // Children are scaled in the parent's (stretched) space, so the paddings below are
            // divided by the wall's size to come out as a constant thickness in world units.
            var edge = new GameObject("Edge");
            edge.transform.SetParent(wall.transform, false);
            edge.transform.localScale = new Vector3(
                1f + WallEdgeThickness * 2f / size.x,
                1f + WallEdgeThickness * 2f / size.y, 1f);
            var edgeRenderer = edge.AddComponent<SpriteRenderer>();
            edgeRenderer.sprite = _square;
            edgeRenderer.color = WallEdgeColor;
            edgeRenderer.sortingOrder = -1;

            var top = new GameObject("Top");
            top.transform.SetParent(wall.transform, false);
            float topFraction = Mathf.Min(0.45f, WallTopThickness / size.y);
            top.transform.localScale = new Vector3(1f, topFraction, 1f);
            top.transform.localPosition = new Vector3(0f, 0.5f - topFraction * 0.5f, 0f);
            var topRenderer = top.AddComponent<SpriteRenderer>();
            topRenderer.sprite = _square;
            topRenderer.color = WallTopColor;
            topRenderer.sortingOrder = 1;

            return wall;
        }

        // ------------------------------------------------------------------ rooms

        public static RoomController Room(Stage stage, string name, bool isStart = false, bool isFinal = false)
        {
            var roomObject = new GameObject(name);
            roomObject.transform.SetParent(stage.Root.transform, false);

            var room = roomObject.AddComponent<RoomController>();
            EditorSetupUtility.SetPrivateField(room, "isStartRoom", isStart);
            EditorSetupUtility.SetPrivateField(room, "isFinalRoom", isFinal);

            stage.Rooms.Add(room);
            return room;
        }

        public static SpawnPoint Spawn(RoomController room, string name, Vector2 position,
            EnemyData enemyData, int levelOffset = 0)
        {
            var point = new GameObject(name);
            point.transform.SetParent(room.transform, false);
            point.transform.localPosition = position;

            var spawnPoint = point.AddComponent<SpawnPoint>();
            EditorSetupUtility.SetPrivateField(spawnPoint, "enemyData", enemyData);
            EditorSetupUtility.SetPrivateField(spawnPoint, "levelOffset", levelOffset);
            return spawnPoint;
        }

        public static RoomController.Wave Wave(string name, float delay, params SpawnPoint[] points)
            => new RoomController.Wave
            {
                Name = name,
                DelayBeforeSpawn = delay,
                SpawnInterval = 0.15f,
                RequiredToClear = true,
                SpawnPoints = new List<SpawnPoint>(points)
            };

        /// <summary>
        /// Writes the wave list through SerializedObject. Waves are a nested serializable class,
        /// so each field is set explicitly rather than assigning the C# objects directly.
        /// </summary>
        public static void Waves(RoomController room, params RoomController.Wave[] waves)
        {
            var serialized = new SerializedObject(room);
            SerializedProperty waveList = serialized.FindProperty("waves");
            waveList.ClearArray();

            for (int i = 0; i < waves.Length; i++)
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

        /// <summary>
        /// A door leading OUT of <paramref name="from"/> INTO <paramref name="to"/>. It is
        /// registered on the source room's exits, so clearing that room unlocks it.
        /// </summary>
        public static RoomExit Door(RoomController from, RoomController to, string name,
            Vector2 position, Vector2 size)
        {
            var door = new GameObject(name);
            door.transform.SetParent(from.transform, false);
            door.transform.localPosition = position;
            door.transform.localScale = new Vector3(size.x, size.y, 1f);

            // The trigger that detects the player walking through.
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            // The solid bar that blocks the doorway while locked.
            var blockerObject = new GameObject("Blocker");
            blockerObject.transform.SetParent(door.transform, false);
            blockerObject.layer = _wallLayer;

            var blockerRenderer = blockerObject.AddComponent<SpriteRenderer>();
            blockerRenderer.sprite = _square;
            blockerRenderer.color = DoorColor;
            blockerRenderer.sortingOrder = 1;

            Collider2D blocker = blockerObject.AddComponent<BoxCollider2D>();

            var exit = door.AddComponent<RoomExit>();
            EditorSetupUtility.SetPrivateField(exit, "roomToActivate", to);
            EditorSetupUtility.SetPrivateField(exit, "blocker", blocker);
            EditorSetupUtility.SetPrivateField(exit, "lockedVisual", blockerObject);
            EditorSetupUtility.SetPrivateField(exit, "playerLayers", LayerMask.GetMask(GameLayers.Player));

            // Append to the room's exits, keeping any already registered.
            var serialized = new SerializedObject(from);
            SerializedProperty exits = serialized.FindProperty("exits");
            int index = exits.arraySize;
            exits.InsertArrayElementAtIndex(index);
            exits.GetArrayElementAtIndex(index).objectReferenceValue = exit;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return exit;
        }

        // ------------------------------------------------------------------ helpers

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }
    }
}
