using UnityEngine;
using RPG.Classes;
using RPG.Items;

namespace RPG.Player.Combat
{
    /// <summary>
    /// Picks which weapon behaviour is live, based on the selected class's allowed weapon type.
    ///
    /// This is the seam that keeps combat data-driven: PlayerController asks the router to
    /// attack, and the router matches ClassData.AllowedWeaponType against the WeaponType each
    /// attack component declares. Adding a third class means adding its attack component and
    /// a ClassData asset - this script does not change.
    ///
    /// It is also where equipped-weapon switching will hook in later, since a weapon's type
    /// determines the behaviour just as the class's does today.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerAttackRouter : MonoBehaviour, IPlayerAttack
    {
        [SerializeField] private bool logSwitching;

        private PlayerStats _stats;
        private PlayerAttackBase[] _attacks;
        private PlayerAttackBase _active;

        public PlayerAttackBase Active => _active;
        public bool CanAttack => _active != null && _active.CanAttack;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _attacks = GetComponents<PlayerAttackBase>();

            if (_attacks.Length == 0)
            {
                Debug.LogError($"{nameof(PlayerAttackRouter)} on '{name}' found no attack components.", this);
            }
        }

        private void OnEnable() => _stats.ClassChanged += OnClassChanged;
        private void OnDisable() => _stats.ClassChanged -= OnClassChanged;

        // Applied again on Start because PlayerStats may have selected a class during Awake,
        // before this component subscribed.
        private void Start() => OnClassChanged(_stats.CurrentClass);

        private void OnClassChanged(ClassData classData)
        {
            if (classData == null) return;
            SetActiveWeapon(classData.AllowedWeaponType);
        }

        private void SetActiveWeapon(WeaponType weaponType)
        {
            _active = null;

            for (int i = 0; i < _attacks.Length; i++)
            {
                PlayerAttackBase attack = _attacks[i];
                bool matches = attack.WeaponType == weaponType;

                // Disabling rather than destroying keeps class switching instant and allocation free.
                attack.enabled = matches;
                if (matches) _active = attack;
            }

            if (_active == null)
            {
                Debug.LogError($"{nameof(PlayerAttackRouter)}: no attack component handles " +
                               $"weapon type '{weaponType}'.", this);
            }
            else if (logSwitching)
            {
                Debug.Log($"[AttackRouter] Active weapon behaviour: {_active.GetType().Name}", this);
            }
        }

        public bool TryAttack(Vector2 direction) => _active != null && _active.TryAttack(direction);
    }
}
