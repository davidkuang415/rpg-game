# Phase 12 — Reference Card

The polish pass: a frame cap, sharper art, a job for gold, ten stages, and motion everywhere.

## Run
`RPG > Phase 12 > Build Polish, Economy And Stages`

Safe to re-run. It runs the Phase 11 tool itself first (the hub is rebuilt there, and its pages
now need the economy wired in), then applies everything below. Data assets it creates
(`ItemEconomyConfig`, `BossWarlord`) are only written on first creation, so hand tuning survives.

**Do not run the Phase 6 tool after this one.** Phase 6 rebuilds stages 1 and 2 in the old flat
style and resets the registry to two stages. Stage prefabs are owned by Phase 12 from here on.

## What changed, in one table

| Ask | Where it lives |
|---|---|
| 144 fps cap | `FrameRateLimiter` on `GameSystems` |
| Higher resolution | Placeholder sprites regenerated at 4x; standalone player = borderless native res, 1080x1920 window |
| More stages | Stages 3–10 in `Phase12StageLayouts`, built with `StageKit`; 10 is the Warlord boss |
| Better graphics | Body / Outline / Shadow on every character, tiled floors, edged walls, rounded UI, text shadows, vignette |
| Sell gear (gems for Legendary+) | `ItemEconomy.TrySell`, SELL button on the bag page |
| A use for gold | `ItemEconomy.TryUpgrade`, UPGRADE button on the bag and gear pages |
| Animations | `CharacterAnimator`, `SwingArcVisual`, `CameraShake`, `ButtonPressFeedback`, `ModalPanel` fade-in, `RoomExit` door dissolve |

## Frame cap

`Application.targetFrameRate = 144` with **VSync off**. VSync is not optional here: while it is
on, Unity ignores `targetFrameRate` completely and syncs to the display instead, so on a 240 Hz
monitor the "cap" would do nothing. The tool also turns VSync off on every quality level in
Project Settings, and the limiter re-applies itself on focus and every few seconds in case a
platform resets it. Change the number on the component, not in code.

## Resolution

Two separate things:

- **Sprites.** Phase 1 drew a 64px square, 128px circle and 256px ring. On a 1440p phone the
  player's circle covers ~170 screen pixels, so those were being upscaled. They are now 256 /
  512 / 1024, regenerated *in place* so every existing reference keeps working. Pixels Per Unit
  still equals the pixel size, so a sprite is still exactly one world unit.
- **The window.** Standalone builds open as a borderless full-screen window (the display's own
  resolution, no mode switch), falling back to a 1080x1920 resizable window. Interface
  orientation is locked to portrait — the canvas is designed at 1080x1920 and auto-rotating it
  to landscape produced an unusable layout.

## Gold: upgrading and selling

```
ItemEconomyConfig (asset)      every number: costs, growth, stat bonus, sell values, gem payouts
   └─ ItemEconomy (GameSystems) the transaction: checks ownership, moves currency, mutates the item
        ├─ TryUpgrade(item)      +1 level, gold spent, EquipmentManager told if it is equipped
        └─ TrySell(item)         bag only; gold, plus gems at Legendary and above
```

**Upgrading** multiplies an item's *base* stats: `1 + 0.06 x level`, to a maximum of +10 (+60%).
It never touches enchantment values — that is the design rule `EquipmentStatCalculator` was
already written around; the multiplier it always accepted is finally being supplied. Cost grows
with item level, rarity tier and the upgrade level already on the item. The upgrade level was
already part of `EquipmentInstance` and therefore already in the save file — no format change.

**Selling** pays `BaseValue x level scale x RecycleValueMultiplier` (both of those fields
existed since Phase 7 and were waiting for this), plus half of whatever was spent upgrading it.
Legendary items also return 5 gems, Mythic 15. Selling is **bag only**: an equipped item has to
be taken off first, so the "are you sure?" moment is about something already in storage.

### The pages
Tapping an item now **selects** it rather than acting on it. The details box describes it and
the row of buttons acts on it: EQUIP / UPGRADE / SELL on the bag page, UNEQUIP / UPGRADE on the
gear page. Tapping the selected item again does what one tap used to do (equip / unequip), so
the old reflex still works.

SELL is a two-tap action: the first tap arms it ("SURE? tap again"), the second sells. Selecting
anything else disarms it. Item tiles show `+N` in their name once upgraded.

## Characters

Every character's sprite used to sit on its root, next to the collider. Nothing could animate
the sprite without resizing the hitbox — which is exactly why `HitFlash` was written to only
ever change colour. The tool moves the sprite onto a **`Body` child** and adds an `Outline`
(dark circle behind) and a `Shadow` (soft blob below):

```
Enemy_MeleeGrunt (collider, rigidbody, Health, brain...)
 ├─ Body        the sprite, tinted; everything animates THIS
 ├─ Outline     circle x1.14, nearly black
 ├─ Shadow      soft blob, offset down, squashed
 └─ HealthBar   (Phase 11, unchanged)
```

`CharacterBody.FindRenderer` finds the sprite in either layout, so a prefab that has not been
restructured still works. `HitFlash`, `PlayerStatsBinder`, `EnemyController`'s fade and the
enemy telegraph tint were all re-pointed at the body.

The player is restructured on the **scene** object, for the reason Phase 11 documents: the
prefab is stale. Removing the prefab's root renderer on the instance becomes a "removed
component" override, which is what you will see in the Inspector.

### CharacterAnimator
One component, on the player and every enemy. It finds whatever movement / attack / health
components the character has and listens to those; anything missing is skipped. All
procedural, no Animator asset, no per-frame allocation:

