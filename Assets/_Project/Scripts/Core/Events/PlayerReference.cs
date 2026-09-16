using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Core.Events
{
    /// <summary>
    /// A shared handle to "the player", written once when the player spawns and read by
    /// everything that needs to find them - enemy perception, the camera, spawners.
    ///
    /// This replaces FindObjectOfType/GameObject.Find, which the design spec bans during
    /// gameplay: with fifty enemies on screen, per-frame scene searches are a real mobile
    /// cost. It is also not a singleton - it is an asset, so it can be swapped in tests and
    /// nothing takes a static dependency on a MonoBehaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerReference", menuName = "RPG/Core/Player Reference")]
    public class PlayerReference : ScriptableObject
    {
        public Transform Transform { get; private set; }
        public GameObject GameObject { get; private set; }
        public IDamageable Damageable { get; private set; }
        public Health Health { get; private set; }

        public bool Exists => Transform != null;
        public Vector2 Position => Transform != null ? (Vector2)Transform.position : Vector2.zero;

        public void Register(GameObject player)
        {
            GameObject = player;
            Transform = player != null ? player.transform : null;
            Health = player != null ? player.GetComponent<Health>() : null;
            Damageable = Health;
        }

        public void Unregister(GameObject player)
        {
            // Guard against a stale unregister from an object that was already replaced.
            if (GameObject != player) return;
            Clear();
        }

        private void Clear()
        {
            GameObject = null;
            Transform = null;
            Health = null;
            Damageable = null;
        }

        // Runtime state in an asset survives exiting Play Mode in the Editor, so it is
        // explicitly cleared on load and unload.
        private void OnEnable() => Clear();
        private void OnDisable() => Clear();
    }
}
