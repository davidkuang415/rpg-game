using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>
    /// A shared handle to the live enemy pool.
    ///
    /// Spawn points live inside stage PREFABS, and a prefab cannot hold a reference to an object
    /// in a scene. The pool publishes itself here at runtime and spawn points reference this
    /// asset instead - the same pattern as ProjectilePoolReference and PlayerReference.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPoolReference", menuName = "RPG/Enemies/Enemy Pool Reference")]
    public class EnemyPoolReference : ScriptableObject
    {
        public EnemyPool Current { get; private set; }

        public void Register(EnemyPool pool) => Current = pool;

        public void Unregister(EnemyPool pool)
        {
            if (Current == pool) Current = null;
        }

        private void OnEnable() => Current = null;
        private void OnDisable() => Current = null;
    }
}
