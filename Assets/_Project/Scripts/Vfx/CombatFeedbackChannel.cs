using System;
using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Vfx
{
    /// <summary>
    /// The one place anything asks for combat feedback: "a hit landed here, show it".
    ///
    /// An asset rather than a singleton, for the same reason as ProjectilePoolReference: the
    /// things that request feedback are prefabs (enemies, the player) and a prefab cannot hold
    /// a reference to an object in a scene. The pools live in the scene and publish themselves
    /// here at runtime; everyone else just references this asset.
    ///
    /// Every call is null-safe on purpose. Feedback is decoration - a scene with no pools wired
    /// up must still play correctly, just silently.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatFeedbackChannel", menuName = "RPG/Vfx/Combat Feedback Channel")]
    public class CombatFeedbackChannel : ScriptableObject
    {
        private HitSparkPool _sparks;
        private DamageNumberPool _numbers;

        /// <summary>
        /// (info, result, victim) for every hit any HitFeedbackEmitter reports, dodged or not.
        /// The sound layer listens here; it is the one place every hit in the game passes through.
        /// </summary>
        public event Action<DamageInfo, DamageResult, Transform> DamageShown;

        public void NotifyDamage(in DamageInfo info, in DamageResult result, Transform victim)
            => DamageShown?.Invoke(info, result, victim);

        public void RegisterSparks(HitSparkPool pool) => _sparks = pool;
        public void RegisterNumbers(DamageNumberPool pool) => _numbers = pool;

        public void UnregisterSparks(HitSparkPool pool)
        {
            if (_sparks == pool) _sparks = null;
        }

        public void UnregisterNumbers(DamageNumberPool pool)
        {
            if (_numbers == pool) _numbers = null;
        }

        /// <summary>A burst at the point of impact. Direction points away from the attacker.</summary>
        public void SpawnHitSpark(Vector2 point, Vector2 direction, bool critical)
        {
            if (_sparks == null) return;

            HitSpark spark = _sparks.Get();
            if (spark != null) spark.Play(point, direction, critical);
        }

        /// <summary>A floating number. Pass dodged = true to show a miss instead of an amount.</summary>
        public void SpawnDamageNumber(Vector2 point, float amount, bool critical, bool dodged)
        {
            if (_numbers == null) return;

            DamageNumber number = _numbers.Get();
            if (number != null) number.Play(point, amount, critical, dodged);
        }

        // Runtime state on an asset survives play sessions in the editor, so it is cleared on
        // both edges rather than trusted to be empty.
        private void OnEnable()
        {
            _sparks = null;
            _numbers = null;
            DamageShown = null;
        }

        private void OnDisable()
        {
            _sparks = null;
            _numbers = null;
            DamageShown = null;
        }
    }
}