| Trigger | Motion |
|---|---|
| moving | bob (rectified sine), lean into the direction of travel, shadow shrinks as the body lifts |
| standing | slow breathing |
| hit (`Health.DamageTaken`) | recoil away from the hit, squash |
| player attack (`PlayerAttackBase.Attacked`) | lunge toward the swing |
| enemy windup (`EnemyAttackBase.AttackTelegraphed`) | pull back and hold — the telegraph is now motion as well as tint |
| enemy strike (`EnemyAttackBase.AttackExecuted`, new) | lunge |
| spawn / revive | pop in with overshoot |
| death (`Health.Died`) | flatten, tip over, sink; the enemy then fades as before |

Impulses are additive, so a hit during a lunge just adds — neither needs to know about the
other. The player's revive on a stage restart has no event, so it is detected by seeing
`Health.IsAlive` flip back on.

## Stages

`StageKit` is the Phase 6 helper set pulled out into its own class, with the Phase 12 look:
tiled floors (`FloorTile`, drawn with `SpriteDrawMode.Tiled` so the grid is the same size in
every arena), a darker rim inside the walls, and walls with a dark edge and lit top face. The
Level Tools menu still works for hand-built stages.

| # | Name | Idea | Level |
|---|---|---|---|
| 1 | Arena | the Phase 6 layout | 1 |
| 2 | Two Rooms | the Phase 6 layout | 3 |
| 3 | Corridor | three rooms in a row: doors open as rooms clear | 4 |
| 4 | Pillars | one room, four pillars, three waves | 5 |
| 5 | Crossroads | an L of rooms around a solid block; doors turn corners | 6 |
| 6 | Gauntlet | tall run, slingers dug in behind cover | 7 |
| 7 | Ambush | open room, ring of cover, waves get heavier | 8 |
| 8 | Switchback | zigzag walls, every fight is around a corner | 9 |
| 9 | Citadel | four rooms in a square, cleared clockwise | 10 |
| 10 | Warlord | escort wave, then the boss with slingers on the flanks | 11 |

Doorways are cut to exactly the door's size (4 units) so nothing slips past a locked blocker.
Loot and gold modifiers rise gently from stage 5, and stage 10 pays 1.5x loot / 1.6x gold.

The empty `Stage_03.prefab` scaffold from the Level Tools is deleted; `Stage03.asset` now
points at `Stage_03_Corridor`. Stages 1 and 2 are rebuilt at their old prefab paths, so their
GUIDs — and the references from their data assets — are unchanged.

### The Warlord
`BossWarlord.asset` + `Enemy_BossWarlord.prefab`. The prefab is a copy of the Brute's, scaled
x1.9 and tinted; the asset's numbers are what make it a boss (700 HP / 20 ATK / 30 DEF at level
1, 2.2 reach, longer windup, `LootTable_Boss`, 300 XP, 150 gold). It uses the same melee
behaviour as every other enemy — a boss with its own attack patterns is a later phase.

## Feel

- **`SwingArcVisual`** (player): a crescent (`Slash.png`) that flashes in the swing direction,
  scaled from the attack's real range so what you see is exactly what was checked. A whiff no
  longer looks like standing still. Alternates sweep direction. Archer arrows already show.
- **`CameraShake`** (Main Camera): trauma-based, applied after `CameraFollow2D` in `LateUpdate`
  and removed before the next follow, so neither fights the other. Player hit 0.35, crit 0.55,
  kill 0.12. `AddTrauma()` is public for later (boss slams, level-ups).
- **`ModalPanel`**: every screen fades and grows in over 0.18 s (unscaled; the game is paused).
  Raycasts are blocked during the fade so a tap cannot land on a half-transparent button.
- **`ButtonPressFeedback`**: every button shrinks on touch-down and springs back.
- **`RoomExit`**: the locked bar dissolves over 0.35 s instead of vanishing. The collider drops
  instantly — only the picture lingers.
- UI: rounded corners on every flat-coloured button (`RoundedRect.png`, 9-sliced), a shadow
  under every label at 26pt or larger, and a vignette as the first child of the HUD.

## Test it

1. **Cap.** Stats window or the dev overlay: frame time floors at ~6.9 ms.
2. **Look.** Play → hub: rounded buttons that squash when tapped, shadowed text, the hub fades
   in. Start Stage 1: tiled floor, edged walls, the player has an outline and a shadow, bobs and
   leans while walking, breathes while standing.
3. **Fight.** Swing: a crescent flashes and the body lunges. Get hit: the body recoils, the
   camera shakes. Kill: the enemy tips over, flattens and fades. Enemies visibly rear back
   before striking.
4. **Doors.** Clear a room in Stage 3: the orange bar shrinks away instead of popping.
5. **Economy.** Clear stages until the bag has something. BAG: tap an item — it highlights,
   the details show, three buttons light up. UPGRADE: gold drops, the tile reads `+1`, the stat
   lines grow. SELL: first tap says SURE?, second pays out. Sell a Legendary: gems too. GEAR:
   tap an equipped slot → UNEQUIP / UPGRADE; upgrading an equipped item changes the stat sheet
   immediately.
6. **Stages.** Ten in the list; stage 10 is marked BOSS. The Warlord is big, pink and slow.

## Known limits (deliberate, for later)

- The bag page still does not scroll (Phase 11's limit). Past ~24 slots the action row will be
  covered by tiles.
- The boss shares the Brute's behaviour. Attack patterns, phases and a boss health bar are their
  own phase.
- `Player.prefab` is more stale than ever: the scene instance now also carries a removed
  renderer, three new children and three new components. Worth reconciling before a second scene.
- `Elite` spawns are still a flag with no effect.
- Sell prices and upgrade costs are first-guess numbers in `ItemEconomyConfig`; tune there.
