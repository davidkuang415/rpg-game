using UnityEngine;

namespace RPG.Core.Events
{
    /// <summary>
    /// Publishes this GameObject into the shared PlayerReference for its lifetime.
    ///
    /// A separate one-job component rather than another responsibility bolted onto
    /// PlayerController, so the player prefab can be used in scenes and tests that do not
    /// want the registration at all - just remove the component.
    /// </summary>
    public class PlayerRegistrar : MonoBehaviour
    {
        [SerializeField] private PlayerReference playerReference;

        private void OnEnable()
        {
            if (playerReference == null)
            {
                Debug.LogError($"{nameof(PlayerRegistrar)} on '{name}' has no Player Reference asset.", this);
                return;
            }

            playerReference.Register(gameObject);
        }

        private void OnDisable()
        {
            if (playerReference != null) playerReference.Unregister(gameObject);
        }
    }
}
