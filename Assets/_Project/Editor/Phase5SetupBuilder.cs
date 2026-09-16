using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Core;
using RPG.Core.Events;
using RPG.DebugTools;
using RPG.Enemies;
using RPG.Progression;
using RPG.Vfx;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 5 setup: XP curve and level growth assets, the PlayerLevel component, and the
    /// XP mote burst that plays on every kill.
    ///
    /// Safe to re-run. Curve assets are reused so tuning survives.
    /// </summary>
    public static class Phase5SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ProgressionFolder = "Assets/_Project/Data/Progression";
        private const string XpCurvePath = ProgressionFolder + "/XpCurve.asset";
        private const string LevelGrowthPath = ProgressionFolder + "/LevelGrowth.asset";
        private const string ParticlePrefabPath = "Assets/_Project/Prefabs/Combat/XpParticle.prefab";
        private const string CircleSpritePath = "Assets/_Project/Art/Placeholder/Circle.png";
        private const string PlayerReferencePath = "Assets/_Project/Data/Core/PlayerReference.asset";
        private const string EventChannelPath = "Assets/_Project/Data/Enemies/EnemyEventChannel.asset";

        [MenuItem("RPG/Phase 5/Add XP And Leveling", priority = 100)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Phase 5] No 'Player' in the scene. Run the earlier phase tools first.");
                return;
            }

            var xpCurve = EditorSetupUtility.CreateOrLoadAsset<XpCurveData>(XpCurvePath, out _);

            var levelGrowth = EditorSetupUtility.CreateOrLoadAsset<LevelGrowthData>(
                LevelGrowthPath, out bool growthCreated);
            if (growthCreated)
            {
                levelGrowth.ResetToDefaults();
                EditorUtility.SetDirty(levelGrowth);
            }

            var playerReference = AssetDatabase.LoadAssetAtPath<PlayerReference>(PlayerReferencePath);
            var enemyEvents = AssetDatabase.LoadAssetAtPath<EnemyEventChannel>(EventChannelPath);

            if (playerReference == null || enemyEvents == null)
            {
                Debug.LogError("[Phase 5] Missing PlayerReference or EnemyEventChannel. Run the Phase 4 tool first.");
                return;
            }

            PlayerLevel playerLevel = SetupPlayerLevel(player, xpCurve, levelGrowth);
            XpParticlePool particlePool = EnsureParticlePool();
            SetupGameSystems(enemyEvents, playerLevel, particlePool, playerReference);
            WireDebugOverlay(playerLevel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 5]</b> XP and leveling installed. Kill enemies and watch the motes fly in. " +
                      "Level 1 -> 2 costs 60 XP; each level grants +8 Max HP and +1.5 ATK.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 5] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static PlayerLevel SetupPlayerLevel(GameObject player, XpCurveData curve,
            LevelGrowthData growth)
        {
            var playerLevel = EditorSetupUtility.EnsureComponent<PlayerLevel>(player);
            EditorSetupUtility.SetPrivateField(playerLevel, "xpCurve", curve);
            EditorSetupUtility.SetPrivateField(playerLevel, "levelGrowth", growth);
            return playerLevel;
        }

        private static XpParticlePool EnsureParticlePool()
        {
            XpParticle prefab = EnsureParticlePrefab();

            GameObject poolObject = GameObject.Find("XpParticlePool");
            if (poolObject == null) poolObject = new GameObject("XpParticlePool");

            var pool = EditorSetupUtility.EnsureComponent<XpParticlePool>(poolObject);
            EditorSetupUtility.SetPrivateField(pool, "prefab", prefab);
            EditorSetupUtility.SetPrivateField(pool, "prewarmCount", 24);
            EditorSetupUtility.SetPrivateField(pool, "maxPoolSize", 96);
            return pool;
        }

        private static XpParticle EnsureParticlePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ParticlePrefabPath);
            if (existing != null) return existing.GetComponent<XpParticle>();

            Directory.CreateDirectory(Path.GetDirectoryName(ParticlePrefabPath));

            var particle = new GameObject("XpParticle");
            particle.layer = LayerMask.NameToLayer(GameLayers.PickupVisual);
            particle.transform.localScale = Vector3.one * 0.28f;

            var renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
            renderer.color = new Color(0.35f, 0.95f, 1f);
            renderer.sortingOrder = 5;

            particle.AddComponent<XpParticle>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(particle, ParticlePrefabPath);
            Object.DestroyImmediate(particle);
            return saved.GetComponent<XpParticle>();
        }

        private static void SetupGameSystems(EnemyEventChannel enemyEvents, PlayerLevel playerLevel,
            XpParticlePool particlePool, PlayerReference playerReference)
        {
            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null) systems = new GameObject("GameSystems");

            var collector = EditorSetupUtility.EnsureComponent<ExperienceCollector>(systems);
            EditorSetupUtility.SetPrivateField(collector, "enemyEvents", enemyEvents);
            EditorSetupUtility.SetPrivateField(collector, "playerLevel", playerLevel);

            var spawner = EditorSetupUtility.EnsureComponent<XpParticleSpawner>(systems);
            EditorSetupUtility.SetPrivateField(spawner, "enemyEvents", enemyEvents);
            EditorSetupUtility.SetPrivateField(spawner, "particlePool", particlePool);
            EditorSetupUtility.SetPrivateField(spawner, "playerReference", playerReference);
        }

        private static void WireDebugOverlay(PlayerLevel playerLevel)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "playerLevel", playerLevel);
        }
    }
}
