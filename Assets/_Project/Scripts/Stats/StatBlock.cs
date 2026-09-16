using System;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// A full set of stat values, one per StatType.
    ///
    /// Backed by an array rather than named fields so that math can be written once and run
    /// over every stat. Named properties exist for the common reads so gameplay code stays
    /// readable (stats.Current.MoveSpeed rather than stats.Current[StatType.MoveSpeed]).
    ///
    /// Instances are reused rather than reallocated - stat recalculation happens on equip,
    /// level-up and buff changes, and this is a mobile target.
    /// </summary>
    [Serializable]
    public class StatBlock
    {
        [SerializeField] private float[] values = new float[StatTypeInfo.Count];

        private float[] Values
        {
            get
            {
                // Guards against a stat being added to the enum after this was serialized.
                if (values == null || values.Length != StatTypeInfo.Count)
                {
                    Array.Resize(ref values, StatTypeInfo.Count);
                }
                return values;
            }
        }

        public float this[StatType stat]
        {
            get => Values[(int)stat];
            set => Values[(int)stat] = value;
        }

        public float MaxHealth => this[StatType.MaxHealth];
        public float Attack => this[StatType.Attack];
        public float Defense => this[StatType.Defense];
        public float AttackSpeed => this[StatType.AttackSpeed];
        public float MoveSpeed => this[StatType.MoveSpeed];
        public float CritChance => this[StatType.CritChance];
        public float CritDamage => this[StatType.CritDamage];

        public void Clear() => Array.Clear(Values, 0, Values.Length);

        public void CopyFrom(StatBlock other)
        {
            if (other == null) { Clear(); return; }
            Array.Copy(other.Values, Values, StatTypeInfo.Count);
        }

        public void Add(StatType stat, float amount) => Values[(int)stat] += amount;

        public StatBlock Clone()
        {
            var clone = new StatBlock();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
