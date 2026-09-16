using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Combat.Projectiles;
using RPG.Core;
using RPG.Core.Combat;
using RPG.DebugTools;
using RPG.Player;
using RPG.Player.Combat;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 3 setup: installs the real combat components on the Player, creates the pooled
    /// arrow prefab, and drops training dummies into the test arena.
    ///
    /// Safe to re-run. Dummies are rebuilt each time; the arrow prefab is reused if present.
    /// </summary>
    public static class Phase3SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ArrowPrefabPath = "Assets/_Project/Prefabs/Combat/Arrow.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";
        private const string CircleSpritePath = "Assets/_Project/Art/Placeholder/Circle.png";
        private const string DummyRootName = "TrainingDummies";

        [MenuItem("RPG/Phase 3/Add Combat To Scene", priority = 60)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Phase 3] No 'Player' in the scene. Run the Phase 1 and 2 tools first.");
                return;
            }

            CleanUpDeletedScripts(player);

            Projectile arrowPrefab = EnsureArrowPrefab();
            ProjectilePool pool = EnsureArrowPool(arrowPrefab);

            SetupPlayerCombat(player, pool);
            BuildTrainingDummies();
            WireDebugOverlay(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player;
            Debug.Log("<b>[Phase 3]</b> Combat installed. Play, pick a class, and attack the dummies. " +
                      "The dummy behind the inner wall should be unreachable until you walk around it.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 3] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// PlaceholderAttack was deleted this phase, which leaves a "missing script" entry on
        /// anything that still references it. Strip those from both the scene object and the
        /// prefab asset so the Inspector stays clean.
        /// </summary>
        private static void CleanUpDeletedScripts(GameObject scenePlayer)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(scenePlayer);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            if (removed > 0) Debug.Log($"[Phase 3] Removed {removed} missing script reference(s).");
        }

        // ------------------------------------------------------------------ arrow

        private static Projectile EnsureArrowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            if (existing != null) return existing.GetComponent<Projectile>();

            Directory.CreateDirectory(Path.GetDirectoryName(ArrowPrefabPath));

            var arrow = new GameObject("Arrow");
            arrow.layer = LayerMask.NameToLayer(GameLayers.PlayerProjectile);

            var renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            renderer.color = new Color(1f, 0.95f, 0.6f);
            renderer.sortingOrder = 3;
            arrow.transform.localScale = new Vector3(0.45f, 0.1f, 1f);

            // No collider and no Rigidbody: the projectile sweeps with a CircleCast instead,
            // which cannot tunnel through thin walls.
            arrow.AddComponent<Projectile>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(arrow, ArrowPrefabPath);
            Object.DestroyImmediate(arrow);

            return saved.GetComponent<Projectile>();
        }

        private static ProjectilePool EnsureArrowPool(Projectile arrowPrefab)
        {
            GameObject poolObject = GameObject.Find("ArrowPool");
            if (poolObject == null) poolObject = new GameObject("ArrowPool");

            ProjectilePool pool = EditorSetupUtility.EnsureComponent<ProjectilePool>(poolObject);
            EditorSetupUtility.SetPrivateField(pool, "prefab", arrowPrefab);
            return pool;
        }

        // ------------------------------------------------------------------ player

        private static void SetupPlayerCombat(GameObject player, ProjectilePool pool)
        {
            int enemyMask = LayerMask.GetMask(GameLayers.Enemy);
            int wallMask = LayerMask.GetMask(GameLayers.Wall);

            EditorSetupUtility.EnsureComponent<Health>(player);

            var knight = EditorSetupUtility.EnsureComponent<KnightSwordAttack>(player);
            EditorSetupUtility.SetPrivateField(knight, "targetLayers", enemyMask);
            EditorSetupUtility.SetPrivateField(knight, "blockingLayers", wallMask);

            var archer = EditorSetupUtility.EnsureComponent<ArcherBowAttack>(player);
            EditorSetupUtility.SetPrivateField(archer, "targetLayers", enemyMask);
            EditorSetupUtility.SetPrivateField(archer, "blockingLayers", wallMask);
            EditorSetupUtility.SetPrivateField(archer, "projectilePool", pool);

            // The router is the only IPlayerAttack on the object, so PlayerController binds to it.
            EditorSetupUtility.EnsureComponent<PlayerAttackRouter>(player);
        }

        // ------------------------------------------------------------------ dummies

        private static void BuildTrainingDummies()
        {
            GameObject existing = GameObject.Find(DummyRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(DummyRootName);

            // Deliberately varied: one soft target, one armoured target to show the defense
            // curve, and one behind the inner wall to prove line of sight blocks attacks.
            CreateDummy(root.transform, "Dummy_NoArmor", new Vector2(-3f, 4f), health: 60f, defense: 0f);
            CreateDummy(root.transform, "Dummy_50Def", new Vector2(0f, 6f), health: 60f, defense: 50f);
            CreateDummy(root.transform, "Dummy_BehindWall", new Vector2(9f, 3f), health: 60f, defense: 0f);
        }

        private static void CreateDummy(Transform parent, string name, Vector2 position,
            float health, float defense)
        {
            var dummy = new GameObject(name);
            dummy.transform.SetParent(parent);
            dummy.transform.position = position;
            dummy.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            dummy.layer = LayerMask.NameToLayer(GameLayers.Enemy);

            var renderer = dummy.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            renderer.color = defense > 0f ? new Color(0.85f, 0.6f, 0.3f) : new Color(0.9f, 0.35f, 0.35f);
            renderer.sortingOrder = 1;

            dummy.AddComponent<CircleCollider2D>().radius = 0.5f;

            var provider = dummy.AddComponent<SimpleStatProvider>();
            provider.Configure(health, defense);

            dummy.AddComponent<Health>();
            dummy.AddComponent<HitFlash>();

            var trainingDummy = dummy.AddComponent<TrainingDummy>();
            EditorSetupUtility.SetPrivateField(trainingDummy, "label", name.Replace("Dummy_", ""));
        }

        private static void WireDebugOverlay(GameObject player)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "playerHealth", player.GetComponent<Health>());
        }
    }
}
