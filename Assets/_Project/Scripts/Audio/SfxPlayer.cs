using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Core.Events;
using RPG.Enemies;
using RPG.Player;
using RPG.Player.Combat;
using RPG.Progression;
using RPG.Stages;
using RPG.UI;
using RPG.Vfx;

namespace RPG.Audio
{
    /// <summary>
    /// The sound layer. Listens to the game's existing event seams and plays a clip for each
    /// beat: swings, hits, deaths, dashes, level-ups, room clears, the boss, button taps.
    ///
    /// Nothing in the game references this; it only subscribes. That is the same rule the
    /// VFX follow (see CombatFeedbackChannel), and it means the game plays identically with
    /// this object deleted - just silently, which is how it has played until now.
    ///
    /// Clips come from ProceduralSfx unless an override is assigned in the Inspector, so real
    /// recordings can be dropped in one at a time.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        public enum Sound
        {
            Swing, Hit, Crit, Hurt, Dodge, Death, Dash, LevelUp, RoomClear,
            StageComplete, StageFailed, Enrage, BossSpawn, Click
        }

        [Serializable]
        public class ClipOverride
        {
            public Sound Sound;
            public AudioClip Clip;
        }

        [Header("Sources")]
        [SerializeField] private CombatFeedbackChannel feedback;
        [SerializeField] private EnemyEventChannel enemyEvents;
        [SerializeField] private StageEventChannel stageEvents;
        [SerializeField] private PlayerReference playerReference;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;

        [Tooltip("Random pitch spread, as a fraction. 0.08 = +/-8%. Stops repeated hits sounding like a loop.")]
        [SerializeField, Range(0f, 0.5f)] private float pitchJitter = 0.08f;

        [Tooltip("Shortest gap between two plays of the same sound. A Knight swing into five " +
                 "enemies is one hit sound, not five stacked on the same frame.")]
        [SerializeField, Min(0f)] private float minRepeatInterval = 0.045f;

        [Tooltip("Simultaneous sounds. Each needs its own AudioSource to carry its own pitch.")]
        [SerializeField, Range(1, 16)] private int voices = 6;

        [Header("Overrides (optional real clips)")]
        [SerializeField] private List<ClipOverride> overrides = new List<ClipOverride>();

        private const string MutedPrefKey = "rpg.sfx.muted";

        private readonly Dictionary<Sound, AudioClip> _clips = new Dictionary<Sound, AudioClip>();
        private readonly Dictionary<Sound, float> _lastPlayed = new Dictionary<Sound, float>();
        private AudioSource[] _voices;
        private int _nextVoice;

        // Player-side subscriptions, bound once the player registers.
        private GameObject _boundPlayer;
        private PlayerAttackBase[] _attacks;
        private PlayerDash _dash;
        private PlayerLevel _level;
        private BossEnrage _boss;

        private bool _muted;

