using System;
using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// Which stages the player has unlocked.
    ///
    /// Held in memory for now and reset each play session. When the save system arrives it
    /// will load into and save out of this object - the rest of the game already only talks
    /// to this interface, so nothing else will need to change.
    /// </summary>
    [CreateAssetMenu(fileName = "StageProgress", menuName = "RPG/Stages/Stage Progress State")]
    public class StageProgressState : ScriptableObject
    {
        [Tooltip("Stage the player starts with access to. Stage 1 is always unlocked.")]
        [SerializeField, Min(1)] private int startingUnlockedStage = 1;

        /// <summary>Highest stage number the player may enter.</summary>
        public int HighestUnlockedStage { get; private set; } = 1;

        public event Action<int> ProgressChanged;

        public bool IsUnlocked(int stageNumber) => stageNumber <= HighestUnlockedStage;

        /// <summary>
        /// Unlocks up to the given stage. Never moves progression backwards, so replaying an
        /// early stage cannot cost the player access to later ones.
        /// </summary>
        public void UnlockUpTo(int stageNumber)
        {
            if (stageNumber <= HighestUnlockedStage) return;

            HighestUnlockedStage = stageNumber;
            ProgressChanged?.Invoke(HighestUnlockedStage);
        }

        /// <summary>Used by the save system on load, and by debug tools.</summary>
        public void SetProgress(int highestUnlockedStage)
        {
            HighestUnlockedStage = Mathf.Max(1, highestUnlockedStage);
            ProgressChanged?.Invoke(HighestUnlockedStage);
        }

        private void OnEnable()
        {
            HighestUnlockedStage = Mathf.Max(1, startingUnlockedStage);
            ProgressChanged = null;
        }
    }
}
