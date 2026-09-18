using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>
    /// Marks an enemy as belonging to a pool, and remembers which prefab it came from.
    ///
    /// Added by the pool at spawn rather than authored on the prefab, so an enemy dropped into a
    /// scene by hand is simply not pooled and destroys itself as before.
    /// </summary>
    public class PooledEnemy : MonoBehaviour
    {
        private EnemyPool _pool;
        private GameObject _prefab;

        public GameObject Prefab => _prefab;

        public void Bind(EnemyPool pool, GameObject prefab)
        {
            _pool = pool;
            _prefab = prefab;
        }

        /// <summary>Returns to the pool. False when there is no pool to return to.</summary>
        public bool ReturnToPool()
        {
            if (_pool == null) return false;

            _pool.Release(this);
            return true;
        }
    }
}