        /// <summary>Silences everything. Persisted, so it survives a relaunch.</summary>
        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                PlayerPrefs.SetInt(MutedPrefKey, value ? 1 : 0);
            }
        }

        private void Awake()
        {
            _muted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1;

            _voices = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < _voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
                _voices[i] = source;
            }

            BuildClips();
        }

        private void OnEnable()
        {
            if (feedback != null) feedback.DamageShown += OnDamageShown;

            if (enemyEvents != null)
            {
                enemyEvents.EnemyDied += OnEnemyDied;
                enemyEvents.EnemySpawned += OnEnemySpawned;
            }

            if (stageEvents != null)
            {
                stageEvents.RoomCleared += OnRoomCleared;
                stageEvents.StageCompleted += OnStageCompleted;
                stageEvents.StageFailed += OnStageFailed;
                stageEvents.StageUnloaded += OnStageUnloaded;
            }

            ButtonPressFeedback.Pressed += OnButtonPressed;
        }

        private void OnDisable()
        {
            if (feedback != null) feedback.DamageShown -= OnDamageShown;

            if (enemyEvents != null)
            {
                enemyEvents.EnemyDied -= OnEnemyDied;
                enemyEvents.EnemySpawned -= OnEnemySpawned;
            }

            if (stageEvents != null)
            {
                stageEvents.RoomCleared -= OnRoomCleared;
                stageEvents.StageCompleted -= OnStageCompleted;
                stageEvents.StageFailed -= OnStageFailed;
                stageEvents.StageUnloaded -= OnStageUnloaded;
            }

            ButtonPressFeedback.Pressed -= OnButtonPressed;

            UnbindPlayer();
            UnbindBoss();
        }

        private void Update()
        {
            GameObject player = playerReference != null && playerReference.Exists ? playerReference.GameObject : null;
            if (player != _boundPlayer) BindPlayer(player);
        }

        // ------------------------------------------------------------------ clips

        private void BuildClips()
        {
            _clips[Sound.Swing] = ProceduralSfx.Swing();
            _clips[Sound.Hit] = ProceduralSfx.Hit();
            _clips[Sound.Crit] = ProceduralSfx.Crit();
            _clips[Sound.Hurt] = ProceduralSfx.Hurt();
            _clips[Sound.Dodge] = ProceduralSfx.Dodge();
            _clips[Sound.Death] = ProceduralSfx.Death();
            _clips[Sound.Dash] = ProceduralSfx.Dash();
            _clips[Sound.LevelUp] = ProceduralSfx.LevelUp();
            _clips[Sound.RoomClear] = ProceduralSfx.RoomClear();
            _clips[Sound.StageComplete] = ProceduralSfx.StageComplete();
            _clips[Sound.StageFailed] = ProceduralSfx.StageFailed();
            _clips[Sound.Enrage] = ProceduralSfx.Enrage();
            _clips[Sound.BossSpawn] = ProceduralSfx.BossSpawn();
            _clips[Sound.Click] = ProceduralSfx.Click();

            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i] != null && overrides[i].Clip != null) _clips[overrides[i].Sound] = overrides[i].Clip;
            }
        }

        /// <summary>Plays a sound. Safe to call from anywhere; silent when muted or throttled.</summary>
        public void Play(Sound sound, float volume = 1f, float pitch = 1f)
        {
            if (_muted || _voices == null) return;
            if (!_clips.TryGetValue(sound, out AudioClip clip) || clip == null) return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sound, out float last) && now - last < minRepeatInterval) return;
            _lastPlayed[sound] = now;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            voice.pitch = pitch * (1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter));
            voice.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        }

        // ------------------------------------------------------------------ combat

        private void OnDamageShown(DamageInfo info, DamageResult result, Transform victim)
        {
            bool onPlayer = playerReference != null && playerReference.Exists && victim == playerReference.Transform;

            if (result.WasDodged)
            {
                Play(Sound.Dodge, onPlayer ? 0.9f : 0.5f);
                return;
            }

            if (onPlayer)
            {
                Play(Sound.Hurt, info.IsCritical ? 1f : 0.85f, info.IsCritical ? 0.9f : 1f);
                return;
            }

            Play(info.IsCritical ? Sound.Crit : Sound.Hit, 0.9f);
        }

        private void OnEnemyDied(EnemyDeathInfo info)
        {
            bool big = info.IsElite || (info.Data != null && info.Data.IsBoss);
            Play(Sound.Death, big ? 1f : 0.75f, big ? 0.75f : 1f);
        }

        private void OnEnemySpawned(EnemyStats enemy)
        {
            if (enemy == null || enemy.Data == null || !enemy.Data.IsBoss) return;

            Play(Sound.BossSpawn);

            UnbindBoss();
            _boss = enemy.GetComponent<BossEnrage>();
            if (_boss != null) _boss.Enraged += OnBossEnraged;
        }

        private void OnBossEnraged(BossEnrage boss) => Play(Sound.Enrage);

        private void UnbindBoss()
        {
            if (_boss == null) return;
            _boss.Enraged -= OnBossEnraged;
            _boss = null;
        }

        // ------------------------------------------------------------------ player

        private void BindPlayer(GameObject player)
        {
            UnbindPlayer();
            _boundPlayer = player;
            if (player == null) return;

            _attacks = player.GetComponents<PlayerAttackBase>();
            for (int i = 0; i < _attacks.Length; i++) _attacks[i].Attacked += OnPlayerAttacked;

            _dash = player.GetComponent<PlayerDash>();
            if (_dash != null) _dash.Dashed += OnPlayerDashed;

            _level = player.GetComponent<PlayerLevel>();
            if (_level != null) _level.LeveledUp += OnLeveledUp;
        }

        private void UnbindPlayer()
        {
            if (_attacks != null)
            {
                for (int i = 0; i < _attacks.Length; i++)
                {
                    if (_attacks[i] != null) _attacks[i].Attacked -= OnPlayerAttacked;
                }
                _attacks = null;
            }

            if (_dash != null)
            {
                _dash.Dashed -= OnPlayerDashed;
                _dash = null;
            }

            if (_level != null)
            {
                _level.LeveledUp -= OnLeveledUp;
                _level = null;
            }

            _boundPlayer = null;
        }

        private void OnPlayerAttacked(Vector2 direction) => Play(Sound.Swing, 0.7f);
        private void OnPlayerDashed(Vector2 direction) => Play(Sound.Dash, 0.8f);
        private void OnLeveledUp(int previous, int next) => Play(Sound.LevelUp);

        // ------------------------------------------------------------------ stage

        private void OnRoomCleared(RoomController room)
        {
            // The final room's clear is the stage completing; that has its own fanfare.
            if (room != null && room.IsFinalRoom) return;
            Play(Sound.RoomClear);
        }

        private void OnStageCompleted(StageData stage) => Play(Sound.StageComplete);
        private void OnStageFailed(StageData stage) => Play(Sound.StageFailed);
        private void OnStageUnloaded(StageData stage) => UnbindBoss();

        // ------------------------------------------------------------------ ui

        private void OnButtonPressed() => Play(Sound.Click, 0.6f);
    }
}
