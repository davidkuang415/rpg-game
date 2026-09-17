using UnityEngine;
using RPG.Core.Pooling;

namespace RPG.Vfx
{
    /// <summary>
    /// Pool of impact sparks. Hits are the most frequent spawn in the game, so these are
    /// pooled from the start rather than instantiated per swing.
    /// </summary>
    public class HitSparkPool : ComponentPool<HitSpark>
    {
        [Tooltip("Publishes this pool so prefabs can request sparks without a scene reference.")]
        [SerializeField] private CombatFeedbackChannel publishAs;

        protected override void Awake()
        {
            base.Awake();
            if (publishAs != null) publishAs.RegisterSparks(this);
        }

        private void OnDestroy()
        {
            if (publishAs != null) publishAs.UnregisterSparks(this);
        }

        protected override void OnInstanceCreated(HitSpark instance) => instance.BindPool(this);
    }
}
