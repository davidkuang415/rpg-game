<<<<<<< HEAD
using System.Collections.Generic;
using System.IO;
=======
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
<<<<<<< HEAD
using RPG.Audio;
using RPG.Core.Events;
using RPG.Enemies;
using RPG.Loot;
using RPG.Player;
using RPG.Player.Input;
using RPG.Progression;
using RPG.Stages;
using RPG.UI;
using RPG.UI.Controls;
using RPG.UI.HUD;
using RPG.Vfx;
=======
using RPG.CameraSystem;
using RPG.Core;
using RPG.UI.HUD;
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0

namespace RPG.EditorTools
{
    /// <summary>
<<<<<<< HEAD
    /// Phase 15: the playtest pass. Everything a critical first hour of play asked for:
    ///
    ///   - a dodge roll (second HUD button, Shift on the keyboard) with invulnerability frames
    ///   - a pause menu with Resume / Sound / Leave Stage - the first way out of a fight that
    ///     is not winning or dying
    ///   - a combat readout (stage, room, wave, enemies left) and a banner for the beats
    ///   - elites that actually exist: bigger, gold-outlined, tougher, worth 2.5x, elite loot
    ///   - a boss health bar and an enrage phase for the Warlord
    ///   - a heal on room clear and a full heal (plus a shake and a banner) on level-up
    ///   - sound, synthesised at startup, so the game stops being silent
    ///
    /// Safe to re-run. It rebuilds the HUD pieces it owns and only ever adds to the rest.
=======
    /// Phase 15: see-more-of-the-arena pass.
    ///
    ///   - The camera pulls back, so more of a room is visible at once.
    ///   - An arrow at the screen edge points at every living enemy the camera can't see, so a
    ///     straggler behind the camera (or across a big room) is never just missing.
    ///
    /// The circular backdrop every character used to have behind it (Phase 12's stand-in
    /// outline, from when every body was a flat-tinted circle) is removed in Phase 12 itself -
    /// RestructureCharacter now strips it - so re-run Phase 12 to pick that up if it hasn't run
    /// since Phase 14's real art went in.
    ///
    /// Safe to re-run.
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
    /// </summary>
    public static class Phase15SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
<<<<<<< HEAD
        private const string CoreDataFolder = "Assets/_Project/Data/Core";
        private const string StageDataFolder = "Assets/_Project/Data/Stages";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";
        private const string InputDataFolder = "Assets/_Project/Data/Input";
        private const string LootDataFolder = "Assets/_Project/Data/Loot";
        private const string StagePrefabFolder = "Assets/_Project/Prefabs/Stages";
        private const string BossPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_BossWarlord.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const string KenneyButtonPanelPath = "Assets/_Project/Art/Kenney/UI/ButtonPanel.png";
        private const string CirclePath = "Assets/_Project/Art/Placeholder/Circle.png";
        private const string SquarePath = "Assets/_Project/Art/Placeholder/Square.png";

        private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.09f, 0.94f);

        /// <summary>
        /// Which spawn points become elites, per stage prefab. Mirrors the `elite: true` marks
        /// in Phase12StageLayouts, so the prefabs match whether they were rebuilt by the
        /// Phase 12 tool or only touched by this one.
        /// </summary>
        private static readonly Dictionary<string, string[]> ElitePromotions = new Dictionary<string, string[]>
        {
            { "Stage_04_Pillars", new[] { "W3_Brute" } },
            { "Stage_05_Crossroads", new[] { "C_Brute_2" } },
            { "Stage_06_Gauntlet", new[] { "C_Brute" } },
            { "Stage_07_Ambush", new[] { "W3_Brute_2" } },
            { "Stage_08_Switchback", new[] { "A_Slinger_2", "B_Brute_2" } },
            { "Stage_09_Citadel", new[] { "C_Brute_2", "D_Brute_1" } },
            { "Stage_10_Warlord", new[] { "E_Grunt_3", "E_Grunt_4" } },
        };

        [MenuItem("RPG/Phase 15/Apply Playtest Pass", priority = 300)]
=======
        private const string ArrowSpritePath = "Assets/_Project/Art/Kenney/UI/EnemyArrow.png";

        // Was 12 (Phase 1). 20 shows ~67% more of the arena at once.
        private const float PreviousVisibleWorldHeight = 12f;
        private const float NewVisibleWorldHeight = 20f;

        [MenuItem("RPG/Phase 15/See More Of The Arena", priority = 280)]
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

