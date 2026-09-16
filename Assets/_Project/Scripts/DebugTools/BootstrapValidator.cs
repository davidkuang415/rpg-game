using UnityEngine;
using RPG.Core;

namespace RPG.DebugTools
{
    /// <summary>
    /// Runs once when the scene starts and shouts about setup mistakes that would
    /// otherwise fail silently - most importantly missing physics layers, which make
    /// LayerMask.GetMask return 0 and quietly break every raycast in the combat system.
    ///
    /// Editor/development only; it costs nothing in a release build.
    /// </summary>
    public class BootstrapValidator : MonoBehaviour
    {
        [SerializeField] private bool validateLayers = true;
        [SerializeField] private bool validatePlayerTag = true;

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (validateLayers && !GameLayers.ValidateLayers(out string missing))
            {
                Debug.LogError($"[Bootstrap] Missing physics layers: {missing}. " +
                               "Add them in Project Settings > Tags and Layers.", this);
            }

            if (validatePlayerTag && GameObject.FindGameObjectWithTag("Player") == null)
            {
                Debug.LogWarning("[Bootstrap] No GameObject tagged 'Player' found in the scene.", this);
            }
#endif
        }
    }
}
