using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// Every stage in the game, in order.
    ///
    /// Stage progression is looked up by NUMBER rather than by list index, so stages can be
    /// reordered or inserted in the asset list without changing what "stage 7" means to a
    /// save file.
    /// </summary>
    [CreateAssetMenu(fileName = "StageRegistry", menuName = "RPG/Stages/Stage Registry")]
    public class StageRegistry : ScriptableObject
    {
        [SerializeField] private List<StageData> stages = new List<StageData>();

        public IReadOnlyList<StageData> Stages => stages;
        public int Count => stages.Count;

        public StageData GetByNumber(int stageNumber)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && stages[i].StageNumber == stageNumber) return stages[i];
            }
            return null;
        }

        /// <summary>The stage after this one, or null if this is the last built stage.</summary>
        public StageData GetNext(int stageNumber) => GetByNumber(stageNumber + 1);

        public int HighestStageNumber
        {
            get
            {
                int highest = 0;
                for (int i = 0; i < stages.Count; i++)
                {
                    if (stages[i] != null) highest = Mathf.Max(highest, stages[i].StageNumber);
                }
                return highest;
            }
        }

        private void OnValidate()
        {
            var seen = new HashSet<int>();
            foreach (StageData stage in stages)
            {
                if (stage == null) continue;
                if (!seen.Add(stage.StageNumber))
                {
                    Debug.LogError($"StageRegistry '{name}': duplicate stage number {stage.StageNumber}.", this);
                }
            }
        }
    }
}
