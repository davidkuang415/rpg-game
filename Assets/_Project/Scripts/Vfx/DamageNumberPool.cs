using UnityEngine;
using RPG.Core.Pooling;

namespace RPG.Vfx
{
    /// <summary>Pool of floating damage numbers, published on the shared feedback channel.</summary>
    public class DamageNumberPool : ComponentPool<DamageNumber>
    {
        [SerializeField] private CombatFeedbackChannel publishAs;

        protected override void Awake()
        {
            base.Awake();
            if (publishAs != null) publishAs.RegisterNumbers(this);
        }

        private void OnDestroy()
        {
            if (publishAs != null) publishAs.UnregisterNumbers(this);
        }

        protected override void OnInstanceCreated(DamageNumber instance) => instance.BindPool(this);
    }
}
