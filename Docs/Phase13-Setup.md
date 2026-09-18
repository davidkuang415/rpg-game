# Phase 13 — Reference Card

The QA fix pass: the hub screens are reachable in full, hits register right, enemies stop
allocating on every spawn, gold and gems both have somewhere to go, and nothing survives a quit
by accident.

## Run

Two tools, in this order:

1. `RPG > Phase 12 > Build Polish, Economy And Stages` - rebuilds the hub (it calls the Phase 11
   tool itself), which is what actually creates the tooltip panel, wires the scrollable pages and
   arms the RESET button's confirm step. Safe to re-run.
2. `RPG > Phase 13 > Apply QA Fix Pass` - wires the enemy pool, player targeting, first-clear
   bonus, save manager and stage reward curves. Safe to re-run. Checks for `GameSystems` and
   `Player` first, so run it after the hub exists.

Do this even if Phase 13 looks already applied in the scene - the hub rebuild in step 1 is new
and has not been run since the tooltip/scroll/confirm-button code was written.

## What changed, in one table

| Ask | Where it lives |
|---|---|
| Every page fully reachable | `CreateScrollContent` + `NestedPageScroll` on Stages/Gear/Bag |
| See an item's stats without selecting it | `ItemTooltip` (shared panel), `ItemTooltipTrigger` on bag and gear tiles |
| RESET can't be thumbed by accident | `ConfirmTapButton` on the debug reset button |
| Swiping between pages doesn't misfire | `SwipePageView` now measures its own drag velocity |
| Hold to keep attacking | `PlayerController.autoRepeatWhileHeld`, `PlayerInputChannel.HasBufferedPress` |
| Aim while retreating or circling | `PlayerTargeting` |
| Enemies stop costing an alloc spike per wave | `EnemyPool`, `PooledEnemy`, `EnemyPoolReference` |
| Gems have a second source | `FirstClearBonus` |
| Overflow loot survives a quit | `SaveManager` + `StageRewardCollector` |
| Early stages worth replaying | `FixStageRewardCurves` (escalating loot/gold per stage) |
| Upgrading gear is worth it | `ItemEconomyConfig`: bigger per-level bonus, cheaper growth, higher sell-back |
| `Player.prefab` matches the scene | `ReconcilePlayerPrefab` |

## Hub screens

Every page was taller than its viewport and simply clipped - stages 5 and up and the bag's SELL
button were unreachable outside the editor's Game view scrollbar. `CreateScrollContent` wraps
each page in a `ScrollRect`; `NestedPageScroll` on the inner view judges the first drag's
direction and, if it's more horizontal than vertical, disables itself for that drag and replays
the gesture into the outer `SwipePageView` - two `ScrollRect`s at right angles don't otherwise
cooperate.

`SwipePageView` itself no longer trusts `ScrollRect.velocity` to detect a flick: this component
runs with inertia off (inertia fights paging), and with inertia off `ScrollRect` has no reason to
maintain a velocity. It now tracks the drag's own smoothed speed and picks direction from the
flick, not from total travel - so a drag that wandered before flicking follows the flick.

`ItemTooltip` is one panel shared by every tile in the hub, parented to the canvas root so it
draws over everything and its pointer-relative placement math stays in canvas space. Bag tiles
and equipped-slot tiles both bind it (`ItemTooltipTrigger`, added per tile since the item a tile
represents changes every rebuild). It never blocks raycasts, so it can't eat the tap it's
describing.

`ConfirmTapButton` replaced a single unconfirmed tap on the playtest RESET button - which deletes
the save profile - with an arm-then-confirm step that times out on its own.

## Rebuild storms

`GearPage` and `InventoryPanel` used to call `Refresh()` straight from every event handler.
Upgrading an equipped item raises `EquipmentChanged`, `StatsChanged` and `CurrencyChanged` in one
call, each tearing down and respawning every tile - and doing that from inside a `Button`'s
`onClick` destroys the button that's still mid-dispatch, which is how you get an intermittent
`MissingReferenceException` on tap. Both pages now set a `_rebuildQueued` flag and rebuild once in
`LateUpdate`. The bag's SELL confirmation moved from a plain bool to a timed window
(`_sellArmedUntil`) for the same reason `ConfirmTapButton` exists: a flag survived swiping to
another page and back, leaving a destructive action armed minutes later.

`HitFlash` dropped its coroutine for a counted-down float in `Update`. A swing into a pack of
enemies was allocating an iterator and a coroutine wrapper per enemy, per swing, for a 0.1s tint.

## Combat feel

`PlayerTargeting` breaks the link between walking and aiming: it searches on an interval (not per
frame - a physics query every frame per player is the exact cost the mobile target rules out),
caches the result, and biases toward the current target so aim doesn't flicker between two
similar-distance enemies. `PlayerController.UpdateAttack` checks the buffered press *before*
consuming it - the old order consumed a press the moment it arrived, so a tap during cooldown was
silently eaten and the input buffer never did its one job. Holding the button now auto-repeats at
the weapon's own attack speed instead of needing a tap per swing.

