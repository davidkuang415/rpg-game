using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.CameraSystem;
using RPG.DebugTools;
using RPG.Player;
using RPG.Player.Input;
using RPG.UI.Controls;

namespace RPG.EditorTools
{
    /// <summary>
    /// One-click scaffolder for the Phase 1 test scene.
    ///
    /// This exists because every object it creates is something you would otherwise place by
    /// hand: arena, walls, player, camera, HUD. Nothing here is hidden or magic - after it
    /// runs, select any object and you will see exactly the components and Inspector values
    /// described in the setup instructions. Edit them freely; this tool never runs again
    /// unless you ask it to.
    ///
    /// Editor-only: it lives in an /Editor/ folder, so it is stripped from real builds.
    /// </summary>
    public static class Phase1SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ArtFolder = "Assets/_Project/Art/Placeholder";
        private const string SquarePath = ArtFolder + "/Square.png";
        private const string CirclePath = ArtFolder + "/Circle.png";
        private const string RingPath = ArtFolder + "/Ring.png";
        private const string InputChannelPath = "Assets/_Project/Data/Input/PlayerInputChannel.asset";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        // Layer indices. 0-7 are reserved by Unity, so the project's layers start at 8.
        private static readonly (int index, string name)[] RequiredLayers =
        {
            (8,  Core.GameLayers.Player),
            (9,  Core.GameLayers.Enemy),
            (10, Core.GameLayers.PlayerProjectile),
            (11, Core.GameLayers.EnemyProjectile),
            (12, Core.GameLayers.Wall),
            (13, Core.GameLayers.Hazard),
            (14, Core.GameLayers.PickupVisual),
            (15, Core.GameLayers.Environment),
        };

