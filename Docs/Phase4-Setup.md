# Phase 4 — Reference Card

## Run
1. `RPG > Phase 4 > Add Enemies To Scene`
2. `RPG > Setup > Configure Physics 2D Collision Matrix` (optional; nothing depends on it)

## Enemy composition (one prefab per archetype, all sharing the same components)
```
Enemy_*
  SpriteRenderer + Rigidbody2D + CircleCollider2D
  EnemyStats        EnemyData x level -> DifficultyCurve -> StatBlock (ICombatStatProvider)
  Health            shared with the player: one damage pipeline for everyone
  HitFlash
  EnemyPerception   throttled LOS checks + last-known-position memory
  EnemyMotor        physics movement, speed from the stat pipeline
  EnemyMeleeAttack | EnemyRangedAttack   (windup telegraph, then impact)
  EnemyBrain        Idle / Chase / Attack
  EnemyController   death -> EnemyEventChannel -> despawn
```

## Enemy definitions (level 1)
| | Grunt | Slinger | Brute |
|---|---|---|---|
| HP | 40 | 28 | 120 |
| ATK | 8 | 6 | 14 |
| DEF | 0 | 0 | 25 |
| Move | 2.7 | 2.2 | 1.7 |
| Attacks/sec | 0.9 | 0.6 | 0.5 |
| Attack range | 1.1 | 7 | 1.5 |
| Detection | 9 | 11 | 8 |
| XP | 10 | 12 | 30 |

## Level scaling (DifficultyCurve.asset)
`value x (1 + growth)^(level-1)` — HP +11%, ATK +8%, DEF +7%, XP +10%, Gold +8% per level.
Move speed and attack speed deliberately do NOT scale.

## Key rules implemented
- Walls block sight, melee swings and projectiles
- Losing sight -> hunt the last known position for `memorySeconds`, then give up
- Getting hit from out of view alerts the enemy (`alertOnDamage`)
- Attacks have a windup telegraph; aim locks at windup START so sidestepping works
- Enemy Level is separate from Stage Number

## Not yet done
- Charger archetype (needs a dash behaviour; Tank is data-only and already works)
- Elite modifiers: `isElite` flag and the IStatModifierSource hook exist, no modifiers yet
- Enemies are destroyed on death, not pooled; Phase 6 spawners take that over
- `PlayerDeathHandler` respawns on the spot; Phase 10 replaces it with stage restart
