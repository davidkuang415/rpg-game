# Phase 7 — Reference Card

## Run
`RPG > Phase 7 > Add Loot System`

## Templates vs instances
```
ItemDefinition (ScriptableObject)     EquipmentInstance (plain serializable class)
  "Iron Sword" the concept              one sword someone owns
  itemId "iron_sword"  <---------------  templateId "iron_sword"   (stable string, never an asset ref)
  stat lines, drop weight               instanceId  GUID, unique per item
  weapon type, range, arc               itemLevel, rarity, upgradeLevel, enchantments
```

## Stat formula
```
value = (BaseValue + PerItemLevel x (ItemLevel - 1)) x RarityMultiplier x UpgradeMultiplier
```
Enchantments are deliberately NOT in this formula - upgrading must raise base stats only.

Rarity is a flat multiplier; item level compounds. That is why a Lv50 Rare beats a Lv5 Legendary.

## Rarity table (one asset, all tuning)
| Rarity | Stat x | Weight | Max ench. | Natural ench. | Manual ench. | Recycle x |
|---|---|---|---|---|---|---|
| Common | 1.00 | 55 | 0 | - | no | 1.0 |
| Uncommon | 1.15 | 25 | 0 | - | no | 1.6 |
| Rare | 1.35 | 13 | 0 | - | no | 2.6 |
| Epic | 1.60 | 5 | 1 | 50% | **no** | 4.5 |
| Legendary | 1.90 | 1.8 | 3 | 70% | yes | 9 |
| Mythic | 2.30 | 0.2 | 3 | 100% | yes | 18 |

## Item catalogue
| Item | Profile |
|---|---|
| Iron Sword | balanced: ATK 6 (+1.2/lvl) |
| Broadsword | heavy: ATK 10 (+1.9/lvl), **-0.25 attack speed**, +crit damage |
| Swift Blade | fast: ATK 4 (+0.8/lvl), **+0.35 attack speed**, +crit chance |
| Oak Bow | balanced, range 8 |
| Hunter's Bow | fast, range 9.5, +crit damage |
| Iron Helmet / Chestplate / Leggings | HP + DEF |
| Iron Boots | HP + DEF + **Movement Speed** (boots only) |

## Loot tables
| Table | Drop | Rolls | Min rarity | Used by |
|---|---|---|---|---|
| Standard | 28% | 1 | Common | Grunt, Slinger |
| Elite | 60% | 1 | Uncommon | Brute |
| Boss | 100% | 2 | **Epic** | nothing yet (Phase 15) |

Rarity luck: `weight x luck^tier`, where luck = stage loot modifier x (1 + 0.01 per enemy level).
Higher-level enemies therefore drop better gear without needing separate tables.

## Rewards
Equipment never drops on the floor. A successful roll goes into `StageRewardCollector.Pending`.
Pending rewards survive stage restarts on purpose - dying must not cost loot already earned.
`ClaimAll()` is the only thing that empties them (Phase 9's completion screen will call it).

## Not yet done
- Enchantment rolling (Phase 12) - the rarity rules and the instance's list already exist
- Upgrade multiplier (Phase 11) - `ComputeStats` takes it as a parameter, currently always 1
- Nothing equips these yet (Phase 8)
- Gold is banked in pending rewards but there is no wallet yet
