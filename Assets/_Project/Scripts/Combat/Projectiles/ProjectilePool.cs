using UnityEngine;
using RPG.Core.Pooling;

namespace RPG.Combat.Projectiles
{
    /// <summary>
    /// Pool of arrows and enemy bolts.
    ///
    /// All the machinery lives in ComponentPool; this only teaches new projectiles how to
    /// return themselves, and optionally publishes itself so prefabs can find it.
    /// </summary>
    public class ProjectilePool : ComponentPool<Projectile>
    {
        [Tooltip("Optional: publish this pool into a shared reference asset, so spawned " +
                 "prefabs can find it without a scene reference.")]
        [SerializeField] private ProjectilePoolReference publishAs;

        protected override void Awake()
        {
            base.Awake();
            if (publishAs != null) publishAs.Register(this);
        }

        private void OnDestroy()
        {
            if (publishAs != null) publishAs.Unregister(this);
        }

        protected override void OnInstanceCreated(Projectile instance) => instance.BindPool(this);
    }
}
