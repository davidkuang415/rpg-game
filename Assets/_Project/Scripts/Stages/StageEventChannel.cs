using System;
using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// Broadcasts stage lifecycle events.
    ///
    /// The completion screen, reward accumulator, save system and audio all need to know when
    /// a stage starts, a room clears or a stage completes - and none of them should be
    /// referenced by the stage logic itself. They subscribe here instead.
    /// </summary>
    [CreateAssetMenu(fileName = "StageEventChannel", menuName = "RPG/Stages/Stage Event Channel")]
    public class StageEventChannel : ScriptableObject
    {
        public event Action<StageData> StageStarted;
        public event Action<RoomController> RoomCleared;
        public event Action<StageData> StageCompleted;
        public event Action<StageData> StageFailed;

        public void RaiseStageStarted(StageData stage) => StageStarted?.Invoke(stage);
        public void RaiseRoomCleared(RoomController room) => RoomCleared?.Invoke(room);
        public void RaiseStageCompleted(StageData stage) => StageCompleted?.Invoke(stage);
        public void RaiseStageFailed(StageData stage) => StageFailed?.Invoke(stage);

        private void OnDisable()
        {
            StageStarted = null;
            RoomCleared = null;
            StageCompleted = null;
            StageFailed = null;
        }
    }
}
