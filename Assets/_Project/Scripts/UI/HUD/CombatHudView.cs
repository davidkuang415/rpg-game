using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Core.Events;
using RPG.Progression;
using RPG.Stages;

namespace RPG.UI.HUD
{
    /// <summary>
    /// The in-fight readout: which stage, which room, which wave, how many enemies are left -
    /// and a banner that announces the beats (a new wave, a cleared room, a level-up).
    ///
    /// Until now the only information on screen during a fight was health bars. The player
    /// had no way to know whether the enemy in front of them was the last one in the stage or
    /// the first of three waves, which turns every stage into a fight of unknown length. This
    /// is the missing status line.
    ///
    /// It renders from the live state of the stage's rooms whenever anything changes, rather
    /// than counting events - the first wave of a stage spawns synchronously inside
    /// StageController.Begin, before StageStarted is even raised, so an event counter would
    /// always start one behind.
    /// </summary>
    public class CombatHudView : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private StageEventChannel stageEvents;
        [SerializeField] private StageManager stageManager;
        [SerializeField] private PlayerReference playerReference;

        [Tooltip("Optional. Mentioned in the room-cleared banner when it healed something.")]
        [SerializeField] private RoomClearHeal roomClearHeal;

        [Header("Widgets")]
        [Tooltip("Everything shown only during a fight. Defaults to this object.")]
        [SerializeField] private GameObject root;

        [SerializeField] private Text progressLabel;
        [SerializeField] private Text bannerLabel;
        [SerializeField] private CanvasGroup bannerGroup;

        [Header("Banner timing")]
        [SerializeField, Min(0f)] private float bannerHold = 1.1f;
        [SerializeField, Min(0.01f)] private float bannerFade = 0.35f;

        private readonly List<RoomController> _boundRooms = new List<RoomController>();
        private StageData _stage;
        private PlayerLevel _playerLevel;
        private Coroutine _banner;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (bannerGroup != null) bannerGroup.alpha = 0f;

            // Awake/OnDestroy rather than OnEnable/OnDisable: when 'root' is this object,
            // hiding deactivates the listener too, and a HUD that only listens while visible
            // could never be the thing that shows itself (see StageCompleteScreen).
            if (stageEvents != null)
            {
                stageEvents.StageStarted += OnStageStarted;
                stageEvents.RoomCleared += OnRoomCleared;
                stageEvents.StageCompleted += OnStageEnded;
                stageEvents.StageFailed += OnStageEnded;
                stageEvents.StageUnloaded += OnStageEnded;
            }

