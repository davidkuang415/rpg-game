# Phase 15 — Reference Card

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
