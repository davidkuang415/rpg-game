using UnityEngine;
using RPG.Core.Combat;

namespace RPG.Enemies
{
    /// <summary>
    /// The enemy state machine: decide, each frame, whether to hold, hunt, or swing.
    ///
    ///   Idle   - nothing seen, nothing remembered. Stand still.
    ///   Chase  - move toward the player, or toward where they were last seen.
    ///   Attack - in range with line of sight; stop and commit to an attack.
    ///
    /// It owns no combat or movement code of its own; it only chooses between the components
    /// that do. That is what lets Melee, Ranged and Tank enemies share one brain and differ
    /// only in which attack component they carry and what their EnemyData says.
    /// </summary>
    [RequireComponent(typeof(EnemyStats))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyMotor))]
    public class EnemyBrain : MonoBehaviour
    {
        public enum State { Idle, Chase, Attack }

        [Tooltip("Layers that block attacks. Must match the attack component's blocking layers.")]
        [SerializeField] private LayerMask blockingLayers;

        [Tooltip("Attack only when this much closer than max range, so the enemy does not " +
                 "hover exactly on the boundary starting and cancelling attacks.")]
        [SerializeField, Range(0.5f, 1f)] private float attackRangeFactor = 0.9f;

        [SerializeField] private bool logStateChanges;

        private EnemyPerception _perception;
        private EnemyMotor _motor;
        private EnemyAttackBase _attack;
        private Health _health;

        public State Current { get; private set; } = State.Idle;

        private void Awake()
        {
            _perception = GetComponent<EnemyPerception>();
            _motor = GetComponent<EnemyMotor>();
            _attack = GetComponent<EnemyAttackBase>();
            _health = GetComponent<Health>();

            if (_attack == null)
            {
                Debug.LogWarning($"{nameof(EnemyBrain)} on '{name}' has no attack component; " +
                                 "it will chase but never attack.", this);
            }
        }

        private void Update()
        {
            if (_health != null && !_health.IsAlive) return;

            // Committed to a swing: hold position until it resolves. This is what makes the
            // telegraph meaningful - the enemy cannot windup and reposition at the same time.
            if (_attack != null && _attack.IsAttacking)
            {
                _motor.Stop();
                SetState(State.Attack);
                return;
            }

            if (_perception.HasVisual)
            {
                TickVisible();
                return;
            }

            if (_perception.HasMemory)
            {
                TickInvestigating();
                return;
            }

            _motor.Stop();
            SetState(State.Idle);
        }

        private void TickVisible()
        {
            Vector2 playerPosition = _perception.LastKnownPosition;
            Vector2 selfPosition = transform.position;

            float attackRange = _attack != null ? _attack.AttackRange * attackRangeFactor : 0f;
            bool inRange = (playerPosition - selfPosition).sqrMagnitude <= attackRange * attackRange;

            if (inRange && _attack != null && _attack.IsReady &&
                CombatQueries.HasLineOfSight(selfPosition, playerPosition, blockingLayers))
            {
                _motor.Stop();
                SetState(State.Attack);
                _attack.TryAttack(playerPosition);
                return;
            }

            // In range but still on cooldown: hold rather than shove into the player.
            if (inRange) _motor.Stop();
            else _motor.MoveTo(playerPosition);

            SetState(inRange ? State.Attack : State.Chase);
        }

        private void TickInvestigating()
        {
            Vector2 lastKnown = _perception.LastKnownPosition;

            if (_motor.HasArrived(lastKnown))
            {
                // Reached the spot and the player is not here. Wait for memory to lapse.
                _motor.Stop();
                SetState(State.Idle);
                return;
            }

            _motor.MoveTo(lastKnown);
            SetState(State.Chase);
        }

        private void SetState(State next)
        {
            if (Current == next) return;
            Current = next;
            if (logStateChanges) Debug.Log($"[{name}] -> {next}", this);
        }
    }
}