## Enemies

`EnemyPool` keeps one idle queue per prefab (enemy types aren't interchangeable) and falls back to
`Instantiate` for anything it can't serve, so a scene with no pool still spawns enemies normally.
`SpawnPoint` asks its `EnemyPoolReference` for the pool if one has published itself; spawn points
live inside stage *prefabs*, which can't hold a scene reference directly, hence the indirection
(same pattern as the existing projectile pool and player reference).

`EnemyController.ResetForReuse` undoes everything a death did - stops coroutines, re-enables
brain/motor/colliders, restores each renderer's authored color (captured in `Awake`, before
anything can fade it), resets memory on `EnemyPerception` (otherwise a reused enemy wakes up
already alerted to wherever the last occupant last saw the player), and resets health. It's called
from `SpawnPoint` *after* `EnemyStats.Configure`, so the health reset sees the level this spawn is
configured at, not the level the previous occupant died with.

## Economy

Gems previously had exactly one source: selling a Legendary+, a ~1.8% drop weight, while bag
expansion (the only sink) becomes urgent within a few stages. `FirstClearBonus` pays a one-off
gem reward the first time each stage clears - snapshotted against `HighestUnlockedStage` at stage
*start*, not read live at completion, because `StageManager` also reacts to `StageCompleted` and
Unity doesn't order event subscribers.

Upgrade tuning changed because the old numbers made upgrading irrational: at +6%/level a maxed
Common (+60%) merely *tied* a freshly dropped Epic, so hoarding gold for a better drop always beat
spending it. Per-level bonus is now +10%, cost growth eased from 1.35x to 1.28x per level, and the
refund on sale rose from 50% to 80% - gear turns over constantly here, so a heavy switching tax
just punishes replacing what you just upgraded.

Stages 1-4 all paid the exact same loot and gold modifiers, so replaying an early stage was
strictly worse than pushing forward, and pushing forward wasn't better paid either.
`FixStageRewardCurves` gives every stage `1 + (number-1) x 0.08` loot and `x 0.15` gold.

## Saves

Unclaimed reward overflow - whatever didn't fit in the bag when a stage finished - lived in a
plain in-memory list on `StageRewardCollector` and was silently destroyed on quit. `SaveManager`
now serializes it into `RewardStorageEntry` records (no expiry yet - Reward Storage's 24-hour
timer is a later phase) and restores it straight back into the collector, never into the bag:
the bag being full is exactly why these items were pending, so dumping them in on load would just
overflow it again.

## Stage transitions

`Destroy` is deferred to the end of the frame. The old stage's colliders, enemies and wave
coroutines were staying live for the rest of the frame the next stage loaded in, so for one frame
the player could stand inside the new arena while the old one's walls still existed.
`StageManager` now deactivates the outgoing stage instance before destroying it.

## `Player.prefab`

Frozen since Phase 1 while `Health`, `PlayerStats`, `EquipmentManager`, `PlayerLevel` and the
whole visual rig existed only as scene-instance overrides - one "Revert Prefab Instance" away
from deleting the player. `ReconcilePlayerPrefab` applies the scene instance back onto the prefab,
last, after everything else in this phase has been wired onto it.

## Test it

1. **Scrolling.** Stages page: reach stage 10. Gear page: reach the STATS block and the action
   row. Bag page: reach SELL. A horizontal swipe from inside any of them still changes hub pages.
2. **Tooltip.** Hover (or press, on touch) a bag tile and an equipped slot: the shared panel shows
   name, rarity color and stats, stays clear of the screen edge, and never eats the tap.
3. **Reset.** Tap RESET once: it relabels to `SURE?` and repaints. Wait past the arm window: it
   reverts on its own without deleting anything. Tap twice within the window: the profile resets.
4. **Combat.** Hold the attack button near two enemies at different distances: it stays locked on
   one rather than flickering, fires continuously, and you can back away while still hitting it.
5. **Pooling.** With `logPooling` on, clear a wave and watch the next wave log `Reused`, not a
   fresh spawn every time.
6. **Economy.** Clear a stage for the first time: gems increase even with no Legendary sold.
   Upgrade an item a few levels, then sell it: the refund is visibly higher than before.
7. **Save.** Fill the bag, clear a stage that drops loot, quit without opening the bag, relaunch:
   the pending reward is still there instead of gone.
8. **Stage transitions.** Clear a stage and immediately look at the arena on entry: no leftover
   wall or enemy from the previous stage for even one frame.

## Known limits (deliberate, for later)

- Reward Storage entries never expire (`ExpiresAtUnixSeconds` is always `0`). The 24-hour timer
  is its own phase.
- The enemy pool has no cross-scene lifetime story yet - it lives on `GameSystems` and is never
  explicitly drained; `maxIdlePerPrefab` (16) is the only cap.
- `PlayerTargeting`'s `requireLineOfSight` and `stickiness` are first-guess numbers, tune on feel.
- The boss (`Warlord`) still doesn't use `PlayerTargeting`-aware behaviour on the enemy side -
  aim assist so far is player-only.
