# Phase 11 — Reference Card

Hub UI (stats and bag out of the play area), health bars, and visible hits.

## Run
`RPG > Phase 11 > Build Hub UI And Combat Feedback`

Safe to re-run. It rebuilds the hub and the effect prefabs from scratch each time, and only
writes the health-bar style assets when it first creates them — so anything you tune by hand
in those assets survives.

## What moved out of the play area

| Before | After |
|---|---|
| IMGUI **Stats** overlay drawn over the arena | **GEAR** page in the hub (real uGUI, class / level / XP / stats) |
| **BAG** button in the top-right corner during a fight | **BAG** page in the hub |
| Stage list as its own full-screen modal | **STAGES** page in the hub |

The combat HUD now contains only the joystick and the attack button. The dev overlay still
exists (`StatsDebugOverlay`, §59) but draws **nothing at all** — not even a toggle button —
until you press **F1**. Set `showToggleButton` on it if you need an on-screen toggle for
device testing.

### Why the player's bar needs the scene object, not the prefab
`Player.prefab` was saved in Phase 1 and still holds only `PlayerMotor`, `PlayerFacing` and
`PlayerController`. `Health`, `PlayerStats`, `EquipmentManager` and `PlayerLevel` were all added
to the scene instance in later phases and never applied back to the prefab. So the health bar and
`HitFeedbackEmitter` are added to the **scene** Player, where the rest of its real setup already
lives. Enemies are the opposite case — they are spawned at runtime, so they are set up on the
prefab.

## The hub

```
HubScreen (ModalPanel: freezes input, pauses time)
 ├─ Title            current page name
 ├─ TabBar           tabs built from the pages at runtime
 └─ PageViewport     ScrollRect + SwipePageView + RectMask2D
     └─ Pages
         ├─ StagesPage   StageSelectPanel
         ├─ GearPage     equipped slots + full stat sheet
         └─ BagPage      InventoryPanel
```

Swipe **left** to go STAGES → GEAR → BAG, right to come back. Tapping a tab jumps straight
there. Both do the same thing; the tabs exist so swiping is discoverable.

`SwipePageView` rides on Unity's `ScrollRect` rather than handling touches itself. That is the
whole trick: `ScrollRect` already knows how to tell a swipe apart from a tap on a button inside
the page. A hand-rolled drag handler would equip an item every time you tried to swipe past it.

### Pages are not panels
A `HubPage` never shows itself, hides itself, freezes the player or touches `Time.timeScale`.
The hub does all of that once, for all of them. Adding a fourth page (shop, quests) means:
write a `HubPage`, drop it under `Pages`, add it to the hub's `pages` array. Nothing else changes.

### Adding a stage
Unchanged from Phase 6: add a `StageData` asset to the `StageRegistry`. No C# edits, no UI edits.

## Health bars

`HealthBarView` sits on a `HealthBar` child of the player and of every enemy prefab.

Built from three **SpriteRenderers**, not a world-space Canvas. A Canvas per enemy would rebuild
its mesh every time the enemy moved, and a room full of enemies would mean a room full of canvas
rebuilds every frame — exactly the per-frame cost the design rules out on mobile.

| Part | Job |
|---|---|
| Background | dark outline |
| Trail | the pale bar left behind after a hit, which then drains to meet the real value |
| Fill | actual current HP, turning red below the low-health threshold |

The trail is what makes a single hit visible on a big health pool.

Look and feel are data: `Data/UI/PlayerHealthBarStyle.asset` and `EnemyHealthBarStyle.asset`.
The only real difference is `hideWhenFull` — on for enemies, off for the player.

Max HP is never cached; the bar reads it from `Health`, so a level-up or an equipment swap is
reflected immediately.

## Hit feedback

```
Health.DamageTaken
   └─ HitFeedbackEmitter (on the victim)
        └─ CombatFeedbackChannel (asset)
             ├─ HitSparkPool     -> HitSpark      expanding ring at the impact point
             └─ DamageNumberPool -> DamageNumber  floating number, or MISS on a dodge
```

