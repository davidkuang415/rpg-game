# Phase 14 — Reference Card

Real art on the player, every enemy type, the floor, and every hub button. Source: Kenney
(kenney.nl), CC0 - free for any use, no attribution required. Walls, the joystick and combat VFX
(slash, vignette, shadow) are untouched; see Known limits.

## Run

`RPG > Phase 14 > Wire In Kenney Art`. Safe to re-run. Run it after Phase 12 (needs the enemy and
player prefabs Phase 4/12 build) - order relative to Phase 13 doesn't matter, they touch different
things.

## What changed, in one table

| Ask | Where it lives |
|---|---|
| Player looks like a class, not a tinted circle | `ClassData.BodySprite` (Knight, Archer), applied at runtime in `PlayerStatsBinder` |
| Enemies look like their type, not a tinted circle | `EnemyData.BodySprite`, baked onto each `Enemy_*.prefab`'s Body renderer |
| The boss keeps looking like the Brute's, scaled | `Phase14SetupBuilder.WireBossFromBrute` copies the Brute's sprite onto `Enemy_BossWarlord.prefab` |
| A real floor instead of a checker mask | `StageKit.KenneyFloorTilePath`, `FloorTile_Stone.png` |
| Real buttons instead of flat rounded rectangles | `Phase12SetupBuilder.KenneyButtonPanelPath`, `ButtonPanel.png` |
| Hit flash still reads as a flash on real art | `HitFlash.flashColor` / `criticalFlashColor` bumped to overbright values on every character this phase touches |

## Why full colour, not another tinted mask

Every character in the game so far is the same white circle, multiply-tinted per class or enemy
type (`BodyTint`) - that's how a "Knight" and a "MeleeGrunt" have ever been different colours.
The first attempt here kept that pipeline: convert each Kenney character to a white silhouette
(RGB forced to white, original alpha kept) so it would drop into the same tint machinery. It
looked wrong - rendered and checked before committing to it, not guessed. Kenney's chibi
characters carry their identity in flat COLOUR REGIONS (skin vs. armour vs. hair), not in
negative space, and they're close to fully opaque across their whole silhouette. Flattened to one
tinted colour, a knight in plate and a bare-chested brute both come out as the same faceless
blob - all the shape information the art actually had lived in colour, and tinting erases colour.

So this phase runs the tint pipeline in the other direction instead: `BodySprite` is real, already
-coloured art, and `BodyTint` is set to white (no multiply) wherever a `BodySprite` exists. The
field and the tint machinery are untouched - a class or enemy with no `BodySprite` still renders
exactly as before, tinted circle and all.

## Hit flash on real art

`HitFlash` works by replacing `SpriteRenderer.color` with `flashColor` for one short burst - a
literal colour swap, not a blend. That's invisible logic on a WHITE circle, because the visible
result is `texture x color`, and white text multiply-tinted white is just white. On the old
circle, the base colour (the class/enemy tint) and `flashColor` (`Color.white` by default) were
two different colours, so the swap was always visible.

With a full-colour sprite as the base and `BodyTint` at white, resting and flash-white are the
SAME colour - multiplying real art by plain white changes nothing, so the flash would stop being
visible the moment a character got real art. Fixed by overexposing instead of just brightening:
`flashColor` is bumped to `(2.6, 2.6, 2.6)` and `criticalFlashColor` to `(2.6, 1.9, 1.0)` on every
character this phase gives a sprite to (every enemy prefab, the boss, and the player). Values
above 1 push a multiply toward white the same way the old white-on-white-circle flash did,
without touching `HitFlash.cs` itself - purely a per-character data tweak, same mechanism, no new
code path.

## The floor

`StageKit` always drew one tiled `FloorTile` sprite; only the source image changes. The
procedural version was an ALPHA mask (pure white, shading only in the alpha channel) tinted
almost black (`FloorColor = (0.16, 0.17, 0.21)`) to read as a dim stone floor. The Kenney tile is
real colour, so that same tint would crush it back to near-black and lose the texture entirely -
`FloorColor` is lightened to `(0.88, 0.88, 0.9)`, a near-white wash rather than a fill, for the
same reason `BodyTint` moved to white. Walls, the wall rim, and door blockers still use the
procedural `Square` mask and its original tint - untouched, see Known limits.

