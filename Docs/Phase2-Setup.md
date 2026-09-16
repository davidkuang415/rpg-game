# Phase 2 — Reference Card

## Run
`RPG > Phase 2 > Add Class System To Scene` (safe to re-run; existing assets are never overwritten)

## Assets created
| Asset | Path |
|---|---|
| Knight | Assets/_Project/Data/Classes/Knight.asset |
| Archer | Assets/_Project/Data/Classes/Archer.asset |
| ClassRegistry | Assets/_Project/Data/Classes/ClassRegistry.asset |
| StatRules | Assets/_Project/Data/Stats/StatRules.asset |

## Starting stats
| | Knight | Archer | Source |
|---|---|---|---|
| HP | 100 | 75 | spec |
| DEF | 10 | 20 | spec |
| Crit Chance | 5% | 5% | spec |
| Crit Damage | 150% | 150% | spec |
| ATK | 12 | 8 | **chosen, needs approval** |
| Attack Speed | 1.0/s | 1.4/s | **chosen, needs approval** |
| Move Speed | 5 | 5 | **chosen, needs approval** |
| Attack Range | 1.6 | 8 | **chosen, needs approval** |

## Stat pipeline (StatCalculator)
```
Base Class Stats
  -> StatLayer.LevelGrowth     (Phase 5)
  -> StatLayer.Equipment       (Phase 8)
  -> StatLayer.Enchantment     (Phase 12)
  -> StatLayer.Temporary       (abilities, later)
  -> StatRules clamps          (crit <= 100%, dodge <= 60%, ...)
```
Within a layer: all Flat modifiers add, then all PercentAdd sum and apply once.

## Units - never mix these
- CritChance / DodgeChance / LifeSteal / DefPen: fractions (0.05 = 5%)
- CritDamage: multiplier (1.5 = 150%)
- AttackSpeed: attacks per second
- Percent signs exist only in UI formatting (StatTypeInfo.Format)

## Extension points
- New class: create a ClassData asset, add it to ClassRegistry. No code, no scene edits.
- New stat: add to the StatType enum; it flows through blocks, modifiers, rules and UI automatically.
- New stat source: implement IStatModifierSource, call PlayerStats.RegisterSource.
