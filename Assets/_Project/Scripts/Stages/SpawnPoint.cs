using UnityEngine;
using RPG.Enemies;

namespace RPG.Stages
{
    /// <summary>
    /// A marked position where one enemy appears.
    ///
    /// This is the core level-building tool: drop an empty GameObject into a stage prefab,
    /// add this component, pick an EnemyData asset, and it is done. The gizmo shows what will
    /// spawn and at what level, so a room can be read at a glance in the Scene view without
    /// entering Play Mode.
    ///
    /// A spawn point never references a prefab directly - it asks the EnemyData for it, so
    /// changing an enemy's prefab updates every stage that uses it.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("What To Spawn")]
        [SerializeField] private EnemyData enemyData;

        [Tooltip("Optional: use this prefab instead of the one on the EnemyData asset. " +
                 "For one-off variants; normally leave empty.")]
        [SerializeField] private GameObject prefabOverride;

        [Header("Instance")]
        [Tooltip("Added to the stage's enemy level. Use +1 or +2 to make one spawn tougher " +
                 "than the rest of the room without a separate enemy type.")]
        [SerializeField] private int levelOffset;

        [Tooltip("Spawns as an Elite. Elite modifiers are not implemented yet, but the flag " +
                 "is carried through so stages can already be authored with them.")]
        [SerializeField] private bool spawnAsElite;

        [Tooltip("Optional. Where spawned enemies come from and return to. Without it every " +
                 "enemy is created with Instantiate and destroyed on death, as before.")]
        [SerializeField] private EnemyPoolReference enemyPool;

        [Header("Gizmo")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.35f, 0.35f, 0.9f);
        [SerializeField, Min(0.1f)] private float gizmoRadius = 0.5f;

        public EnemyData EnemyData => enemyData;
        public bool SpawnAsElite => spawnAsElite;

        /// <summary>
        /// Creates the enemy and configures it for this stage's difficulty.
        /// Returns null (with a warning) if the spawn point is not fully authored.
        /// </summary>
        public GameObject Spawn(int stageEnemyLevel, Transform parent)
        {
            GameObject prefab = prefabOverride != null
                ? prefabOverride
                : (enemyData != null ? enemyData.EnemyPrefab : null);

            if (prefab == null)
            {
                Debug.LogWarning($"[SpawnPoint] '{name}' has no enemy prefab to spawn " +
                                 "(check its EnemyData's Enemy Prefab field).", this);
                return null;
            }

            // Pooled when a pool has published itself, created outright when one has not. The
            // fallback matters: a scene with no pool must still spawn enemies.
            EnemyPool pool = enemyPool != null ? enemyPool.Current : null;

            GameObject instance = pool != null
                ? pool.Get(prefab, transform.position, parent)
                : Instantiate(prefab, transform.position, Quaternion.identity, parent);

            if (instance == null) return null;

            var stats = instance.GetComponent<EnemyStats>();
            if (stats != null && enemyData != null)
            {
                stats.Configure(enemyData, Mathf.Max(1, stageEnemyLevel + levelOffset), spawnAsElite);
            }

            // After Configure, never before: the health reset has to see the level this enemy is
            // being spawned at, not the one the previous occupant of this object died with.
            var controller = instance.GetComponent<EnemyController>();
            if (controller != null) controller.ResetForReuse();

            return instance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

            // A small cross makes an empty spawn point visible even when zoomed out.
            Gizmos.DrawLine(transform.position + Vector3.left * gizmoRadius,
                transform.position + Vector3.right * gizmoRadius);
            Gizmos.DrawLine(transform.position + Vector3.down * gizmoRadius,
                transform.position + Vector3.up * gizmoRadius);

#if UNITY_EDITOR
            string label = enemyData != null ? enemyData.DisplayName : "<no enemy>";
            if (levelOffset != 0) label += levelOffset > 0 ? $" +{levelOffset}" : $" {levelOffset}";
            if (spawnAsElite) label += " [Elite]";

            UnityEditor.Handles.color = gizmoColor;
            UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoRadius + 0.25f), label);
#endif
        }
    }
}
