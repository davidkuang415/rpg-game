# Phase 15 — Reference Card

<<<<<<< HEAD
The playtest pass. A critical first hour with the game, written up as what was missing and
then built: a dodge, a pause menu, a combat readout, elites that exist, a real boss fight,
healing between rooms, and sound.

## Run

`RPG > Phase 15 > Apply Playtest Pass`. Safe to re-run. Needs the hub (Phase 12/13) and the
enemy prefabs (Phase 12/14) to exist. It edits the scene, `Enemy_BossWarlord.prefab`,
`BossWarlord.asset`, the stage prefabs 4–10 (elite flags only) and `Player.prefab` (reconciled
last, as Phase 13 does).

If you later re-run the Phase 12 tool, the stage prefabs are rebuilt from
`Phase12StageLayouts` — which now carries the same elite marks — so nothing is lost.

## What the tester said, and where the answer lives

| Complaint | What was built | Where |
|---|---|---|
| "Enemies telegraph, but I can't do anything about it except walk" | A dodge roll with i-frames | `PlayerDash`, `PlayerMotor.Dash`, `Health.HoldInvulnerability`, DASH button / Shift |
| "I'm in a fight and the phone rings. There's no way out but dying" | Pause menu: Resume / Sound / Leave Stage | `PauseMenuScreen`, PAUSE button top-left |
| "Is this the last enemy or the first of three waves? No idea" | Stage · room · wave · enemies-left readout, plus banners | `CombatHudView`, `RoomController.WaveStarted` |
| "The Elite checkbox does nothing" | Elites: +80% HP, +35% ATK, bigger, gold outline, 2.5x rewards, elite loot table | `EliteModifierSource`, `DifficultyCurveData` (Elite block), `EnemyController.ApplyElitePresentation` |
| "The boss is a big Brute" | Top-of-screen boss bar and an enrage phase at 50% | `BossHealthBarView`, `BossEnrage`, `EnemyData.IsBoss` |
| "Three rooms of attrition with no way to recover" | +30% max HP on every room clear; full heal + shake on level-up | `RoomClearHeal`, `LevelUpFeedback` |
| "It's completely silent" | Fourteen sound effects synthesised at startup, a sound toggle | `ProceduralSfx`, `SfxPlayer` |

## The dodge

`PlayerDash` covers 3.2 units in 0.18 s with 0.24 s of invulnerability, then 0.9 s of
cooldown. Direction is the joystick if it is pushed, otherwise the facing. The burst goes
through `PlayerMotor.Dash`, which owns it inside the same `FixedUpdate` that owns walking —
so there is still exactly one writer of the rigidbody's position, and walls stop a dash the
same way they stop a walk. No attacks fire mid-roll; the press stays buffered and comes out
the moment the roll ends.

Invulnerability is a *counted hold* on `Health`, not a bool, so a second source later (a
revive grace period) cannot release the dash's. While held, every hit is reported as dodged:
the MISS number, the absence of a recoil, and the absence of camera shake are exactly the
feedback a successful roll should give, and no new code path had to be added for any of it.

Input follows the attack button's pattern: `PlayerInputChannel.PressDash` with a short buffer,
`TouchActionButton.ActionType.Dash` on the new HUD button, Shift or right-mouse in the editor.

## The pause menu

`PauseMenuScreen` is a `ModalPanel`, so it freezes time and player input the way every other
screen does. LEAVE STAGE calls `StageFailureHandler.Abandon()` — the same path the death
screen's LEAVE takes — so the rules are identical: everything earned so far is kept (it is
pending in the collector and nothing here touches it), and the next stage stays locked.

It refuses to open while the player is dead, because the defeat screen is about to open on
its own and two modal panels fighting over `Time.timeScale` is how you get a frozen game.

## The combat readout

`CombatHudView` draws `STAGE 3   ROOM 2/3   WAVE 1/2   4 LEFT` at the top and a banner for
each beat: the stage name on entry (with BOSS STAGE where relevant), `WAVE 2/3` when
reinforcements spawn, `ROOM CLEARED +30 HP`, `LEVEL UP! 7`.

It re-renders from the rooms' live state on every event rather than counting events, for a
reason worth knowing: `StageController.Begin` starts the first room synchronously, and a
zero-delay first wave spawns *inside that call*, before `StageStarted` is raised. Any counter
subscribed on `StageStarted` would begin one wave behind.

