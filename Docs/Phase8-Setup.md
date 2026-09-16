# Phase 8 — Reference Card

## Run
`RPG > Phase 8 > Add Inventory And Equipment`

## Where things live
```
GameSystems                          Player
  CurrencyWallet   gold, gems          EquipmentManager  (IStatModifierSource, StatLayer.Equipment)
  InventoryManager the bag             PlayerStats       recalculates on every equip/unequip
  RewardClaimer    pending -> account  KnightSwordAttack / ArcherBowAttack read range+arc from the weapon
HUD
  InventoryPanel   tap bag item = equip, tap slot = unequip
  BagButton        top-right, opens the panel (pauses the game)
```

## Rules enforced (EquipmentManager only - no other door in)
| Rule | Result |
|---|---|
| Template id unknown | UnknownItem |
| Ring / Amulet before they ship | SlotNotSupported |
| Knight + bow, Archer + sword | WrongClass |
| Unequip with a full bag | InventoryFull |
| Equip when slot occupied | swap: old item goes to the bag (always fits - the new one just left it) |
| Class change makes the weapon illegal | forced to bag, over capacity if needed - never destroyed |

## Capacity (InventoryConfig.asset)
- Start 10, max 60
- Expansion: +5 slots for 50 gems, +25 gems per expansion already bought
- Equipped gear and currency never count

## Stats
Each equipped item's stats are computed once (EquipmentStatCalculator) and cached per slot,
then handed to the central calculator as flat Equipment-layer modifiers. Weapon attack speed is
therefore a BONUS on top of the class's base (Broadsword -0.25, Swift Blade +0.35).

## Claiming
`RewardClaimer` banks pending rewards on StageCompleted: gold -> wallet, items -> bag.
Items that do not fit stay pending (nothing is ever lost). Phase 14 moves them to Reward Storage.
Phase 9's completion screen will turn `claimOnStageComplete` off and claim after the reveal.

## Not yet done
- Save/load: every manager has Set*/load methods ready, no SaveManager yet
- Upgrades (+1, +2): `EquipmentStatCalculator` accepts the multiplier; nothing supplies it yet
- Enchantment stat modifiers (Phase 12)
