# Phase 10 — Reference Card

## Run
`RPG > Phase 10 > Add Death Handling And Saving`

## Death rule (§36)
| Requirement | How it holds |
|---|---|
| Restart the current stage | `StageFailureHandler.Retry()` -> `StageManager.RestartCurrentStage()` |
| Next stage does NOT unlock | unlocking only ever happens in StageManager's *completion* handler |
| Earlier stages stay unlocked | `StageProgressState.UnlockUpTo` never moves backwards |
| Keep XP earned | XP is banked per kill, the moment the enemy dies |
| Keep gold and loot | pending rewards are only emptied by an explicit claim |

The whole rule lives in `StageFailureHandler`, so changing it later (lose gold, revive cost,
run-ending penalty) is one local edit.

Flow: player HP hits 0 -> input disabled -> 0.9s beat -> StageFailed event -> DEFEATED screen
showing what the attempt banked -> RETRY (reload stage, full heal) or LEAVE (back to stage list).

## Save system (§45)
```
SaveManager  -- JSON string -->  ISaveStorage
                                   LocalFileSaveStorage  (persistentDataPath/profile.json)
                                   [a cloud implementation drops in here later]
```
- `SaveData.CurrentVersion = 1`, checked and migrated on load; a NEWER save refuses to load
  rather than being overwritten by an older build.
- Stores stable IDs, never ScriptableObject references: `"knight"`, `"iron_sword"`.
- Atomic writes (temp file, then move) so a crash mid-write cannot corrupt the profile.
- Autosaves on stage completion, on app pause, and on quit.

### What is saved
class id, level, XP, highest unlocked stage, gold, gems, inventory capacity + expansions
bought, every inventory item instance (level, rarity, upgrade level, enchantments), equipped
items per slot, settings, and an empty Reward Storage list reserved for Phase 14.

### Load-time validation
Equipment is re-validated exactly as a live equip would be. A Knight's saved bow, an item whose
template was deleted, or a slot that no longer exists degrades into "moved to the bag" or one
dropped item — never a broken profile or an illegal loadout.

## Startup
`GameFlowController.Start` loads the profile, then chooses the first screen: stage list for a
returning player, class select for a new one. Loading happens there (not in SaveManager's own
Start) because Unity does not order Start calls between components.

## Debug
Overlay has **Save / Load / Wipe** under "Profile". Wipe then Play to test the new-player path.