New seams for it: `RoomController.WaveStarted` / `CurrentWave` / `RequiredWaveCount`,
`StageManager.CurrentController`, and `StageEventChannel.StageUnloaded` — raised whenever the
stage instance goes away for any reason (completed, abandoned, replaced by a restart), so
things that show only during a fight have one event to hide on instead of a list.

Both HUD views subscribe in `Awake` and unsubscribe in `OnDestroy`, and hide a *child*
(`Contents`) rather than themselves — the same trap `StageCompleteScreen` documents: a view
that only listens while visible can never be the thing that shows itself.

## Elites

The flag on `SpawnPoint` was carried through every layer since Phase 6 and never did
anything. Now `EnemyStats.Configure` registers an `EliteModifierSource` — percent modifiers on
the Temporary layer, through the same `IStatModifierSource` seam the player's equipment uses —
and `XpReward` / `GoldReward` multiply by `EliteRewardMultiplier`. The numbers live on
`DifficultyCurveData` so one asset tunes every enemy type.

`EnemyController.ApplyElitePresentation` runs on every spawn (a pooled object can be an elite
this time and a regular the next): root scale ×1.22, `Outline` tinted gold, both restored
from the authored values otherwise. `StageRewardCollector` rolls elites from
`LootTable_Elite` instead of their own table; a boss keeps its own.

Elites are placed from stage 4 on — usually the last Brute of a stage, an elite Slinger in
the Switchback, and the two Grunts escorting the Warlord. See `ElitePromotions` in the setup
tool and the `elite: true` marks in `Phase12StageLayouts`.

## The boss

`EnemyData.IsBoss` (set on `BossWarlord.asset`) is what the boss bar keys on. It binds on the
new `EnemyEventChannel.EnemySpawned`, raised from `EnemyController.ResetForReuse` *after*
health is reset, so the bar's first read is the right number.

`BossEnrage` on the boss prefab watches its own `Health`; at 50% it registers itself as a stat
source (+25% ATK, +45% attack speed, +35% move speed) and tints the body. The tint is
re-asserted in `LateUpdate` because `HitFlash` restores whatever colour it captured when a
flash began — if a flash was mid-air when the phase changed, its end would quietly put the
calm colour back. The bar turns orange and reads ENRAGED; the sound layer growls.

## Healing

`RoomClearHeal` restores 30% of max HP on every room clear except the final one (the stage is
over; the next one heals on entry). `LevelUpFeedback` heals to full and kicks the camera on a
mid-fight level-up, which until now was a `Debug.Log`. Both are fractions of max, so they
mean the same thing at level 40 as at level 1.

## Sound

There are no audio files in the project. `ProceduralSfx` synthesises every clip at startup —
swept oscillators and low-passed noise under an envelope, a few hundred milliseconds each.
`SfxPlayer` listens to the game's existing seams (`CombatFeedbackChannel.DamageShown`, the
enemy and stage channels, the player's `Attacked` / `Dashed` / `LeveledUp`, a static
`ButtonPressFeedback.Pressed`) and plays them with a little pitch jitter through a small pool
of `AudioSource`s. Same sound within 45 ms is dropped, so a Knight swing into five enemies is
one hit, not five stacked. Mute is a `PlayerPrefs` flag; the pause menu toggles it.

Any clip can be replaced by a real recording via the `Overrides` list on `SfxPlayer` without
touching code.

## Test it

1. **Dash.** Stand in a Brute's windup and press DASH (Shift): a MISS number, no recoil, no
   shake. Dash into a wall: it stops. Mash DASH: one roll per ~1.1 s. Tap ATTACK during a
   roll: the swing comes out as the roll ends.
2. **Pause.** PAUSE mid-fight: time stops, enemies freeze mid-windup. RESUME: they finish it.
   SOUND: label toggles, sound stops, survives a relaunch. LEAVE STAGE: hub, rewards from the
   attempt still pending, next stage still locked. Die, then try PAUSE during the death beat:
   it does not open.
3. **Readout.** Stage 3: `ROOM 1/3`, count drops per kill, `ROOM CLEARED +N HP` banner and
   the health bar refills. Stage 4: `WAVE 2/3` banner as the second wave lands.
4. **Elites.** Stage 4, wave 3: a bigger, gold-outlined Brute. It takes visibly more hits, its
   damage number on you is bigger, and the completion screen's gold/XP for the stage is up.
5. **Boss.** Stage 10: a bar with WARLORD at the top from the moment it spawns. At half: it
   turns orange, reads ENRAGED, the boss goes red and swings noticeably faster.
