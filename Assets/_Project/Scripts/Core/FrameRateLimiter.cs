using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Caps how fast the game is allowed to render.
    ///
    /// Without a cap a simple 2D scene renders as fast as the GPU can go - many hundreds of
    /// frames per second on a desktop, and on a phone as many as the display allows - which
    /// is heat and battery spent on frames nobody can see. 144 covers every high-refresh
    /// display that matters while still stopping the runaway case.
    ///
    /// VSync is turned OFF deliberately: when it is on, Unity ignores targetFrameRate entirely
    /// and syncs to the display instead, so the cap would silently do nothing on a 240 Hz
    /// monitor. Leaving it off is what makes the number below the one that actually applies.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class FrameRateLimiter : MonoBehaviour
    {
        [Tooltip("Maximum frames per second. -1 removes the cap (not recommended).")]
        [SerializeField] private int targetFrameRate = 144;

        [Tooltip("Re-apply every few seconds. Some platforms reset the target after a resolution " +
                 "or focus change, and the cap must not quietly disappear when that happens.")]
        [SerializeField, Min(0f)] private float reapplyInterval = 5f;

        private float _nextReapply;

        public int TargetFrameRate => targetFrameRate;

        private void Awake() => Apply();

        private void OnApplicationFocus(bool focused)
        {
            if (focused) Apply();
        }

        private void Update()
        {
            if (reapplyInterval <= 0f || Time.unscaledTime < _nextReapply) return;

            if (Application.targetFrameRate != targetFrameRate || QualitySettings.vSyncCount != 0)
            {
                Apply();
            }

            _nextReapply = Time.unscaledTime + reapplyInterval;
        }

        /// <summary>Applies the cap now. Also called by settings UI later, when there is one.</summary>
        public void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            _nextReapply = Time.unscaledTime + reapplyInterval;
        }

        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Apply();
        }
    }
}
