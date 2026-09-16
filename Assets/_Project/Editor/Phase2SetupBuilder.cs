using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.Classes;
using RPG.DebugTools;
using RPG.Items;
using RPG.Player;
using RPG.Stats;
using RPG.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 2 setup: creates the class and stat-rule assets, attaches the stat components to
    /// the Player, and adds the class selection screen to the existing test scene.
    ///
    /// Safe to run more than once. Existing assets are reused rather than overwritten, so any
    /// balance values you tune by hand survive a re-run.
    /// </summary>
    public static class Phase2SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ClassFolder = "Assets/_Project/Data/Classes";
        private const string KnightPath = ClassFolder + "/Knight.asset";
        private const string ArcherPath = ClassFolder + "/Archer.asset";
        private const string RegistryPath = ClassFolder + "/ClassRegistry.asset";
        private const string StatRulesPath = "Assets/_Project/Data/Stats/StatRules.asset";

        [MenuItem("RPG/Phase 2/Add Class System To Scene", priority = 40)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            StatRules statRules = CreateStatRules();
            ClassData knight = CreateKnight();
            ClassData archer = CreateArcher();
            ClassRegistry registry = CreateRegistry(knight, archer);

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("HUD");
            GameObject devTools = GameObject.Find("DevTools");

            if (player == null || hud == null)
            {
                Debug.LogError("[Phase 2] Could not find 'Player' and 'HUD' in the scene. " +
                               "Run RPG > Phase 1 > Build Test Scene first.");
                return;
            }

            PlayerStats stats = SetupPlayer(player, knight, statRules);
            ClassSelectionPanel panel = SetupSelectionPanel(hud, registry, stats, player);
            SetupDebugOverlay(devTools, stats, panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player;
            Debug.Log("<b>[Phase 2]</b> Class system installed. Press Play: choose Knight or Archer, " +
                      "then check the stat panel in the top-left.");
        }

        // ------------------------------------------------------------------ assets

        private static StatRules CreateStatRules()
        {
            StatRules rules = EditorSetupUtility.CreateOrLoadAsset<StatRules>(StatRulesPath, out bool created);
            if (created)
            {
                rules.ResetToDefaults();
                EditorUtility.SetDirty(rules);
            }
            return rules;
        }

        private static ClassData CreateKnight()
        {
            ClassData knight = EditorSetupUtility.CreateOrLoadAsset<ClassData>(KnightPath, out bool created);
            if (!created) return knight;

            Write(knight, "knight", "Knight",
                "Short-range melee fighter. High direct damage, larger health pool.",
                WeaponType.Sword,
                maxHealth: 100f, attack: 12f, defense: 10f,
                attackSpeed: 1.0f, moveSpeed: 5f,
                range: 1.6f, arc: 120f,
                tint: new Color(0.35f, 0.75f, 1f));
            return knight;
        }

        private static ClassData CreateArcher()
        {
            ClassData archer = EditorSetupUtility.CreateOrLoadAsset<ClassData>(ArcherPath, out bool created);
            if (!created) return archer;

            Write(archer, "archer", "Archer",
                "Ranged fighter. Lower basic attack damage, safer range, lower HP.",
                WeaponType.Bow,
                maxHealth: 75f, attack: 8f, defense: 20f,
                attackSpeed: 1.4f, moveSpeed: 5f,
                range: 8f, arc: 0f,
                tint: new Color(0.45f, 0.9f, 0.5f));
            return archer;
        }

        private static void Write(ClassData target, string id, string displayName, string description,
            WeaponType weaponType, float maxHealth, float attack, float defense,
            float attackSpeed, float moveSpeed, float range, float arc, Color tint)
        {
            EditorSetupUtility.SetPrivateField(target, "classId", id);
            EditorSetupUtility.SetPrivateField(target, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(target, "description", description);
            EditorSetupUtility.SetPrivateField(target, "allowedWeaponType", weaponType);
            EditorSetupUtility.SetPrivateField(target, "maxHealth", maxHealth);
            EditorSetupUtility.SetPrivateField(target, "attack", attack);
            EditorSetupUtility.SetPrivateField(target, "defense", defense);
            EditorSetupUtility.SetPrivateField(target, "attackSpeed", attackSpeed);
            EditorSetupUtility.SetPrivateField(target, "moveSpeed", moveSpeed);
            EditorSetupUtility.SetPrivateField(target, "critChance", 0.05f);
            EditorSetupUtility.SetPrivateField(target, "critDamage", 1.5f);
            EditorSetupUtility.SetPrivateField(target, "baseAttackRange", range);
            EditorSetupUtility.SetPrivateField(target, "baseAttackArcDegrees", arc);
            EditorSetupUtility.SetPrivateField(target, "bodyTint", tint);
        }

        private static ClassRegistry CreateRegistry(ClassData knight, ClassData archer)
        {
            ClassRegistry registry = EditorSetupUtility.CreateOrLoadAsset<ClassRegistry>(RegistryPath, out _);
            EditorSetupUtility.SetPrivateObjectList(registry, "classes",
                new Object[] { knight, archer });
            return registry;
        }

        // ------------------------------------------------------------------ scene

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 2] {ScenePath} does not exist. " +
                               "Run RPG > Phase 1 > Build Test Scene first.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static PlayerStats SetupPlayer(GameObject player, ClassData defaultClass, StatRules rules)
        {
            PlayerStats stats = EditorSetupUtility.EnsureComponent<PlayerStats>(player);
            EditorSetupUtility.SetPrivateField(stats, "startingClass", defaultClass);
            EditorSetupUtility.SetPrivateField(stats, "statRules", rules);

            PlayerStatsBinder binder = EditorSetupUtility.EnsureComponent<PlayerStatsBinder>(player);
            EditorSetupUtility.SetPrivateField(binder, "bodyRenderer", player.GetComponent<SpriteRenderer>());

            return stats;
        }

        private static ClassSelectionPanel SetupSelectionPanel(GameObject hud, ClassRegistry registry,
            PlayerStats stats, GameObject player)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "ClassSelectPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Image panelImage = EditorSetupUtility.CreateUiImage("ClassSelectPanel", hud.transform,
                null, new Color(0.04f, 0.05f, 0.08f, 0.94f));
            var panelRect = (RectTransform)panelImage.transform;
            EditorSetupUtility.StretchFull(panelRect);
            panelRect.SetAsLastSibling();   // Draw above the joystick and attack button.

            Text title = EditorSetupUtility.CreateUiText("Title", panelRect,
                "CHOOSE YOUR CLASS", 54, TextAnchor.MiddleCenter);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(900f, 100f);
            titleRect.anchoredPosition = new Vector2(0f, -260f);

            GameObject container = EditorSetupUtility.CreateUiObject("ButtonContainer", panelRect);
            var containerRect = (RectTransform)container.transform;
            containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(820f, 700f);
            containerRect.anchoredPosition = new Vector2(0f, -40f);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 32f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button template = CreateButtonTemplate(containerRect);

            var panel = panelImage.gameObject.AddComponent<ClassSelectionPanel>();
            EditorSetupUtility.SetPrivateField(panel, "registry", registry);
            EditorSetupUtility.SetPrivateField(panel, "playerStats", stats);
            EditorSetupUtility.SetPrivateField(panel, "playerController", player.GetComponent<PlayerController>());
            EditorSetupUtility.SetPrivateField(panel, "panelRoot", panelImage.gameObject);
            EditorSetupUtility.SetPrivateField(panel, "buttonContainer", containerRect);
            EditorSetupUtility.SetPrivateField(panel, "buttonTemplate", template);

            return panel;
        }

        private static Button CreateButtonTemplate(RectTransform container)
        {
            Image buttonImage = EditorSetupUtility.CreateUiImage("ClassButtonTemplate", container,
                null, new Color(0.18f, 0.22f, 0.3f, 1f));

            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var layoutElement = buttonImage.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 220f;
            layoutElement.preferredWidth = 800f;

            Text label = EditorSetupUtility.CreateUiText("Label", buttonImage.transform,
                "Class", 32, TextAnchor.MiddleCenter);
            var labelRect = (RectTransform)label.transform;
            EditorSetupUtility.StretchFull(labelRect);
            labelRect.offsetMin = new Vector2(24f, 16f);
            labelRect.offsetMax = new Vector2(-24f, -16f);

            buttonImage.gameObject.SetActive(false);   // Template only; the panel clones it.
            return button;
        }

        private static void SetupDebugOverlay(GameObject devTools, PlayerStats stats, ClassSelectionPanel panel)
        {
            if (devTools == null)
            {
                Debug.LogWarning("[Phase 2] No 'DevTools' object found; skipping the stat overlay.");
                return;
            }

            StatsDebugOverlay overlay = EditorSetupUtility.EnsureComponent<StatsDebugOverlay>(devTools);
            EditorSetupUtility.SetPrivateField(overlay, "playerStats", stats);
            EditorSetupUtility.SetPrivateField(overlay, "classSelectionPanel", panel);
        }
    }
}
