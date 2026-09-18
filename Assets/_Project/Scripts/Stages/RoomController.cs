using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Stages
{
    /// <summary>
    /// One room of a stage: it spawns its waves, tracks what is still alive, and reports when
    /// it has been cleared.
    ///
    /// Clearing is tracked by subscribing to the Health of the exact enemies this room spawned,
    /// rather than by counting global deaths. That way two rooms can be active at once, and an
    /// enemy that wandered in from elsewhere cannot accidentally satisfy a clear condition.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        /// <summary>
        /// A group of spawns that appear together. Waves run in order, each starting after the
        /// previous one is cleared - which is what "delayed waves" means in the design spec.
        /// </summary>
        [Serializable]
        public class Wave
        {
            [Tooltip("Label for the Inspector only.")]
            public string Name = "Wave";

            [Tooltip("Seconds to wait before this wave spawns, after the previous one is cleared.")]
            [Min(0f)] public float DelayBeforeSpawn;

            [Tooltip("Seconds between individual spawns within this wave.")]
            [Min(0f)] public float SpawnInterval = 0.15f;

            [Tooltip("Off means the room can be cleared without killing this wave (ambient enemies).")]
            public bool RequiredToClear = true;

            public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();
        }

        public enum RoomState { Inactive, Active, Cleared }

        [Header("Content")]
        [SerializeField] private List<Wave> waves = new List<Wave>();

        [Tooltip("Doors out of this room. They unlock when the room is cleared.")]
        [SerializeField] private List<RoomExit> exits = new List<RoomExit>();

        [Header("Behaviour")]
        [Tooltip("The room the stage starts in. Exactly one room per stage should have this on.")]
        [SerializeField] private bool isStartRoom;

        [Tooltip("Clearing this room completes the stage. Usually the last room.")]
        [SerializeField] private bool isFinalRoom;

        [SerializeField] private bool logProgress;

        private readonly HashSet<Health> _aliveEnemies = new HashSet<Health>();
        private Transform _enemyParent;
        private int _stageEnemyLevel = 1;
        private Coroutine _waveRoutine;

        public RoomState State { get; private set; } = RoomState.Inactive;
        public bool IsStartRoom => isStartRoom;
        public bool IsFinalRoom => isFinalRoom;
        public int AliveEnemyCount => _aliveEnemies.Count;

        /// <summary>1-based index of the wave currently spawning or being fought. 0 before the first.</summary>
        public int CurrentWave { get; private set; }

        /// <summary>Waves that must be cleared. Ambient (not required) waves are not counted.</summary>
        public int RequiredWaveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < waves.Count; i++) if (waves[i].RequiredToClear) count++;
                return count;
            }
        }

        /// <summary>Raised once, when every required enemy in the room is dead.</summary>
        public event Action<RoomController> Cleared;

        /// <summary>Raised whenever the number of living enemies changes. The combat HUD listens.</summary>
        public event Action<int> AliveCountChanged;

        /// <summary>(room, waveNumber, requiredWaveCount) as each required wave begins to spawn.</summary>
        public event Action<RoomController, int, int> WaveStarted;

        /// <summary>Starts the room. Called by StageController, not by the room itself.</summary>
        public void Activate(int stageEnemyLevel)
        {
            if (State != RoomState.Inactive) return;

            State = RoomState.Active;
            _stageEnemyLevel = stageEnemyLevel;
            CurrentWave = 0;

            if (_enemyParent == null)
            {
                var parent = new GameObject($"{name}_Enemies");
                parent.transform.SetParent(transform, false);
                _enemyParent = parent.transform;
            }

            CloseExits();
            _waveRoutine = StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                Wave wave = waves[i];

                if (wave.DelayBeforeSpawn > 0f) yield return new WaitForSeconds(wave.DelayBeforeSpawn);

                if (logProgress) Debug.Log($"[Room {name}] Spawning wave '{wave.Name}'.", this);

                if (wave.RequiredToClear)
                {
                    CurrentWave++;
                    WaveStarted?.Invoke(this, CurrentWave, RequiredWaveCount);
                }

                for (int s = 0; s < wave.SpawnPoints.Count; s++)
                {
                    SpawnPoint point = wave.SpawnPoints[s];
                    if (point == null) continue;

                    GameObject enemy = point.Spawn(_stageEnemyLevel, _enemyParent);
                    if (enemy != null && wave.RequiredToClear) Track(enemy);

                    if (wave.SpawnInterval > 0f) yield return new WaitForSeconds(wave.SpawnInterval);
                }

                // Hold here until this wave is dead before starting the next one.
                if (wave.RequiredToClear)
                {
                    while (_aliveEnemies.Count > 0) yield return null;
                }
            }

            MarkCleared();
        }

        private void Track(GameObject enemy)
        {
            var health = enemy.GetComponent<Health>();
            if (health == null) return;

            _aliveEnemies.Add(health);
            health.Died += OnTrackedEnemyDied;
            AliveCountChanged?.Invoke(_aliveEnemies.Count);
        }

        private void OnTrackedEnemyDied(GameObject killer)
        {
            // The event does not say which Health raised it, so prune everything that is gone.
            _aliveEnemies.RemoveWhere(health => health == null || !health.IsAlive);
            AliveCountChanged?.Invoke(_aliveEnemies.Count);

            if (logProgress) Debug.Log($"[Room {name}] {_aliveEnemies.Count} enemies remaining.", this);
        }

        private void MarkCleared()
        {
            if (State == RoomState.Cleared) return;

            State = RoomState.Cleared;
            OpenExits();

            if (logProgress) Debug.Log($"[Room {name}] Cleared.", this);
            Cleared?.Invoke(this);
        }

        private void OpenExits()
        {
            for (int i = 0; i < exits.Count; i++)
            {
                if (exits[i] != null) exits[i].Open();
            }
        }

        private void CloseExits()
        {
            for (int i = 0; i < exits.Count; i++)
            {
                if (exits[i] != null) exits[i].Close();
            }
        }

        /// <summary>Clears the room instantly. Debug tool support ("complete stage").</summary>
        public void ForceClear()
        {
            if (_waveRoutine != null) StopCoroutine(_waveRoutine);

            foreach (Health health in _aliveEnemies)
            {
                if (health != null && health.IsAlive) Destroy(health.gameObject);
            }
            _aliveEnemies.Clear();

            MarkCleared();
        }

        private void OnDisable()
        {
            // Unsubscribe so a destroyed room cannot be resurrected by a late death event.
            foreach (Health health in _aliveEnemies)
            {
                if (health != null) health.Died -= OnTrackedEnemyDied;
            }
        }
    }
}