<<<<<<< HEAD
            GameObject hud = GameObject.Find("HUD");
            GameObject systems = GameObject.Find("GameSystems");
            GameObject player = GameObject.Find("Player");
            if (hud == null || systems == null || player == null)
            {
                Debug.LogError("[Phase 15] Missing HUD, GameSystems or Player. Run the earlier tools first.");
                return;
            }

            var inputChannel = AssetDatabase.LoadAssetAtPath<PlayerInputChannel>(InputDataFolder + "/PlayerInputChannel.asset");
            var stageEvents = AssetDatabase.LoadAssetAtPath<StageEventChannel>(StageDataFolder + "/StageEventChannel.asset");
            var enemyEvents = AssetDatabase.LoadAssetAtPath<EnemyEventChannel>(EnemyDataFolder + "/EnemyEventChannel.asset");
            var playerReference = AssetDatabase.LoadAssetAtPath<PlayerReference>(CoreDataFolder + "/PlayerReference.asset");
            var feedback = AssetDatabase.LoadAssetAtPath<CombatFeedbackChannel>(CoreDataFolder + "/CombatFeedbackChannel.asset");

            if (inputChannel == null || stageEvents == null || enemyEvents == null || playerReference == null)
            {
                Debug.LogError("[Phase 15] A shared data asset is missing (input channel, stage events, " +
                               "enemy events or player reference). Run the earlier tools first.");
                return;
            }

            // --- player -------------------------------------------------------------
            SetUpDash(player);
            BuildDashButton(hud, inputChannel);

            // --- systems ------------------------------------------------------------
            var roomHeal = SetUpRoomClearHeal(systems, stageEvents, playerReference);
            SetUpLevelUpFeedback(systems, playerReference);
            SfxPlayer sfx = SetUpSound(systems, feedback, enemyEvents, stageEvents, playerReference);
            WireEliteLoot(systems);

            // --- hud ----------------------------------------------------------------
            Button pauseButton = BuildCombatHud(hud, systems, stageEvents, playerReference, roomHeal);
            BuildBossBar(hud, enemyEvents, stageEvents);
            PauseMenuScreen pauseMenu = BuildPauseMenu(hud, systems, player, playerReference, sfx, pauseButton);

            var flow = systems.GetComponent<GameFlowController>();
            if (flow != null) EditorSetupUtility.SetPrivateField(flow, "pauseMenu", pauseMenu);

            KeepModalScreensOnTop(hud);

            // --- content ------------------------------------------------------------
            FlagBoss();
            AddEnrageToBoss();
            PromoteElites();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            ReconcilePlayerPrefab(player);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Verify(hud, systems, player);
=======
            GameObject camera = GameObject.Find("Main Camera");
            GameObject hud = GameObject.Find("HUD");

            if (camera == null || hud == null)
            {
                Debug.LogError("[Phase 15] Missing Main Camera or HUD. Run the earlier tools first.");
                return;
            }

            ZoomOut(camera);
            BuildOffscreenIndicators(hud, camera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Verify(camera, hud);
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 15] {ScenePath} is missing.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

