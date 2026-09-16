# Phase 3 — Reference Card

## Run
`RPG > Phase 3 > Add Combat To Scene` (safe to re-run; dummies are rebuilt, arrow prefab reused)

## Damage pipeline
```
ATTACKER                                 DEFENDER (Health)
DamageCalculator.RollOutgoingDamage      DamageCalculator.RollDodge      -> 0 damage
  ATK x damageMultiplier                 DamageCalculator.ApplyDefense
  crit roll -> x CritDamage                raw * 100 / (100 + DEF x (1 - pen))
        |                                overkill trimmed
        +--> DamageInfo -----------------> DamageResult --> life steal on attacker
```

## Knight sword
- Cone of `ClassData.BaseAttackArcDegrees` (120) centred on facing
- Range from `ClassData.BaseAttackRange` (1.6)
- Once per enemy per swing (HashSet, recorded only after cone + LOS pass)
- Walls block: `CombatQueries.HasLineOfSight` against the Wall layer

## Archer bow
- Aim assist: closest enemy within range, inside +/-15 degrees, with line of sight
- No target -> fires straight ahead
- Arrows sweep with `CircleCast` (no tunnelling), stop on walls, pooled via `ProjectilePool`
- Damage rolled at fire time, not impact

## Key tuning fields
| Value | Where |
|---|---|
| Aim assist angle | Player > ArcherBowAttack > Aim Assist Angle (15) |
| Sword arc | Knight.asset > Base Attack Arc Degrees (120) |
| Melee range | Knight.asset > Base Attack Range (1.6) |
| Bow range | Archer.asset > Base Attack Range (8) |
| Arrow speed | Arrow.prefab > Projectile > Speed (18) |
| Pool size | ArrowPool > Prewarm Count (16) |

## Not yet done (by design)
- Physics2D collision matrix still default; masks are explicit in raycasts. Tune it in Phase 4.
- Damage numbers, hit reactions beyond a 0.08s flash: Phase 16.
- `maxPierceCount` on Projectile exists but nothing raises it above 0.
