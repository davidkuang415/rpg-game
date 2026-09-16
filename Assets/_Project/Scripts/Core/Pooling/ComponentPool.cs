using UnityEngine;
using UnityEngine.Pool;

namespace RPG.Core.Pooling
{
    /// <summary>
    /// Reusable object pool for any component prefab.
    ///
    /// The design spec calls for pooling every frequently spawned object - arrows, enemy
    /// projectiles, damage numbers, XP particles, combat effects. Rather than one hand-written
    /// pool per type, each of those gets a tiny subclass of this.
    ///
    /// Generic MonoBehaviours cannot be added to a GameObject directly, so subclasses are
    /// concrete: `public class ProjectilePool : ComponentPool&lt;Projectile&gt;`.
    /// </summary>
    public abstract class ComponentPool<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private T prefab;

        [Tooltip("Instances created up front, before any are needed.")]
        [SerializeField, Min(0)] private int prewarmCount = 16;

        [Tooltip("Above this many idle instances, extras are destroyed instead of kept.")]
        [SerializeField, Min(1)] private int maxPoolSize = 64;

        private ObjectPool<T> _pool;

        protected virtual void Awake()
        {
            if (prefab == null)
            {
                Debug.LogError($"{GetType().Name} on '{name}' has no prefab assigned.", this);
                enabled = false;
                return;
            }

            _pool = new ObjectPool<T>(
                createFunc: CreateInstance,
                actionOnGet: instance => instance.gameObject.SetActive(true),
                actionOnRelease: instance => instance.gameObject.SetActive(false),
                actionOnDestroy: instance => Destroy(instance.gameObject),
                collectionCheck: true,
                defaultCapacity: prewarmCount,
                maxSize: maxPoolSize);

            Prewarm();
        }

        private T CreateInstance()
        {
            T instance = Instantiate(prefab, transform);
            OnInstanceCreated(instance);
            return instance;
        }

        /// <summary>Hook for handing a new instance its way back home.</summary>
        protected abstract void OnInstanceCreated(T instance);

        private void Prewarm()
        {
            if (prewarmCount <= 0) return;

            var warmed = new T[prewarmCount];
            for (int i = 0; i < prewarmCount; i++) warmed[i] = _pool.Get();
            for (int i = 0; i < prewarmCount; i++) _pool.Release(warmed[i]);
        }

        public T Get() => _pool.Get();

        public void Release(T instance)
        {
            if (instance != null) _pool.Release(instance);
        }
    }
}
