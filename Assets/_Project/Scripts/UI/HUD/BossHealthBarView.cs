using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RPG.Core.Combat;
using RPG.Enemies;
using RPG.Stages;

namespace RPG.UI.HUD
{
    /// <summary>
    /// The big bar across the top of the screen for a boss, with its name and its phase.
    ///
    /// A boss with only the same small bar under its feet as a grunt does not read as a boss.
    /// This binds to whichever enemy spawns flagged IsBoss, tracks its health and its
    /// BossEnrage phase, and gets out of the way when it dies or the stage ends.
    /// </summary>
    public class BossHealthBarView : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private EnemyEventChannel enemyEvents;
        [SerializeField] private StageEventChannel stageEvents;

        [Header("Widgets")]
        [Tooltip("Shown while a boss is alive. Defaults to this object.")]
        [SerializeField] private GameObject root;

        [SerializeField] private Text nameLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Image fill;

        [Header("Look")]
        [SerializeField] private Color calmColor = new Color(0.85f, 0.3f, 0.32f);
        [SerializeField] private Color enragedColor = new Color(1f, 0.55f, 0.15f);

        [Tooltip("Seconds the empty bar lingers after the kill before hiding.")]
        [SerializeField, Min(0f)] private float lingerAfterDeath = 0.8f;

        private Health _boss;
        private BossEnrage _enrage;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            if (root == null) root = gameObject;

            // Awake/OnDestroy, not OnEnable/OnDisable: hiding the bar deactivates this object,
            // and a bar that only listens while visible would never hear the boss spawn.
            if (enemyEvents != null) enemyEvents.EnemySpawned += OnEnemySpawned;
            if (stageEvents != null)
            {
                stageEvents.StageCompleted += OnStageEnded;
                stageEvents.StageFailed += OnStageEnded;
                // StageUnloaded, not StageStarted, covers a restart mid-boss: a boss in a stage's
                // first wave spawns before StageStarted is raised, and hiding on it would hide
                // the bar that spawn just bound.
                stageEvents.StageUnloaded += OnStageEnded;
            }
        }

        private void OnDestroy()
        {
            if (enemyEvents != null) enemyEvents.EnemySpawned -= OnEnemySpawned;
            if (stageEvents != null)
            {
                stageEvents.StageCompleted -= OnStageEnded;
                stageEvents.StageFailed -= OnStageEnded;
                stageEvents.StageUnloaded -= OnStageEnded;
            }
            Unbind();
        }

        private void Start()
        {
            if (_boss == null) root.SetActive(false);
        }

        private void OnDisable() => _hideRoutine = null;

        private void OnEnemySpawned(EnemyStats enemy)
        {
            if (enemy == null || enemy.Data == null || !enemy.Data.IsBoss) return;

            Bind(enemy);
        }

        private void OnStageEnded(StageData stage)
        {
            Unbind();
            root.SetActive(false);
        }

        private void Bind(EnemyStats enemy)
        {
            Unbind();

            _boss = enemy.GetComponent<Health>();
            if (_boss == null) return;

            _boss.HealthChanged += OnHealthChanged;
            _boss.Died += OnDied;

            _enrage = enemy.GetComponent<BossEnrage>();
            if (_enrage != null) _enrage.Enraged += OnEnraged;

            if (nameLabel != null) nameLabel.text = enemy.Data.DisplayName.ToUpperInvariant();
            SetPhase(_enrage != null && _enrage.IsEnraged);

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            root.SetActive(true);
            OnHealthChanged(_boss.CurrentHealth, _boss.MaxHealth);
        }

        private void Unbind()
        {
            if (_boss != null)
            {
                _boss.HealthChanged -= OnHealthChanged;
                _boss.Died -= OnDied;
                _boss = null;
            }

            if (_enrage != null)
            {
                _enrage.Enraged -= OnEnraged;
                _enrage = null;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (fill != null) fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        private void OnEnraged(BossEnrage boss) => SetPhase(true);

        private void SetPhase(bool enraged)
        {
            if (phaseLabel != null) phaseLabel.text = enraged ? "ENRAGED" : string.Empty;
            if (fill != null) fill.color = enraged ? enragedColor : calmColor;
        }

        private void OnDied(GameObject killer)
        {
            OnHealthChanged(0f, 1f);
            Unbind();

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterLinger());
        }

        private IEnumerator HideAfterLinger()
        {
            yield return new WaitForSeconds(lingerAfterDeath);
            root.SetActive(false);
            _hideRoutine = null;
        }
    }
}
