# Phase 5 — Reference Card

## Run
`RPG > Phase 5 > Add XP And Leveling`

## Flow
```
Enemy dies
  -> EnemyEventChannel.EnemyDied
       -> ExperienceCollector  -> PlayerLevel.AddXp()   [RULES: XP banked immediately]
       -> XpParticleSpawner    -> XpParticlePool        [VISUAL ONLY: never affects XP]

PlayerLevel (IStatModifierSource, StatLayer.LevelGrowth)
  -> PlayerStats.Recalculate() -> +8 Max HP, +1.5 ATK per level
```

## Assets
| Asset | Path | Default |
|---|---|---|
| XpCurve | Data/Progression/XpCurve.asset | base 60 XP, +18%/level, max level 60 |
| LevelGrowth | Data/Progression/LevelGrowth.asset | +8 Max HP, +1.5 ATK per level (flat) |
| XpParticle | Prefabs/Combat/XpParticle.prefab | pooled, 24 prewarmed |

## XP requirements
| Level | XP to next |
|---|---|
| 1 -> 2 | 60 |
| 2 -> 3 | 71 |
| 3 -> 4 | 83 |
| 5 -> 6 | 116 |
| 10 -> 11 | 266 |

`earlyLevelOverrides` on XpCurve lets the first levels be hand-tuned without touching the formula.

## Pooling refactor
`ComponentPool<T>` is the shared base. `ProjectilePool : ComponentPool<Projectile>` and
`XpParticlePool : ComponentPool<XpParticle>`. Serialized field names (prefab, prewarmCount,
maxPoolSize) are unchanged, so existing scene references survive.

## Notes
- Level-ups heal: +8 Max HP grants +8 current HP (Health preserves missing HP).
- Multiple level-ups from one award each raise their own `LeveledUp` event, so Phase 9 can
  animate them in sequence.
- Level and XP are shared across classes, per the spec.
- No player-facing XP bar yet - that is Phase 9. Use the debug overlay.
