using UnityEngine;

namespace RPG.Combat.Projectiles
{
    /// <summary>
    /// A shared handle to a live projectile pool.
    ///
    /// A prefab cannot hold a reference to an object in a scene, which matters now that
    /// enemies are spawned from stage prefabs rather than placed by hand. The pool publishes
    /// itself into this asset at runtime, and prefabs reference the asset instead.
    ///
    /// Same pattern as PlayerReference: an asset, not a singleton, so tests and alternate
    /// scenes can supply their own.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectilePoolReference", menuName = "RPG/Combat/Projectile Pool Reference")]
    public class ProjectilePoolReference : ScriptableObject
    {
        public ProjectilePool Current { get; private set; }

        public void Register(ProjectilePool pool) => Current = pool;

        public void Unregister(ProjectilePool pool)
        {
            if (Current == pool) Current = null;
        }

        private void OnEnable() => Current = null;
        private void OnDisable() => Current = null;
    }
}
