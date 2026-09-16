# Phase 9 — Reference Card

## Run
`RPG > Phase 9 > Add Completion Screen`

## The ordering that matters
```
StageController: final room cleared
  -> StageEventChannel.StageCompleted
       -> StageCompleteScreen.OnStageCompleted
            1. copy pending rewards for DISPLAY
            2. RewardClaimer.ClaimPending()      <-- banked here, before any animation
            3. Show() -> animate from the snapshot
       -> GameFlowController: unload the arena behind the screen, wait for Continue
```
XP was already awarded live, per kill. Gold and items are banked the instant the screen opens.
The animation is a replay of something already permanent - quitting mid-animation costs nothing.

## XP bar
Two stacked filled Images inside one bar:
- `XpFillPending` (dark blue) - drawn first, sits at `(current + earned) / required`
- `XpFillCurrent` (cyan) - drawn on top, animates from `current` upward, eating the dark portion

On reaching a full bar: level label increments, "LEVEL UP!" appears for `levelUpPause`, both
fills reset, and the remainder keeps filling. Several level-ups play in sequence.

All timing uses **unscaled** time, because ModalPanel pauses the game while a screen is open.

## What the screen shows
Stage name - level before -> after - XP gained - gold gained - gems balance - every item
obtained (rarity-coloured tiles, revealed one at a time).

## Timing fields (on StageCompletePanel)
| Field | Default | Meaning |
|---|---|---|
| Seconds Per Full Bar | 1.1 | time to fill one whole level; partial fills scale down |
| Delay Before Xp | 0.35 | beat before the bar starts |
| Level Up Pause | 0.45 | how long "LEVEL UP!" holds |
| Item Reveal Stagger | 0.12 | delay between item tiles |

Pressing CONTINUE during the animation skips it rather than being ignored.

## Notes
- `RewardClaimer.claimOnStageComplete` is now OFF - the screen claims instead, so rewards are
  never claimed twice.
- The "level before" snapshot is taken on StageStarted, but skipped on a death-retry so it
  stays anchored to the original attempt (matching the rewards, which also carry over).
- Displayed XP comes from `StageRewards.XpEarned`; if you change `ExperienceCollector`'s
  xpMultiplier away from 1, the displayed total will differ from what was actually awarded.
