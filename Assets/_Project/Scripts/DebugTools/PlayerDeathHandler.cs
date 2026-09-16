using System.Collections;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Player;

namespace RPG.DebugTools
{
    /// <summary>
    /// Temporary death handling so testing can continue when enemies actually kill you.
    ///
    /// Phase 10 replaces this with the real rule from the design spec: restart the current
    /// stage, keep XP, gold and loot earned during the failed attempt, and do not unlock the
    /// next stage. This version only respawns you on the spot so combat can be tested.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float respawnDelay = 2f;
        [SerializeField] private bool respawnAtStartPosition = true;

        private Health _health;
        private PlayerController _controller;
        private Vector3 _startPosition;
        private bool _dying;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _controller = GetComponent<PlayerController>();
            _startPosition = transform.position;
        }

        private void OnEnable() => _health.Died += OnDied;
        private void OnDisable() => _health.Died -= OnDied;

        private void OnDied(GameObject killer)
        {
            if (_dying) return;
            _dying = true;

            Debug.Log($"[Player] Died to '{(killer != null ? killer.name : "unknown")}'. " +
                      $"Respawning in {respawnDelay:0.#}s (temporary - Phase 10 restarts the stage).", this);

            if (_controller != null) _controller.SetInputEnabled(false);
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            if (respawnAtStartPosition) transform.position = _startPosition;

            _health.ResetToFull();
            if (_controller != null) _controller.SetInputEnabled(true);
            _dying = false;
        }
    }
}