<<<<<<< HEAD
        // ================================================================== player

        private static void SetUpDash(GameObject player)
        {
            var dash = EditorSetupUtility.EnsureComponent<PlayerDash>(player);

            var controller = player.GetComponent<PlayerController>();
            if (controller != null) EditorSetupUtility.SetPrivateField(controller, "dash", dash);
        }

        /// <summary>
        /// A second thumb button, left of ATTACK and a little smaller. Same TouchActionButton
        /// the attack button uses, just a different action.
        /// </summary>
        private static void BuildDashButton(GameObject hud, PlayerInputChannel inputChannel)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "DashButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            Image image = EditorSetupUtility.CreateUiImage("DashButton", hud.transform, circle,
                new Color(0.35f, 0.65f, 0.95f, 0.75f));

            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 160f);
            rect.anchoredPosition = new Vector2(-420f, 210f);

            Text label = EditorSetupUtility.CreateUiText("Label", rect, "DASH", 26, TextAnchor.MiddleCenter);
            EditorSetupUtility.StretchFull((RectTransform)label.transform);
            label.color = new Color(1f, 1f, 1f, 0.9f);

            var button = image.gameObject.AddComponent<TouchActionButton>();
            EditorSetupUtility.SetPrivateField(button, "inputChannel", inputChannel);
            EditorSetupUtility.SetPrivateField(button, "action", TouchActionButton.ActionType.Dash);

            // Right after the attack button in the hierarchy, so both sit under the hub and
            // the modal screens like the attack button already does.
            Transform attack = EditorSetupUtility.FindChild(hud.transform, "AttackButton");
            if (attack != null) rect.SetSiblingIndex(attack.GetSiblingIndex() + 1);
        }

        // ================================================================== systems

        private static RoomClearHeal SetUpRoomClearHeal(GameObject systems, StageEventChannel stageEvents,
            PlayerReference playerReference)
        {
            var heal = EditorSetupUtility.EnsureComponent<RoomClearHeal>(systems);
            EditorSetupUtility.SetPrivateField(heal, "stageEvents", stageEvents);
            EditorSetupUtility.SetPrivateField(heal, "playerReference", playerReference);
            return heal;
        }

        private static void SetUpLevelUpFeedback(GameObject systems, PlayerReference playerReference)
        {
            var feedback = EditorSetupUtility.EnsureComponent<LevelUpFeedback>(systems);
            EditorSetupUtility.SetPrivateField(feedback, "playerReference", playerReference);

            Camera main = Camera.main;
            if (main != null && main.GetComponent<RPG.CameraSystem.CameraShake>() != null)
            {
                EditorSetupUtility.SetPrivateField(feedback, "cameraShake",
                    main.GetComponent<RPG.CameraSystem.CameraShake>());
            }
        }

        private static SfxPlayer SetUpSound(GameObject systems, CombatFeedbackChannel feedback,
            EnemyEventChannel enemyEvents, StageEventChannel stageEvents, PlayerReference playerReference)
        {
            var sfx = EditorSetupUtility.EnsureComponent<SfxPlayer>(systems);
            if (feedback != null) EditorSetupUtility.SetPrivateField(sfx, "feedback", feedback);
            EditorSetupUtility.SetPrivateField(sfx, "enemyEvents", enemyEvents);
            EditorSetupUtility.SetPrivateField(sfx, "stageEvents", stageEvents);
            EditorSetupUtility.SetPrivateField(sfx, "playerReference", playerReference);

            // Sound needs a listener; the camera is the conventional place for it.
            Camera main = Camera.main;
            if (main != null && Object.FindAnyObjectByType<AudioListener>() == null)
            {
                main.gameObject.AddComponent<AudioListener>();
            }

            return sfx;
        }

        private static void WireEliteLoot(GameObject systems)
        {
            var collector = systems.GetComponent<StageRewardCollector>();
            var eliteTable = AssetDatabase.LoadAssetAtPath<LootTableData>(LootDataFolder + "/LootTable_Elite.asset");

            if (collector == null || eliteTable == null)
            {
                Debug.LogWarning("[Phase 15] StageRewardCollector or LootTable_Elite missing; elites " +
                                 "will drop from their normal tables.");
                return;
            }

            EditorSetupUtility.SetPrivateField(collector, "eliteLootTable", eliteTable);
        }

        // ================================================================== hud

        /// <summary>
        /// The combat readout. The component lives on an always-active holder so it can run
        /// coroutines while the visible part ('Contents') is hidden between stages.
        /// </summary>
        private static Button BuildCombatHud(GameObject hud, GameObject systems, StageEventChannel stageEvents,
            PlayerReference playerReference, RoomClearHeal roomHeal)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "CombatHud");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject holder = EditorSetupUtility.CreateUiObject("CombatHud", hud.transform);
            EditorSetupUtility.StretchFull((RectTransform)holder.transform);

            GameObject contents = EditorSetupUtility.CreateUiObject("Contents", holder.transform);
            EditorSetupUtility.StretchFull((RectTransform)contents.transform);

            // Progress line, top centre, below the notch zone.
            Text progress = EditorSetupUtility.CreateUiText("ProgressLabel", contents.transform,
                "STAGE", 30, TextAnchor.MiddleCenter);
            var progressRect = (RectTransform)progress.transform;
            progressRect.anchorMin = progressRect.anchorMax = new Vector2(0.5f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.sizeDelta = new Vector2(760f, 46f);
            progressRect.anchoredPosition = new Vector2(0f, -24f);
            progress.color = new Color(0.9f, 0.92f, 0.96f, 0.95f);
            AddShadow(progress);

            // Banner, upper third of the screen, faded out by default.
            Text banner = EditorSetupUtility.CreateUiText("Banner", contents.transform,
                string.Empty, 64, TextAnchor.MiddleCenter);
            var bannerRect = (RectTransform)banner.transform;
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(1000f, 200f);
            bannerRect.anchoredPosition = new Vector2(0f, -420f);
            banner.color = new Color(1f, 0.95f, 0.8f);
            banner.fontStyle = FontStyle.Bold;
            AddShadow(banner);
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;

            // Pause, top left. The debug RESET button owns the top-right corner.
            Button pause = LabeledButton("PauseButton", contents.transform, "PAUSE", 24,
                new Color(0.2f, 0.22f, 0.28f, 0.85f), new Vector2(150f, 64f));
            var pauseRect = (RectTransform)pause.transform;
            pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(0f, 1f);
            pauseRect.pivot = new Vector2(0f, 1f);
            pauseRect.anchoredPosition = new Vector2(16f, -16f);

            var view = holder.AddComponent<CombatHudView>();
            EditorSetupUtility.SetPrivateField(view, "stageEvents", stageEvents);
            EditorSetupUtility.SetPrivateField(view, "stageManager", systems.GetComponent<StageManager>());
            EditorSetupUtility.SetPrivateField(view, "playerReference", playerReference);
            EditorSetupUtility.SetPrivateField(view, "roomClearHeal", roomHeal);
            EditorSetupUtility.SetPrivateField(view, "root", contents);
            EditorSetupUtility.SetPrivateField(view, "progressLabel", progress);
            EditorSetupUtility.SetPrivateField(view, "bannerLabel", banner);
            EditorSetupUtility.SetPrivateField(view, "bannerGroup", bannerGroup);

            // Under the hub and the modal screens: the hub is a full-screen panel and must
            // cover this between fights.
            PlaceBelowHub(hud, holder.transform);

            return pause;
        }

        private static void BuildBossBar(GameObject hud, EnemyEventChannel enemyEvents, StageEventChannel stageEvents)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "BossHealthBar");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);

            GameObject holder = EditorSetupUtility.CreateUiObject("BossHealthBar", hud.transform);
            EditorSetupUtility.StretchFull((RectTransform)holder.transform);

            GameObject contents = EditorSetupUtility.CreateUiObject("Contents", holder.transform);
            var contentsRect = (RectTransform)contents.transform;
            contentsRect.anchorMin = contentsRect.anchorMax = new Vector2(0.5f, 1f);
            contentsRect.pivot = new Vector2(0.5f, 1f);
            contentsRect.sizeDelta = new Vector2(900f, 110f);
            contentsRect.anchoredPosition = new Vector2(0f, -80f);

            Text name = EditorSetupUtility.CreateUiText("NameLabel", contents.transform, "BOSS", 32,
                TextAnchor.MiddleLeft);
            var nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(0.6f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.sizeDelta = new Vector2(0f, 44f);
            nameRect.anchoredPosition = new Vector2(8f, 0f);
            name.color = new Color(1f, 0.85f, 0.85f);
            AddShadow(name);

            Text phase = EditorSetupUtility.CreateUiText("PhaseLabel", contents.transform, string.Empty, 28,
                TextAnchor.MiddleRight);
            var phaseRect = (RectTransform)phase.transform;
            phaseRect.anchorMin = new Vector2(0.4f, 1f);
            phaseRect.anchorMax = new Vector2(1f, 1f);
            phaseRect.pivot = new Vector2(1f, 1f);
            phaseRect.sizeDelta = new Vector2(0f, 44f);
            phaseRect.anchoredPosition = new Vector2(-8f, 0f);
            phase.color = new Color(1f, 0.6f, 0.2f);
            AddShadow(phase);

            Image back = EditorSetupUtility.CreateUiImage("BarBack", contents.transform, square,
                new Color(0.05f, 0.05f, 0.07f, 0.85f));
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = new Vector2(0f, 0f);
            backRect.anchorMax = new Vector2(1f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(0f, 40f);
            backRect.anchoredPosition = Vector2.zero;
            back.raycastTarget = false;

            Image fill = EditorSetupUtility.CreateUiImage("Fill", backRect, square, new Color(0.85f, 0.3f, 0.32f));
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            var view = holder.AddComponent<BossHealthBarView>();
            EditorSetupUtility.SetPrivateField(view, "enemyEvents", enemyEvents);
            EditorSetupUtility.SetPrivateField(view, "stageEvents", stageEvents);
            EditorSetupUtility.SetPrivateField(view, "root", contents);
            EditorSetupUtility.SetPrivateField(view, "nameLabel", name);
            EditorSetupUtility.SetPrivateField(view, "phaseLabel", phase);
            EditorSetupUtility.SetPrivateField(view, "fill", fill);

            PlaceBelowHub(hud, holder.transform);
        }

        private static PauseMenuScreen BuildPauseMenu(GameObject hud, GameObject systems, GameObject player,
            PlayerReference playerReference, SfxPlayer sfx, Button openButton)
        {
            Transform existing = EditorSetupUtility.FindChild(hud.transform, "PauseMenuPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Image panelImage = EditorSetupUtility.CreateUiImage("PauseMenuPanel", hud.transform, null, PanelColor);
            var panelRect = (RectTransform)panelImage.transform;
            EditorSetupUtility.StretchFull(panelRect);

            Text title = EditorSetupUtility.CreateUiText("Title", panelRect, "PAUSED", 58, TextAnchor.MiddleCenter);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(1000f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -520f);
            AddShadow(title);

            Text hint = EditorSetupUtility.CreateUiText("Hint", panelRect,
                "Leaving keeps everything earned so far.\nThe next stage stays locked until this one is cleared.",
                26, TextAnchor.UpperCenter);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(880f, 140f);
            hintRect.anchoredPosition = new Vector2(0f, 40f);
            hint.color = new Color(0.75f, 0.78f, 0.85f);

            Button resume = CenteredButton("ResumeButton", panelRect, "RESUME", 38,
                new Color(0.22f, 0.4f, 0.3f), new Vector2(520f, 130f), 480f);
            Button sound = CenteredButton("SoundButton", panelRect, "SOUND: ON", 32,
                new Color(0.25f, 0.28f, 0.36f), new Vector2(520f, 110f), 320f);
            Button leave = CenteredButton("LeaveButton", panelRect, "LEAVE STAGE", 34,
                new Color(0.42f, 0.2f, 0.22f), new Vector2(520f, 120f), 160f);

            var screen = panelImage.gameObject.AddComponent<PauseMenuScreen>();
            EditorSetupUtility.SetPrivateField(screen, "panelRoot", panelImage.gameObject);
            EditorSetupUtility.SetPrivateField(screen, "playerController", player.GetComponent<PlayerController>());
            EditorSetupUtility.SetPrivateField(screen, "stageManager", systems.GetComponent<StageManager>());
            EditorSetupUtility.SetPrivateField(screen, "failureHandler", systems.GetComponent<StageFailureHandler>());
            EditorSetupUtility.SetPrivateField(screen, "playerReference", playerReference);
            EditorSetupUtility.SetPrivateField(screen, "sfx", sfx);
            EditorSetupUtility.SetPrivateField(screen, "openButton", openButton);
            EditorSetupUtility.SetPrivateField(screen, "resumeButton", resume);
            EditorSetupUtility.SetPrivateField(screen, "soundButton", sound);
            EditorSetupUtility.SetPrivateField(screen, "soundLabel", sound.GetComponentInChildren<Text>());
            EditorSetupUtility.SetPrivateField(screen, "leaveButton", leave);

            RestyleButtons(panelImage.gameObject);
            return screen;
        }

        /// <summary>Same ordering rule Phase 11 applies: anything modal draws over the hub.</summary>
        private static void KeepModalScreensOnTop(GameObject hud)
        {
            string[] onTop =
            {
                "ClassSelectPanel", "StageCompletePanel", "StageFailedPanel", "PauseMenuPanel",
                "ItemTooltip", "DebugResetButton"
            };

            for (int i = 0; i < onTop.Length; i++)
            {
                Transform found = EditorSetupUtility.FindChild(hud.transform, onTop[i]);
                if (found != null) found.SetAsLastSibling();
            }
        }

        private static void PlaceBelowHub(GameObject hud, Transform target)
        {
            Transform hub = EditorSetupUtility.FindChild(hud.transform, "HubScreen");
            if (hub != null) target.SetSiblingIndex(hub.GetSiblingIndex());
        }

        // ================================================================== content

        private static void FlagBoss()
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/BossWarlord.asset");
            if (boss == null)
            {
                Debug.LogWarning("[Phase 15] BossWarlord.asset missing; no boss bar will bind.");
                return;
            }

            EditorSetupUtility.SetPrivateField(boss, "isBoss", true);
        }

        private static void AddEnrageToBoss()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) == null)
            {
                Debug.LogWarning("[Phase 15] Boss prefab missing; skipping the enrage phase.");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            EditorSetupUtility.EnsureComponent<BossEnrage>(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, BossPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        /// <summary>
        /// Flips the spawn points in ElitePromotions to elite, inside the stage prefabs. Every
        /// other spawn point in those prefabs is left exactly as it was.
        /// </summary>
        private static void PromoteElites()
        {
            int promoted = 0;

            foreach (KeyValuePair<string, string[]> entry in ElitePromotions)
            {
                string path = $"{StagePrefabFolder}/{entry.Key}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[Phase 15] {path} missing; skipping its elites.");
                    continue;
                }

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                SpawnPoint[] points = contents.GetComponentsInChildren<SpawnPoint>(true);

                for (int i = 0; i < points.Length; i++)
                {
                    if (System.Array.IndexOf(entry.Value, points[i].name) < 0) continue;
                    EditorSetupUtility.SetPrivateField(points[i], "spawnAsElite", true);
                    promoted++;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log($"[Phase 15] Promoted {promoted} spawn point(s) to elite across stages 4-10.");
        }

        private static void ReconcilePlayerPrefab(GameObject player)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(player)) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null) return;

            PrefabUtility.ApplyPrefabInstance(player, InteractionMode.AutomatedAction);
        }

        // ================================================================== ui helpers

        private static Button LabeledButton(string name, Transform parent, string content, int fontSize,
            Color color, Vector2 size)
        {
            Image image = EditorSetupUtility.CreateUiImage(name, parent, null, color);
            ((RectTransform)image.transform).sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text label = EditorSetupUtility.CreateUiText("Label", image.transform, content, fontSize,
                TextAnchor.MiddleCenter);
            EditorSetupUtility.StretchFull((RectTransform)label.transform);

            RestyleButtons(image.gameObject);
            return button;
        }

        private static Button CenteredButton(string name, Transform parent, string text, int fontSize,
            Color color, Vector2 size, float y)
        {
            Button button = LabeledButton(name, parent, text, fontSize, color, size);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            return button;
        }

        /// <summary>
        /// The Phase 12/14 button look: Kenney's bevelled panel, 9-sliced, with the role colour
        /// washed toward white so it tints the art instead of crushing it.
        /// </summary>
        private static void RestyleButtons(GameObject root)
        {
            Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(KenneyButtonPanelPath);

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var image = buttons[i].GetComponent<Image>();
                if (image != null && rounded != null && image.sprite != rounded)
                {
                    image.color = Color.Lerp(image.color, Color.white, 0.6f);
                    image.sprite = rounded;
                    image.type = Image.Type.Sliced;
                }

                ColorBlock colors = buttons[i].colors;
                colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
                colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
                colors.fadeDuration = 0.06f;
                buttons[i].colors = colors;

                EditorSetupUtility.EnsureComponent<ButtonPressFeedback>(buttons[i].gameObject);
            }
        }

        private static void AddShadow(Text text)
        {
            var shadow = EditorSetupUtility.EnsureComponent<Shadow>(text.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
        }

        // ================================================================== verification

        private static void Verify(GameObject hud, GameObject systems, GameObject player)
        {
            bool ok = true;

            ok &= Check(player.GetComponent<PlayerDash>() != null, "Player has no PlayerDash.", player);
            ok &= Check(EditorSetupUtility.FindChild(hud.transform, "DashButton") != null, "HUD has no DashButton.", hud);
            ok &= Check(hud.GetComponentInChildren<CombatHudView>(true) != null, "HUD has no CombatHudView.", hud);
            ok &= Check(hud.GetComponentInChildren<BossHealthBarView>(true) != null, "HUD has no BossHealthBarView.", hud);
            ok &= Check(hud.GetComponentInChildren<PauseMenuScreen>(true) != null, "HUD has no PauseMenuScreen.", hud);
            ok &= Check(systems.GetComponent<SfxPlayer>() != null, "GameSystems has no SfxPlayer.", systems);
            ok &= Check(systems.GetComponent<RoomClearHeal>() != null, "GameSystems has no RoomClearHeal.", systems);
            ok &= Check(systems.GetComponent<LevelUpFeedback>() != null, "GameSystems has no LevelUpFeedback.", systems);

            var boss = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            ok &= Check(boss == null || boss.GetComponent<BossEnrage>() != null, "Boss prefab has no BossEnrage.", boss);

            if (ok)
            {
                Debug.Log("<b>[Phase 15]</b> Playtest pass applied and verified. Press Play: DASH (or " +
                          "Shift) rolls through attacks, PAUSE opens a menu with a way out, the top of " +
                          "the screen tells you where you are in the stage, elites appear from stage 4, " +
                          "the Warlord has a bar and a second phase, and everything makes a sound.");
            }
        }

        private static bool Check(bool condition, string message, Object context)
        {
            if (!condition) Debug.LogError($"[Phase 15] VERIFY FAILED: {message}", context);
            return condition;
        }
=======
        // ------------------------------------------------------------------ camera

        private static void ZoomOut(GameObject camera)
        {
            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                Debug.LogError("[Phase 15] Main Camera has no CameraFollow2D.");
                return;
            }

            EditorSetupUtility.SetPrivateField(follow, "visibleWorldHeight", NewVisibleWorldHeight);
        }

        // ------------------------------------------------------------------ offscreen indicator

        private static void BuildOffscreenIndicators(GameObject hud, GameObject cameraObject)
        {
            EditorSetupUtility.ConfigureSpriteImport(ArrowSpritePath, pixelsPerUnit: 100,
                wrap: TextureWrapMode.Clamp, meshType: SpriteMeshType.FullRect, border: Vector4.zero);
            Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);

            Transform existing = EditorSetupUtility.FindChild(hud.transform, "OffscreenIndicators");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject root = EditorSetupUtility.CreateUiObject("OffscreenIndicators", hud.transform);
            EditorSetupUtility.StretchFull((RectTransform)root.transform);

            Image arrowTemplate = EditorSetupUtility.CreateUiImage("ArrowTemplate", root.transform,
                arrowSprite, new Color(1f, 0.4f, 0.35f));
            arrowTemplate.raycastTarget = false;
            arrowTemplate.preserveAspect = true;

            var arrowRect = (RectTransform)arrowTemplate.transform;
            arrowRect.sizeDelta = new Vector2(64f, 64f);

            var indicator = root.AddComponent<OffscreenEnemyIndicator>();
            EditorSetupUtility.SetPrivateField(indicator, "targetCamera", cameraObject.GetComponent<Camera>());
            EditorSetupUtility.SetPrivateField(indicator, "enemyLayers", LayerMask.GetMask(GameLayers.Enemy));
            EditorSetupUtility.SetPrivateField(indicator, "arrowTemplate", arrowRect);
        }

        // ------------------------------------------------------------------ verification

        private static void Verify(GameObject camera, GameObject hud)
        {
            bool ok = true;

            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                Debug.LogError("[Phase 15] VERIFY FAILED: Main Camera has no CameraFollow2D.");
                ok = false;
            }

            if (EditorSetupUtility.FindChild(hud.transform, "OffscreenIndicators") == null)
            {
                Debug.LogError("[Phase 15] VERIFY FAILED: HUD has no OffscreenIndicators.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log($"<b>[Phase 15]</b> Camera pulled back from {PreviousVisibleWorldHeight} to " +
                          $"{NewVisibleWorldHeight} visible world units; off-screen enemies now get an " +
                          "edge arrow.");
            }
        }
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
    }
}