Plus `HitFlash` on the victim's sprite, now with a separate, longer, orange flash for criticals.

**Nothing in the attack code changed.** The emitter listens to the *victim's* `Health`, so one
component covers sword swings, arrows, enemy bolts, and later hazards and boss attacks. Crits
read differently everywhere: bigger spark, bigger orange number, longer flash.

Both effect types are pooled (`ComponentPool<T>`), because hits are the most frequent spawn in
the game.

Damage numbers use `TextMesh`, not a world-space Canvas — same reason as the health bars.

## Test it

1. **Fresh profile** — Wipe in the dev overlay, Play → class select → pick Knight.
2. **Hub opens on STAGES.** Swipe left: GEAR (class, level, XP bar, equipped slots, full stat
   list). Swipe left again: BAG. Swipe right to come back. Tabs jump directly.
3. **Nothing menu-shaped over the arena.** Start Stage 1: joystick, attack button, and the
   fight. No BAG button, no stats overlay.
4. **Health bars.** A bar sits under the player and under each enemy. Enemy bars appear on
   first damage and hide again when full; the player's is always visible.
5. **Hits are obvious.** Swing at an enemy: ring spark at the point of contact, damage number
   floating up, sprite flashes, pale trail drains on its health bar. Crits are orange and bigger.
   Archer arrows do the same where the arrow lands.
6. **Equip loop.** Clear a stage, Continue, swipe to BAG, tap an item → it equips; swipe left to
   GEAR and the stat you raised shows in green.

## Known limits (deliberate, for later)

- Pages do not scroll vertically yet. Nested scroll views (vertical inside horizontal) fight
  each other in uGUI, so content is laid out to fit one screen. Once the bag outgrows ~24 slots
  this needs a proper solution, not a quick nested `ScrollRect`.
- No item comparison ("equipped vs this") on the bag page.
- No hitstop. It is the obvious next step for impact, but it means touching `Time.timeScale`,
  which the modal panels already own — worth doing carefully rather than quickly.
- `Player.prefab` is still stale (see above). Worth reconciling before there is a second scene,
  because right now only the scene instance is a complete player.

## RESET button (playtest only)

A small **RESET** button is pinned to the top-right corner of the HUD. It is the last child of
the HUD, so it floats above every panel including the hub — which is where you are standing when
you want it. Pressing it wipes the profile and drops you straight back to class select, with no
Stop/Play cycle.

`GameFlowController.RestartFromZero()` owns the sequence (close screens, unload the arena, choose
the next screen); `SaveManager.ResetProfile()` owns the data (delete the file, then return
equipment, bag, wallet, level, stage progress and class to their day-one state).

Two things had to change for a wipe to actually stick:

- `DeleteSave()` only ever removed the *file*. The class, level, wallet and bag stayed loaded, so
  play carried on unchanged and the next autosave wrote them back. `ResetProfile()` clears the
  live state too.
- `Save()` now refuses to write a profile with no class chosen. Without that, quitting right
  after a wipe re-saved the still-loaded state over the file that had just been deleted.

Delete the `BuildResetButton` call from the Phase 11 tool when a real settings menu arrives.

## Movement fixes that came out of this phase

Two bugs behind "the character drifts left with no input":

- `KeyboardInputWriter` read `GetAxisRaw("Horizontal")`. The legacy Horizontal/Vertical axes are
  also bound to gamepad sticks, and `GetAxisRaw` bypasses the dead zone — so a controller resting
  slightly off centre fed a small constant value forever. It now reads WASD/arrow keys directly.
- `PlayerMotor` (and `EnemyMotor`) drive movement with `Rigidbody2D.MovePosition` but never
  cleared the body's velocity. A top-down body has no gravity and no drag, so any velocity it
  picked up — a shove from an enemy, residue from the previous step — persisted and kept pushing.
  Both now zero `linearVelocity` each `FixedUpdate`, which also means enemies can no longer shove
  the player around.
