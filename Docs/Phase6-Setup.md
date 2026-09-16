# Phase 6 — Reference Card

## Run
`RPG > Phase 6 > Add Stages And Rooms`

## Structural change
The arena is no longer part of the scene. Stages are PREFABS that StageManager instantiates:

```
TestArena.unity  (permanent)          Stage_01_Arena.prefab  (content)
  Player, HUD, Main Camera              StageController         <- the only required script
  GameSystems                             Environment           <- floor, walls
    StageManager                          PlayerSpawn
    GameFlowController                    Room_Main (RoomController)
    ExperienceCollector                     SpawnPoint x5
  StageRoot  <- stages load here
  ArrowPool / EnemyBoltPool / XpParticlePool
```

Player, HUD, pools and progression survive across stages - no scene loading.

## Flow
```
Class select -> Stage select -> StageManager.LoadStage()
  -> StageController.Begin(enemyLevel)
     -> start room Activate() -> waves spawn -> all required enemies dead
        -> room Cleared -> exits open -> player walks through -> next room activates
        -> final room cleared -> StageCompleted -> unlock stage N+1 -> stage select
```

## Example stages
| | Stage 1 | Stage 2 |
|---|---|---|
| Rooms | 1 | 2, joined by a door |
| Waves | 1 | Room A: 1, Room B: 2 (second delayed 1.5s) |
| Enemy level | 1 | 3 |
| Arena | 50 x 50 | 50 x 26 |

## Building your own stage (no code)
1. `RPG > Level Tools > Create New Stage` — makes the prefab + StageData, registers it, opens Prefab Mode
2. Select `Environment`, then `RPG > Level Tools > Add Wall` (repeat; move/scale in the Scene view)
3. Select the room, then `RPG > Level Tools > Add Spawn Point` (Ctrl/Cmd+Shift+S) — assign its Enemy Data
4. On the RoomController, add a Wave and drag the spawn points into it
5. Multi-room: `Add Room`, `Add Door`, set the door's Room To Activate, add the door to the first room's Exits
6. Set the StageController's Camera Bounds Size to your arena size

## Per-stage camera
`StageController.cameraBoundsSize` drives the camera clamp on load, so each stage carries its
own arena size instead of the camera hardcoding one.

## Not yet done
- Stage progress is in memory and resets each session (the save system will own it)
- Hazards: no components yet (Phase 16 in the spec's order, or earlier on request)
- Boss stages: `IsBossStage` is computed (every 10th) but nothing consumes it until Phase 15
- Loot/gold modifiers on StageData are carried but unused until Phase 7
