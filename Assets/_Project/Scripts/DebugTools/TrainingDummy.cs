using System.Collections;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.DebugTools
{
    /// <summary>
    /// A stationary target for testing combat before real enemies exist.
    ///
    /// It has no AI and never fights back - it only proves that damage, defense, crit,
    /// line of sight and the once-per-swing rule all behave. Real enemies arrive next phase
    /// and will reuse the same Health and stat-provider components this dummy uses.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private bool respawnAfterDeath = true;
        [SerializeField, Min(0.1f)] private float respawnDelay = 3f;
        [SerializeField] private bool showHealthLabel = true;
        [SerializeField] private string label = "Dummy";

        private Health _health;
        private SpriteRenderer _renderer;
        private Collider2D _collider;
        private Camera _camera;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
        }

        private void OnEnable() => _health.Died += OnDied;
        private void OnDisable() => _health.Died -= OnDied;

        private void OnDied(GameObject killer)
        {
            if (_renderer != null) _renderer.enabled = false;
            if (_collider != null) _collider.enabled = false;

            if (respawnAfterDeath) StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);

            _health.ResetToFull();
            if (_renderer != null) _renderer.enabled = true;
            if (_collider != null) _collider.enabled = true;
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!showHealthLabel || !_health.IsAlive) return;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 screenPoint = _camera.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
            if (screenPoint.z < 0f) return;   // Behind the camera.

            var rect = new Rect(screenPoint.x - 70f, Screen.height - screenPoint.y - 12f, 140f, 24f);
            GUI.Label(rect, $"{label}  {_health.CurrentHealth:0}/{_health.MaxHealth:0}",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
#endif
        }
    }
}