## The buttons

Every flat-coloured `Button` in the hub gets its `Image.sprite` set to one shared rounded-rect
in `Phase12SetupBuilder.RestyleHud` (this is also where the Phase 11 "known limit" of an
unscrollable bag page etc. got fixed, in Phase 13). Swapping that shared sprite for Kenney's
`buttonSquare_beige.png` is a one-line change at the load site - every button, tile template and
stage button inherits it automatically, same as before. The button's role colour (blue-grey for
equip, dark gold for upgrade, dark red for sell) is still applied as a multiply tint on top, for
the same reason the floor's tint moved: a dark tint multiplied onto real, already-shaded button
art crushes it back to a flat blob. `RestyleHud` now washes that role colour 60% toward white
the FIRST time a button gets real art (guarded so re-running the tool doesn't wash it a second
time) - enough that SELL still reads as "the red one" without losing the art underneath it.

9-slice border (4, 9, 4, 4 px - left/bottom/right/top) was measured directly from
`buttonSquare_beige.png`'s pixels, not guessed: the bottom 9px is the button's own bevel PLUS its
drop shadow, kept together on purpose so 9-slicing never stretches the shadow independently of
the bevel above it.

## Where the source art is

`Assets/_Project/Art/Kenney/{Characters,Environment,UI}` - one PNG per use, cropped by hand from
Kenney's "Roguelike Characters" and "Roguelike/RPG pack" spritesheets (16x16 tile, 1px margin) and
the "UI Pack (RPG Expansion)". Imported Point-filtered at native pixel size (`spritePixelsPerUnit`
= 16 for the 16x16 sprites, matching how every placeholder shape's PPU already equals its pixel
size) so they stay crisp rather than smearing when Bilinear-stretched onto a phone screen.

## Test it

1. **Classes.** Pick Knight: a silver-armoured figure, not a blue circle. Pick Archer: a
   different figure, not a differently-tinted circle.
2. **Enemies.** Stage 1: grunts look like bearded, bare-chested fighters; nothing is a plain
   circle. A later stage with Brutes: bulkier, visually distinct from grunts. A Slinger: green,
   visually distinct from both.
3. **Boss.** Stage 10: the Warlord looks like a scaled-up Brute, not a circle.
4. **Hit flash.** Land a hit on any enemy: still a visible bright flash, not nothing. A critical:
   a warm/orange flash, longer.
5. **Floor.** Any stage: a textured stone tile, not a dark grid-line checker.
6. **Buttons.** Hub: every button shows real bevelled art with its role colour as a light wash -
   SELL still reads reddish, UPGRADE still reads gold-ish, neither is a flat colour block anymore.
7. **Re-run.** Run the Phase 14 tool a second time: no errors, nothing double-brightens or
   double-washes (buttons don't get lighter, enemies don't change).

## Known limits (deliberate, for later)

- Walls, the wall rim/edge/top, and door blockers still use the procedural `Square` mask - only
  the floor changed. A matching Kenney wall tile is picking-and-cropping work for another pass.
- The joystick, the sword-swing crescent, the soft shadow and the vignette are still procedural
  shapes (`PlaceholderArt`) - they already look intentional and didn't need replacing.
- `PlaceholderArt.RegenerateAll()` still regenerates procedural `FloorTile.png` and
  `RoundedRect.png` files on disk; they're simply no longer loaded by anything. Harmless, but
  worth deleting the generation calls if this drifts further.
- No particle/VFX pack wired in yet (Kenney's Particle Pack was downloaded for a later pass -
  hit sparks, level-up bursts). Combat feedback today is still tint + shake + squash only.
- Character sprites are single static poses (Kenney ships more - hold/attack variants exist in
  the same sheet). `CharacterAnimator`'s procedural motion (lean, lunge, recoil) still does all
  the animating; swapping poses per action is unexplored.
