using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stages
{
    /// <summary>
    /// Sits on the root of a stage prefab and runs that stage: start the first room, open the
    /// way onward as rooms clear, and report completion.
    ///
    /// It is the only script that has to exist in a stage layout. Everything else about a
    /// stage - walls, rooms, doors, spawn points, hazards - is authored visually as children
    /// of this object, which is what makes new stages a content job rather than a code job.
    /// </summary>
    public class StageController : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Every room in this stage. Exactly one should be marked Is Start Room.")]
        [SerializeField] private List<RoomController> rooms = new List<RoomController>();

        [Tooltip("Where the player is placed when the stage begins.")]
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Camera")]
        [Tooltip("Arena bounds the camera is clamped to while this stage is loaded. " +
                 "Set Size to zero to leave the camera unclamped.")]
        [SerializeField] private Vector2 cameraBoundsCenter = Vector2.zero;
        [SerializeField] private Vector2 cameraBoundsSize = new Vector2(50f, 50f);

        [SerializeField] private bool logProgress = true;

        private readonly List<RoomExit> _trackedExits = new List<RoomExit>();
        private int _enemyLevel = 1;
        private bool _completed;

        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Vector2 CameraBoundsCenter => cameraBoundsCenter;
        public Vector2 CameraBoundsSize => cameraBoundsSize;
        public IReadOnlyList<RoomController> Rooms => rooms;

        /// <summary>Raised when the final room is cleared. StageManager listens.</summary>
        public event Action<StageController> Completed;

        /// <summary>Raised for every room cleared, including the last.</summary>
        public event Action<RoomController> RoomCleared;

        /// <summary>Called by StageManager once the stage prefab has been instantiated.</summary>
        public void Begin(int enemyLevel)
        {
            _enemyLevel = Mathf.Max(1, enemyLevel);
            _completed = false;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomController room = rooms[i];
                if (room == null) continue;

                room.Cleared += OnRoomCleared;

                foreach (RoomExit exit in room.GetComponentsInChildren<RoomExit>(true))
                {
                    exit.Entered += OnExitEntered;
                    _trackedExits.Add(exit);
                }
            }

            RoomController startRoom = FindStartRoom();
            if (startRoom == null)
            {
                Debug.LogError($"[Stage {name}] No room is marked Is Start Room.", this);
                return;
            }

            startRoom.Activate(_enemyLevel);
            if (logProgress) Debug.Log($"[Stage {name}] Started in room '{startRoom.name}'.", this);
        }

        private RoomController FindStartRoom()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null && rooms[i].IsStartRoom) return rooms[i];
            }

            // Falling back to the first room keeps a half-authored stage testable.
            return rooms.Count > 0 ? rooms[0] : null;
        }

        private void OnRoomCleared(RoomController room)
        {
            if (logProgress) Debug.Log($"[Stage {name}] Room '{room.name}' cleared.", this);
            RoomCleared?.Invoke(room);

            if (room.IsFinalRoom) Complete();
        }

        private void OnExitEntered(RoomExit exit)
        {
            RoomController next = exit.RoomToActivate;
            if (next == null || next.State != RoomController.RoomState.Inactive) return;

            if (logProgress) Debug.Log($"[Stage {name}] Entering room '{next.name}'.", this);
            next.Activate(_enemyLevel);
        }

        private void Complete()
        {
            if (_completed) return;
            _completed = true;

            if (logProgress) Debug.Log($"[Stage {name}] COMPLETE.", this);
            Completed?.Invoke(this);
        }

        /// <summary>Debug tool support: clears every active room and completes the stage.</summary>
        public void ForceComplete()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null && rooms[i].State != RoomController.RoomState.Cleared)
                {
                    rooms[i].ForceClear();
                }
            }

            Complete();
        }

        private void OnDrawGizmosSelected()
        {
            if (cameraBoundsSize.x <= 0f || cameraBoundsSize.y <= 0f) return;

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position + (Vector3)cameraBoundsCenter, cameraBoundsSize);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i] != null) rooms[i].Cleared -= OnRoomCleared;
            }

            for (int i = 0; i < _trackedExits.Count; i++)
            {
                if (_trackedExits[i] != null) _trackedExits[i].Entered -= OnExitEntered;
            }
        }
    }
}
