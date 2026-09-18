using System.Collections.Generic;
using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>
    /// Reuses enemies instead of creating and destroying them.
    ///
    /// Enemies were the one frequently spawned thing in the game still going through
    /// Instantiate/Destroy, which is also the most expensive thing to spawn: a multi-object rig
    /// with a Rigidbody2D, colliders, a health bar and several renderers. A six-enemy wave was
    /// six instantiation spikes and six destroys.
    ///
    /// One queue per prefab, because enemy types are different objects and cannot be swapped for
    /// each other. Anything the pool cannot serve falls back to Instantiate, so a missing pool
    /// degrades to the old behaviour rather than failing to spawn.
    /// </summary>
    public class EnemyPool : MonoBehaviour
    {
        [Tooltip("Publishes this pool so stage prefabs can find it without a scene reference.")]
        [SerializeField] private EnemyPoolReference publishAs;

        [Tooltip("Idle enemies kept per type. Above this, extras are destroyed rather than kept.")]
        [SerializeField, Min(1)] private int maxIdlePerPrefab = 16;

        [SerializeField] private bool logPooling;

        private readonly Dictionary<GameObject, Queue<PooledEnemy>> _idle =
            new Dictionary<GameObject, Queue<PooledEnemy>>();

        private void Awake()
        {
            if (publishAs != null) publishAs.Register(this);
        }

        private void OnDestroy()
        {
            if (publishAs != null) publishAs.Unregister(this);
        }

        /// <summary>Takes an enemy from the pool, or creates one if none are idle.</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Transform parent)
        {
            if (prefab == null) return null;

            if (_idle.TryGetValue(prefab, out Queue<PooledEnemy> queue) && queue.Count > 0)
            {
                PooledEnemy reused = queue.Dequeue();

                // A pooled object can still be destroyed underneath us by a scene unload, so
                // a null here means fall through and make a fresh one.
                if (reused != null)
                {
                    Transform reusedTransform = reused.transform;
                    reusedTransform.SetParent(parent, false);
                    reusedTransform.position = position;
                    reusedTransform.rotation = Quaternion.identity;

                    reused.gameObject.SetActive(true);

                    if (logPooling) Debug.Log($"[EnemyPool] Reused {prefab.name}.", this);
                    return reused.gameObject;
                }
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);

            var marker = instance.GetComponent<PooledEnemy>();
            if (marker == null) marker = instance.AddComponent<PooledEnemy>();
            marker.Bind(this, prefab);

            return instance;
        }

        /// <summary>Takes an enemy back. Called by the enemy itself once its death fade finishes.</summary>
        public void Release(PooledEnemy enemy)
        {
            if (enemy == null) return;

            GameObject prefab = enemy.Prefab;
            if (prefab == null)
            {
                Destroy(enemy.gameObject);
                return;
            }

            if (!_idle.TryGetValue(prefab, out Queue<PooledEnemy> queue))
            {
                queue = new Queue<PooledEnemy>();
                _idle[prefab] = queue;
            }

            if (queue.Count >= maxIdlePerPrefab)
            {
                Destroy(enemy.gameObject);
                return;
            }

            // Re-parented to the pool so a stage unload cannot take the idle enemies with it.
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(transform, false);
            queue.Enqueue(enemy);
        }
    }
}
