using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Combat.Projectiles;
using RPG.Core;
using RPG.Core.Combat;
using RPG.Core.Events;
using RPG.DebugTools;
using RPG.Enemies;
using RPG.Stats;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 4 setup: creates the enemy definitions, builds one prefab per archetype, and
    /// populates the test arena with a fight.
    ///
    /// Safe to re-run. Enemy data assets are reused if present so tuning survives; the enemies
    /// placed in the scene are rebuilt each time.
    /// </summary>
    public static class Phase4SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string CoreDataFolder = "Assets/_Project/Data/Core";
        private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string BoltPrefabPath = "Assets/_Project/Prefabs/Combat/EnemyBolt.prefab";
        private const string CircleSpritePath = "Assets/_Project/Art/Placeholder/Circle.png";
        private const string SquareSpritePath = "Assets/_Project/Art/Placeholder/Square.png";
        private const string StatRulesPath = "Assets/_Project/Data/Stats/StatRules.asset";
        private const string EnemyRootName = "Enemies";

        [MenuItem("RPG/Phase 4/Add Enemies To Scene", priority = 80)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // Open the scene FIRST: OpenScene unloads unreferenced assets, which would destroy
            // anything created before this point (the Phase 1 bug, avoided by ordering).
            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Phase 4] No 'Player' in the scene. Run the earlier phase tools first.");
                return;
            }

            var playerReference = EditorSetupUtility.CreateOrLoadAsset<PlayerReference>(
                CoreDataFolder + "/PlayerReference.asset", out _);
            var eventChannel = EditorSetupUtility.CreateOrLoadAsset<EnemyEventChannel>(
                EnemyDataFolder + "/EnemyEventChannel.asset", out _);
            var difficultyCurve = EditorSetupUtility.CreateOrLoadAsset<DifficultyCurveData>(
                EnemyDataFolder + "/DifficultyCurve.asset", out _);
            var statRules = AssetDatabase.LoadAssetAtPath<StatRules>(StatRulesPath);

            EnemyData melee = CreateEnemyData("MeleeGrunt", "melee_grunt", "Grunt", EnemyArchetype.Melee,
                health: 40f, attack: 8f, defense: 0f, moveSpeed: 2.7f, attackSpeed: 0.9f,
                attackRange: 1.1f, detectionRange: 9f, xp: 10f, gold: 5,
                tint: new Color(0.9f, 0.35f, 0.35f), scale: 0.85f);

            EnemyData ranged = CreateEnemyData("RangedSlinger", "ranged_slinger", "Slinger", EnemyArchetype.Ranged,
                health: 28f, attack: 6f, defense: 0f, moveSpeed: 2.2f, attackSpeed: 0.6f,
                attackRange: 7f, detectionRange: 11f, xp: 12f, gold: 6,
                tint: new Color(0.85f, 0.55f, 0.95f), scale: 0.8f);

            EnemyData tank = CreateEnemyData("TankBrute", "tank_brute", "Brute", EnemyArchetype.Tank,
                health: 120f, attack: 14f, defense: 25f, moveSpeed: 1.7f, attackSpeed: 0.5f,
                attackRange: 1.5f, detectionRange: 8f, xp: 30f, gold: 15,
                tint: new Color(0.55f, 0.45f, 0.75f), scale: 1.25f);

            SetupPlayer(player, playerReference);

            Projectile boltPrefab = EnsureBoltPrefab();
            ProjectilePool boltPool = EnsureBoltPool(boltPrefab);

            GameObject meleePrefab = BuildEnemyPrefab(melee, difficultyCurve, statRules, eventChannel,
                playerReference, isRanged: false);
            GameObject rangedPrefab = BuildEnemyPrefab(ranged, difficultyCurve, statRules, eventChannel,
                playerReference, isRanged: true);
            GameObject tankPrefab = BuildEnemyPrefab(tank, difficultyCurve, statRules, eventChannel,
                playerReference, isRanged: false);

            PopulateArena(meleePrefab, rangedPrefab, tankPrefab, boltPool);
            RemoveTrainingDummies();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 4]</b> Enemies added. Play and fight them. The Slinger behind the " +
                      "inner wall should not shoot until it can actually see you.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 4] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ data

        private static EnemyData CreateEnemyData(string assetName, string id, string displayName,
            EnemyArchetype archetype, float health, float attack, float defense, float moveSpeed,
            float attackSpeed, float attackRange, float detectionRange, float xp, int gold,
            Color tint, float scale)
        {
            EnemyData data = EditorSetupUtility.CreateOrLoadAsset<EnemyData>(
                $"{EnemyDataFolder}/{assetName}.asset", out bool created);

            if (!created) return data;

            EditorSetupUtility.SetPrivateField(data, "enemyId", id);
            EditorSetupUtility.SetPrivateField(data, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(data, "archetype", archetype);
            EditorSetupUtility.SetPrivateField(data, "maxHealth", health);
            EditorSetupUtility.SetPrivateField(data, "attack", attack);
            EditorSetupUtility.SetPrivateField(data, "defense", defense);
            EditorSetupUtility.SetPrivateField(data, "moveSpeed", moveSpeed);
            EditorSetupUtility.SetPrivateField(data, "attackSpeed", attackSpeed);
            EditorSetupUtility.SetPrivateField(data, "attackRange", attackRange);
            EditorSetupUtility.SetPrivateField(data, "detectionRange", detectionRange);
            EditorSetupUtility.SetPrivateField(data, "xpReward", xp);
            EditorSetupUtility.SetPrivateField(data, "goldReward", gold);
            EditorSetupUtility.SetPrivateField(data, "bodyTint", tint);
            EditorSetupUtility.SetPrivateField(data, "bodyScale", scale);

            return data;
        }

        // ------------------------------------------------------------------ player

        private static void SetupPlayer(GameObject player, PlayerReference playerReference)
        {
            var registrar = EditorSetupUtility.EnsureComponent<PlayerRegistrar>(player);
            EditorSetupUtility.SetPrivateField(registrar, "playerReference", playerReference);

            EditorSetupUtility.EnsureComponent<PlayerDeathHandler>(player);
            EditorSetupUtility.EnsureComponent<HitFlash>(player);
        }

        // ------------------------------------------------------------------ projectiles

        private static Projectile EnsureBoltPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefabPath);
            if (existing != null) return existing.GetComponent<Projectile>();

            Directory.CreateDirectory(Path.GetDirectoryName(BoltPrefabPath));

            var bolt = new GameObject("EnemyBolt");
            bolt.layer = LayerMask.NameToLayer(GameLayers.EnemyProjectile);

            var renderer = bolt.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            renderer.color = new Color(1f, 0.45f, 0.85f);
            renderer.sortingOrder = 3;
            bolt.transform.localScale = new Vector3(0.35f, 0.14f, 1f);

            var projectile = bolt.AddComponent<Projectile>();
            EditorSetupUtility.SetPrivateField(projectile, "speed", 9f);   // slower than arrows: dodgeable

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(bolt, BoltPrefabPath);
            Object.DestroyImmediate(bolt);
            return saved.GetComponent<Projectile>();
        }

        private static ProjectilePool EnsureBoltPool(Projectile boltPrefab)
        {
            GameObject poolObject = GameObject.Find("EnemyBoltPool");
            if (poolObject == null) poolObject = new GameObject("EnemyBoltPool");

            ProjectilePool pool = EditorSetupUtility.EnsureComponent<ProjectilePool>(poolObject);
            EditorSetupUtility.SetPrivateField(pool, "prefab", boltPrefab);
            return pool;
        }

        // ------------------------------------------------------------------ enemy prefabs

        private static GameObject BuildEnemyPrefab(EnemyData data, DifficultyCurveData curve,
            StatRules statRules, EnemyEventChannel channel, PlayerReference playerReference, bool isRanged)
        {
            string path = $"{EnemyPrefabFolder}/Enemy_{data.name}.prefab";
            Directory.CreateDirectory(EnemyPrefabFolder);

            int playerMask = LayerMask.GetMask(GameLayers.Player);
            int wallMask = LayerMask.GetMask(GameLayers.Wall);

            var enemy = new GameObject($"Enemy_{data.name}");
            enemy.layer = LayerMask.NameToLayer(GameLayers.Enemy);
            enemy.transform.localScale = Vector3.one * data.BodyScale;

            var renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            renderer.color = data.BodyTint;
            renderer.sortingOrder = 1;

            var body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            enemy.AddComponent<CircleCollider2D>().radius = 0.5f;

            var stats = enemy.AddComponent<EnemyStats>();
            EditorSetupUtility.SetPrivateField(stats, "data", data);
            EditorSetupUtility.SetPrivateField(stats, "difficultyCurve", curve);
            EditorSetupUtility.SetPrivateField(stats, "statRules", statRules);

            enemy.AddComponent<Health>();
            enemy.AddComponent<HitFlash>();

            var perception = enemy.AddComponent<EnemyPerception>();
            EditorSetupUtility.SetPrivateField(perception, "playerReference", playerReference);
            EditorSetupUtility.SetPrivateField(perception, "blockingLayers", wallMask);

            enemy.AddComponent<EnemyMotor>();

            EnemyAttackBase attack = isRanged
                ? enemy.AddComponent<EnemyRangedAttack>()
                : (EnemyAttackBase)enemy.AddComponent<EnemyMeleeAttack>();

            EditorSetupUtility.SetPrivateField(attack, "targetLayers", playerMask);
            EditorSetupUtility.SetPrivateField(attack, "blockingLayers", wallMask);
            EditorSetupUtility.SetPrivateField(attack, "telegraphRenderer", renderer);

            var brain = enemy.AddComponent<EnemyBrain>();
            EditorSetupUtility.SetPrivateField(brain, "blockingLayers", wallMask);

            var controller = enemy.AddComponent<EnemyController>();
            EditorSetupUtility.SetPrivateField(controller, "eventChannel", channel);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(enemy, path);
            Object.DestroyImmediate(enemy);
            return saved;
        }

        // ------------------------------------------------------------------ scene population

        private static void PopulateArena(GameObject meleePrefab, GameObject rangedPrefab,
            GameObject tankPrefab, ProjectilePool boltPool)
        {
            GameObject existing = GameObject.Find(EnemyRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(EnemyRootName);

            Spawn(meleePrefab, root.transform, new Vector2(-6f, 3f), 1, boltPool);
            Spawn(meleePrefab, root.transform, new Vector2(-8f, -2f), 1, boltPool);
            Spawn(meleePrefab, root.transform, new Vector2(-3f, 9f), 3, boltPool);

            // Behind Wall_Inner_A: proves an enemy will not shoot through level geometry.
            Spawn(rangedPrefab, root.transform, new Vector2(9f, 3f), 1, boltPool);
            Spawn(rangedPrefab, root.transform, new Vector2(2f, 11f), 2, boltPool);

            Spawn(tankPrefab, root.transform, new Vector2(0f, -9f), 1, boltPool);
        }

        private static void Spawn(GameObject prefab, Transform parent, Vector2 position, int level,
            ProjectilePool boltPool)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;

            var stats = instance.GetComponent<EnemyStats>();
            EditorSetupUtility.SetPrivateField(stats, "level", level);

            // A prefab asset cannot reference a scene object, so the pool is assigned per instance.
            var ranged = instance.GetComponent<EnemyRangedAttack>();
            if (ranged != null) EditorSetupUtility.SetPrivateField(ranged, "projectilePool", boltPool);
        }

        private static void RemoveTrainingDummies()
        {
            GameObject dummies = GameObject.Find("TrainingDummies");
            if (dummies != null) Object.DestroyImmediate(dummies);
        }
    }
}
