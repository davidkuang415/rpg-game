using UnityEngine;
using RPG.Stats;

namespace RPG.Core.Combat
{
    /// <summary>
    /// A hand-tuned stat source for things that do not need the full stat pipeline:
    /// training dummies now, destructible props later.
    ///
    /// Real enemies get EnemyStats in the next phase, which will scale with enemy level.
    /// This is not that - it is a fixed block of numbers typed into the Inspector.
    /// </summary>
    public class SimpleStatProvider : MonoBehaviour, ICombatStatProvider
    {
        [SerializeField, Min(1f)] private float maxHealth = 50f;
        [SerializeField, Min(0f)] private float attack = 5f;
        [SerializeField, Min(0f)] private float defense = 0f;
        [SerializeField, Range(0f, 1f)] private float dodgeChance = 0f;

        public float GetStat(StatType stat) => stat switch
        {
            StatType.MaxHealth => maxHealth,
            StatType.Attack => attack,
            StatType.Defense => defense,
            StatType.DodgeChance => dodgeChance,
            StatType.CritDamage => 1.5f,
            _ => 0f
        };

        /// <summary>Lets debug tools and the setup tool configure a dummy from code.</summary>
        public void Configure(float health, float armour)
        {
            maxHealth = Mathf.Max(1f, health);
            defense = Mathf.Max(0f, armour);
        }
    }
}