6. **Level-up.** Level up mid-room: banner, full heal, shake, a rising arpeggio.
7. **Sound.** Swing, hit, crit, get hit, kill, dash, room clear, stage complete, defeat, boss
   spawn, enrage, button tap — each distinct.
8. **Re-run.** Run the Phase 15 tool again: no duplicate buttons, no double-scaled elites, no
   second `BossEnrage` on the prefab.

## Known limits (deliberate, for later)

- The dash has no cooldown indicator on the button; `PlayerDash.CooldownFraction` is exposed
  for a ring when the button gets real art.
- Elites use the same behaviour as their base type; only numbers and looks change.
- The boss has one phase change and no new attack pattern. A slam or a charge is its own
  phase (see `EnemyArchetype.Charger`, still unused).
- Sound is synthesised; there is no music. Real clips drop in via `SfxPlayer.Overrides`.
- Slingers do not kite. An Archer with 8 range versus a Slinger with 7 wins standing still.
=======
See more, miss less: the camera pulls back and every living enemy the camera can't see gets an
arrow at the screen edge pointing at it.

## Run

`RPG > Phase 15 > See More Of The Arena`. Safe to re-run.

If the circular backdrop is still visible behind characters, that's a Phase 12 change (removing
the stand-in outline circle Phase 12 used to draw behind every body, back before Phase 14 gave
characters real art) - re-run `RPG > Phase 12 > Build Polish, Economy And Stages` to apply it.

## What changed, in one table

| Ask | Where it lives |
|---|---|
| See more of the arena | `CameraFollow2D.visibleWorldHeight`: 12 → 20 |
| Find enemies that are off-screen | `OffscreenEnemyIndicator` on `HUD/OffscreenIndicators` |
| No more circle behind each character | `Phase12SetupBuilder.RestructureCharacter` no longer creates "Outline"; removes it if found |

## Zoom

One number: `CameraFollow2D.visibleWorldHeight`, already exposed for exactly this ("the design
rule 'the arena is ~50 units but the player sees ~10 around themselves' is a single configurable
number"). 12 → 20 is a real design tradeoff, not a free upgrade - enemies, telegraphs and the
attack range are all now visually smaller on screen, and touch targets (already sized for the
old zoom) get harder to tap precisely. If that reads as too small, `SetVisibleWorldHeight` is a
public runtime hook already, so a settings toggle is cheap to add later.

## Off-screen indicator

`OffscreenEnemyIndicator` finds enemies the same way the player's own aim-assist does -
`CombatQueries.OverlapCircle` on the Enemy layer, on a 0.15s interval rather than every frame -
so nothing new has to register enemies with it. For anything alive and outside a small inset of
the camera's viewport, it keeps one arrow alive at the screen edge, rotated to point at it, and
tears the arrow down the moment that enemy comes on screen, dies, or leaves the search radius.

Direction is computed in world space (target position minus camera position on X/Y) rather than
by projecting through the viewport - this camera never rolls, so world right/up already line up
with screen right/up, and it sidesteps the singularities `WorldToScreenPoint` has for anything
behind the camera.

The arrow (Kenney, CC0, tinted toward red as an "enemy" cue - the only warm color available for
this hasn't been used as a specific role colour elsewhere in the hub) is a template child,
cloned per off-screen enemy and destroyed when it's no longer needed - the same
inactive-template-then-Instantiate pattern every tile and stage button in the hub already uses.

## Test it

1. **Zoom.** Enter a stage: visibly more of the room is on screen than before.
2. **Indicator.** Let an enemy walk (or spawn) off the visible area: an arrow appears at the
   screen edge pointing toward it. Walk toward it until it's back on screen: the arrow
   disappears. Kill an off-screen enemy: its arrow disappears without walking toward it.
3. **Multiple.** With more than one enemy off-screen at once, each gets its own arrow, roughly
   aimed in its own direction.
4. **Outline.** After re-running Phase 12: no dark circle behind any character - the sprite's own
   silhouette is the only shape.

## Known limits (deliberate, for later)

- No distance readout or count badge on the arrow - direction only. A number ("3") is easy to add
  to the same template if stacking arrows for a crowd gets confusing.
- All off-screen enemies look the same (one red arrow); nothing distinguishes a Slinger about to
  loose an arrow from a Grunt still crossing the room.
- Touch target sizes (joystick, attack button, hub buttons) were tuned for the old, closer zoom
  and haven't been revisited for the new one.
>>>>>>> 5257464fecfba92d062cc699187004b864597dc0
