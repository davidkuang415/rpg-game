using System;
using UnityEngine;

namespace RPG.Enemies
{
    /// <summary>Everything a listener needs to know about a kill, captured at the moment of death.</summary>
    public readonly struct EnemyDeathInfo
    {
        public readonly EnemyData Data;
        public readonly int Level;
        public readonly bool IsElite;
        public readonly float XpReward;
        public readonly int GoldReward;
        public readonly Vector3 Position;
        public readonly GameObject Killer;

        public EnemyDeathInfo(EnemyData data, int level, bool isElite, float xpReward,
            int goldReward, Vector3 position, GameObject killer)
        {
            Data = data;
            Level = level;
            IsElite = isElite;
            XpReward = xpReward;
            GoldReward = goldReward;
            Position = position;
            Killer = killer;
        }
    }

    /// <summary>
    /// Broadcasts enemy deaths to whoever cares, without the enemy knowing who that is.
    ///
    /// This is the seam the next phases plug into: the XP system awards experience, the room
    /// controller counts remaining enemies, the loot generator rolls drops, and the VFX system
    /// spawns XP particles - all from this one event, none of them referenced by the enemy.
    ///
    /// An asset rather than a static event so subscriptions cannot leak across play sessions
    /// in the Editor, and so tests can hand systems their own channel.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyEventChannel", menuName = "RPG/Enemies/Enemy Event Channel")]
    public class EnemyEventChannel : ScriptableObject
    {
        public event Action<EnemyDeathInfo> EnemyDied;

        /// <summary>
        /// Raised once per spawn, after the enemy has been configured for its level. The boss
        /// health bar and the sound layer listen here; nothing in the enemy references them.
        /// </summary>
        public event Action<EnemyStats> EnemySpawned;

        public void RaiseEnemyDied(in EnemyDeathInfo info) => EnemyDied?.Invoke(info);
        public void RaiseEnemySpawned(EnemyStats enemy) => EnemySpawned?.Invoke(enemy);

        // Editor play sessions reuse the loaded asset, so stale subscribers are dropped here.
        private void OnDisable()
        {
            EnemyDied = null;
            EnemySpawned = null;
        }
    }
}