        [MenuItem("RPG/Phase 1/Build Test Scene", priority = 0)]
        public static void BuildTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Rebuild Test Scene?",
                    $"{ScenePath} already exists and will be overwritten.\n\n" +
                    "Any changes you made to that scene will be lost.",
                    "Overwrite", "Cancel"))
            {
                return;
            }

            EnsureLayers();
            EnsureFolders();

            // Create the empty scene FIRST. EditorSceneManager.NewScene unloads assets that
            // nothing references yet, which would destroy freshly created assets behind our
            // back and silently write null references into the scene.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite square = EnsureSprite(SquarePath, SpriteShape.Square, 64);
            Sprite circle = EnsureSprite(CirclePath, SpriteShape.Circle, 128);
            Sprite ring = EnsureSprite(RingPath, SpriteShape.Ring, 256);
            PlayerInputChannel channel = EnsureInputChannel();

            BuildArena(square);
            GameObject player = BuildPlayer(circle, square, channel);
            BuildCamera();
            BuildHud(circle, ring, channel);
            BuildDevTools(channel);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));

            Debug.Log("<b>[Phase 1]</b> Test scene built at " + ScenePath +
                      ". Press Play, then drag on the LEFT half of the Game view to move " +
                      "(or use WASD). Tap the red button on the right to attack.");
        }

        [MenuItem("RPG/Phase 1/Create Project Layers Only", priority = 20)]
        public static void EnsureLayersMenu() => EnsureLayers();

        // ------------------------------------------------------------------ project settings

        /// <summary>
        /// Writes the project's physics layers through the TagManager asset. Doing this via
        /// SerializedObject is the supported way to edit Project Settings while the Editor
        /// is open - hand-editing ProjectSettings/TagManager.asset would get overwritten.
        /// </summary>
        private static void EnsureLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Phase 1] Could not open TagManager.asset. Add the layers manually.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            int added = 0;

            foreach ((int index, string name) in RequiredLayers)
            {
                if (index >= layers.arraySize) continue;

                SerializedProperty slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name) continue;

                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    Debug.LogWarning($"[Phase 1] Layer {index} already holds '{slot.stringValue}'; " +
                                     $"expected to write '{name}'. Skipping to avoid clobbering it.");
                    continue;
                }

                slot.stringValue = name;
                added++;
            }

            if (added > 0)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log($"[Phase 1] Added {added} physics layer(s).");
            }
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[]
                     {
                         ArtFolder,
                         "Assets/_Project/Data/Input",
                         "Assets/_Project/Prefabs/Player",
                         "Assets/_Project/Scenes"
                     })
            {
                Directory.CreateDirectory(folder);
            }
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ placeholder art

        private enum SpriteShape { Square, Circle, Ring }

        /// <summary>
        /// Generates a white placeholder PNG and imports it as a sprite whose Pixels Per Unit
        /// equals its pixel size, so the sprite is exactly 1x1 world unit. That makes every
        /// Transform scale in the scene readable as "size in metres".
        /// </summary>
        private static Sprite EnsureSprite(string path, SpriteShape shape, int size)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float outer = half - 1f;
            float inner = outer * 0.82f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha;
                    switch (shape)
                    {
                        case SpriteShape.Circle:
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                            alpha = Mathf.Clamp01(outer - d);          // 1px antialiased edge
                            break;
                        }
                        case SpriteShape.Ring:
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half));
                            alpha = Mathf.Min(Mathf.Clamp01(outer - d), Mathf.Clamp01(d - inner));
                            break;
                        }
                        default:
                            alpha = 1f;
                            break;
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static PlayerInputChannel EnsureInputChannel()
        {
            var channel = AssetDatabase.LoadAssetAtPath<PlayerInputChannel>(InputChannelPath);
            if (channel != null) return channel;

            channel = ScriptableObject.CreateInstance<PlayerInputChannel>();
            AssetDatabase.CreateAsset(channel, InputChannelPath);
            AssetDatabase.SaveAssets();

            // Re-load from disk rather than reusing the in-memory instance, so the reference
            // is one the AssetDatabase owns and keeps alive.
            return AssetDatabase.LoadAssetAtPath<PlayerInputChannel>(InputChannelPath);
        }

        // ------------------------------------------------------------------ scene contents

        private static void BuildArena(Sprite square)
        {
            var arena = new GameObject("Arena");

            GameObject floor = CreateSprite("Floor", arena.transform, square,
                new Color(0.13f, 0.14f, 0.17f), sortingOrder: -10);
            floor.transform.localScale = new Vector3(50f, 50f, 1f);

            var walls = new GameObject("Walls");
            walls.transform.SetParent(arena.transform);

            // Border walls, then two interior walls. Wall_Inner_A is the sight blocker that
            // Phase 3/4 will use to prove arrows and enemy line-of-sight respect geometry.
            CreateWall(walls.transform, square, "Wall_Top", new Vector2(0f, 25f), new Vector2(50f, 1f));
            CreateWall(walls.transform, square, "Wall_Bottom", new Vector2(0f, -25f), new Vector2(50f, 1f));
            CreateWall(walls.transform, square, "Wall_Left", new Vector2(-25f, 0f), new Vector2(1f, 50f));
            CreateWall(walls.transform, square, "Wall_Right", new Vector2(25f, 0f), new Vector2(1f, 50f));
            CreateWall(walls.transform, square, "Wall_Inner_A", new Vector2(6f, 3f), new Vector2(1f, 12f));
            CreateWall(walls.transform, square, "Wall_Inner_B", new Vector2(-8f, -6f), new Vector2(14f, 1f));
        }

        private static void CreateWall(Transform parent, Sprite square, string name, Vector2 position, Vector2 size)
        {
            GameObject wall = CreateSprite(name, parent, square, new Color(0.45f, 0.47f, 0.52f), sortingOrder: 0);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            wall.layer = LayerOrDefault(Core.GameLayers.Wall);
            wall.AddComponent<BoxCollider2D>();
        }

        private static GameObject BuildPlayer(Sprite circle, Sprite square, PlayerInputChannel channel)
        {
            GameObject player = CreateSprite("Player", null, circle,
                new Color(0.35f, 0.75f, 1f), sortingOrder: 1);
            player.tag = "Player";
            player.layer = LayerOrDefault(Core.GameLayers.Player);
            player.transform.position = Vector3.zero;
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            player.AddComponent<CircleCollider2D>().radius = 0.5f;

            // Aim pivot rotates around the player's centre; the indicator is offset inside it,
            // so the whole arrow swings to match the facing direction.
            var aimPivot = new GameObject("AimPivot");
            aimPivot.transform.SetParent(player.transform, false);

            GameObject indicator = CreateSprite("AimIndicator", aimPivot.transform, square,
                new Color(1f, 0.85f, 0.25f), sortingOrder: 2);
            indicator.transform.localPosition = new Vector3(0.62f, 0f, 0f);
            indicator.transform.localScale = new Vector3(0.6f, 0.16f, 1f);

            player.AddComponent<PlayerMotor>();
            var facing = player.AddComponent<PlayerFacing>();
            var controller = player.AddComponent<PlayerController>();

            SetPrivateField(facing, "aimPivot", aimPivot.transform);
            SetPrivateField(controller, "inputChannel", channel);

            Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath));
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);

            return player;
        }

        private static void BuildCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();

            var follow = cameraObject.AddComponent<CameraFollow2D>();
            SetPrivateField(follow, "visibleWorldHeight", 12f);
            SetPrivateField(follow, "useBounds", true);
            SetPrivateField(follow, "boundsSize", new Vector2(50f, 50f));
        }

        private static void BuildHud(Sprite circle, Sprite ring, PlayerInputChannel channel)
        {
            var canvasObject = new GameObject("HUD",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // --- Left half of the screen: the joystick's touch area ---
            GameObject area = CreateUiImage("MoveInputArea", canvasObject.transform, null, Color.clear);
            RectTransform areaRect = area.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = new Vector2(0.5f, 1f);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;

            GameObject background = CreateUiImage("JoystickBackground", area.transform, ring,
                new Color(1f, 1f, 1f, 0.35f));
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(300f, 300f);
            backgroundRect.anchoredPosition = Vector2.zero;

            GameObject handle = CreateUiImage("JoystickHandle", background.transform, circle,
                new Color(1f, 1f, 1f, 0.55f));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(120f, 120f);
            handleRect.anchoredPosition = Vector2.zero;

            var joystick = area.AddComponent<VirtualJoystick>();
            SetPrivateField(joystick, "inputChannel", channel);
            SetPrivateField(joystick, "background", backgroundRect);
            SetPrivateField(joystick, "handle", handleRect);

            // --- Bottom right: the attack button ---
            GameObject attack = CreateUiImage("AttackButton", canvasObject.transform, circle,
                new Color(0.9f, 0.3f, 0.3f, 0.75f));
            RectTransform attackRect = attack.GetComponent<RectTransform>();
            attackRect.anchorMin = attackRect.anchorMax = new Vector2(1f, 0f);
            attackRect.pivot = new Vector2(0.5f, 0.5f);
            attackRect.sizeDelta = new Vector2(220f, 220f);
            attackRect.anchoredPosition = new Vector2(-180f, 260f);

            var button = attack.AddComponent<TouchActionButton>();
            SetPrivateField(button, "inputChannel", channel);

            // uGUI needs an EventSystem in the scene or no touch/click is ever delivered.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private static void BuildDevTools(PlayerInputChannel channel)
        {
            var devTools = new GameObject("DevTools");
            var keyboard = devTools.AddComponent<KeyboardInputWriter>();
            SetPrivateField(keyboard, "inputChannel", channel);
            devTools.AddComponent<BootstrapValidator>();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Safe layer lookup: warns and falls back to Default if a layer is missing.</summary>
        private static int LayerOrDefault(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) return layer;

            Debug.LogWarning($"[Phase 1] Layer '{layerName}' does not exist yet; using Default. " +
                             "Run RPG > Phase 1 > Create Project Layers Only, then rebuild the scene.");
            return 0;
        }

        private static GameObject CreateSprite(string name, Transform parent, Sprite sprite,
            Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private static GameObject CreateUiImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;   // A fully transparent Image still receives touches.
            return go;
        }

        /// <summary>Assigns a private [SerializeField] field. See EditorSetupUtility.</summary>
        private static void SetPrivateField(Object target, string fieldName, object value)
            => EditorSetupUtility.SetPrivateField(target, fieldName, value);

        private static void AddSceneToBuildSettings()
        {
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == ScenePath) return;
            }

            var scenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length + 1];
            EditorBuildSettings.scenes.CopyTo(scenes, 0);
            scenes[^1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }
    }
}