            if (roomClearHeal != null) roomClearHeal.RoomHealed += OnRoomHealed;
        }

        private void OnDestroy()
        {
            if (stageEvents != null)
            {
                stageEvents.StageStarted -= OnStageStarted;
                stageEvents.RoomCleared -= OnRoomCleared;
                stageEvents.StageCompleted -= OnStageEnded;
                stageEvents.StageFailed -= OnStageEnded;
                stageEvents.StageUnloaded -= OnStageEnded;
            }

            if (roomClearHeal != null) roomClearHeal.RoomHealed -= OnRoomHealed;

            UnbindRooms();
            UnbindPlayer();
        }

        private void Start()
        {
            // Hidden until a stage starts. Done in Start rather than Awake so a stage that was
            // somehow already running when this enabled still gets its StageStarted first.
            if (stageManager == null || !stageManager.IsStageActive) SetShown(false);
        }

        private void Update()
        {
            if (_playerLevel == null) BindPlayer();
        }

        private void OnDisable()
        {
            // The banner coroutine dies with the object; leave it in a clean state.
            _banner = null;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
        }

        // ------------------------------------------------------------------ stage events

        private void OnStageStarted(StageData stage)
        {
            _stage = stage;
            SetShown(true);
            BindRooms();
            Refresh();

            string title = stage != null ? stage.DisplayName.ToUpperInvariant() : "STAGE";
            if (stage != null && stage.IsBossStage) title += "\nBOSS STAGE";
            ShowBanner(title);
        }

        private void OnRoomCleared(RoomController room)
        {
            Refresh();

            // With a heal component wired, the banner waits for its result (see OnRoomHealed).
            if (roomClearHeal == null) ShowRoomClearedBanner(room, 0f);
        }

        private void OnRoomHealed(RoomController room, float healed) => ShowRoomClearedBanner(room, healed);

        private void ShowRoomClearedBanner(RoomController room, float healed)
        {
            if (room != null && room.IsFinalRoom) return;   // The completion screen takes over.

            string text = "ROOM CLEARED";
            if (healed > 0.5f) text += $"\n+{healed:0} HP";
            ShowBanner(text);
        }

        private void OnStageEnded(StageData stage)
        {
            UnbindRooms();
            SetShown(false);
        }

        // ------------------------------------------------------------------ rooms

        private void BindRooms()
        {
            UnbindRooms();

            StageController controller = stageManager != null ? stageManager.CurrentController : null;
            if (controller == null) return;

            for (int i = 0; i < controller.Rooms.Count; i++)
            {
                RoomController room = controller.Rooms[i];
                if (room == null) continue;

                room.WaveStarted += OnWaveStarted;
                room.AliveCountChanged += OnAliveCountChanged;
                _boundRooms.Add(room);
            }
        }

        private void UnbindRooms()
        {
            for (int i = 0; i < _boundRooms.Count; i++)
            {
                if (_boundRooms[i] == null) continue;
                _boundRooms[i].WaveStarted -= OnWaveStarted;
                _boundRooms[i].AliveCountChanged -= OnAliveCountChanged;
            }
            _boundRooms.Clear();
        }

        private void OnWaveStarted(RoomController room, int wave, int total)
        {
            Refresh();

            // The first wave of a room is announced by the room itself (stage start or the door
            // opening); only the reinforcements need calling out.
            if (wave > 1) ShowBanner(total > 1 ? $"WAVE {wave}/{total}" : "REINFORCEMENTS");
        }

        private void OnAliveCountChanged(int alive) => Refresh();

        // ------------------------------------------------------------------ player

        private void BindPlayer()
        {
            if (playerReference == null || !playerReference.Exists) return;

            _playerLevel = playerReference.GameObject.GetComponent<PlayerLevel>();
            if (_playerLevel != null) _playerLevel.LeveledUp += OnLeveledUp;
        }

        private void UnbindPlayer()
        {
            if (_playerLevel == null) return;
            _playerLevel.LeveledUp -= OnLeveledUp;
            _playerLevel = null;
        }

        private void OnLeveledUp(int previous, int next)
        {
            if (!IsShown) return;
            ShowBanner($"LEVEL UP!\n{next}");
        }

        // ------------------------------------------------------------------ rendering

        private bool IsShown => root != null && root.activeSelf;

        private void SetShown(bool shown)
        {
            if (root != null) root.SetActive(shown);

            if (!shown && _banner != null)
            {
                StopCoroutine(_banner);
                _banner = null;
                if (bannerGroup != null) bannerGroup.alpha = 0f;
            }
        }

        private void Refresh()
        {
            if (progressLabel == null) return;

            StageController controller = stageManager != null ? stageManager.CurrentController : null;
            if (controller == null)
            {
                progressLabel.text = string.Empty;
                return;
            }

            int roomCount = 0, cleared = 0, alive = 0;
            RoomController active = null;

            for (int i = 0; i < controller.Rooms.Count; i++)
            {
                RoomController room = controller.Rooms[i];
                if (room == null) continue;

                roomCount++;
                if (room.State == RoomController.RoomState.Cleared) cleared++;
                if (room.State == RoomController.RoomState.Active)
                {
                    alive += room.AliveEnemyCount;
                    if (active == null) active = room;
                }
            }

            string stageName = _stage != null ? _stage.DisplayName.ToUpperInvariant() : "STAGE";
            int roomNumber = Mathf.Min(cleared + 1, Mathf.Max(1, roomCount));

            string text = roomCount > 1 ? $"{stageName}   ROOM {roomNumber}/{roomCount}" : stageName;

            if (active != null)
            {
                int totalWaves = active.RequiredWaveCount;
                if (totalWaves > 1) text += $"   WAVE {Mathf.Max(1, active.CurrentWave)}/{totalWaves}";
                text += alive == 1 ? "   1 LEFT" : $"   {alive} LEFT";
            }

            progressLabel.text = text;
        }

        /// <summary>Shows a headline over the arena for a moment. A new one replaces the old.</summary>
        public void ShowBanner(string text)
        {
            if (bannerLabel == null || bannerGroup == null || !IsShown) return;

            bannerLabel.text = text;
            if (_banner != null) StopCoroutine(_banner);
            _banner = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            Transform t = bannerLabel.transform;
            float elapsed = 0f;
            const float popIn = 0.14f;

            while (elapsed < popIn)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / popIn);
                bannerGroup.alpha = p;
                t.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, 1f - (1f - p) * (1f - p));
                yield return null;
            }

            bannerGroup.alpha = 1f;
            t.localScale = Vector3.one;

            yield return new WaitForSeconds(bannerHold);

            elapsed = 0f;
            while (elapsed < bannerFade)
            {
                elapsed += Time.deltaTime;
                bannerGroup.alpha = 1f - Mathf.Clamp01(elapsed / bannerFade);
                yield return null;
            }

            bannerGroup.alpha = 0f;
            _banner = null;
        }
    }
}
