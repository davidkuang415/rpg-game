# Graph Report - rpg-game  (2026-09-17)

## Corpus Check
- 174 files · ~89,595 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 376 file(s) not represented in the graph (top: .meta 283, .asset 71, .prefab 20)

## Summary
- 2484 nodes · 5466 edges · 208 communities (128 shown, 80 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 281 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Camera & Combat Feedback Core
- Component Pooling & VFX
- Economy & Class Debug Data
- Swipe UI & Hub Screen Widgets
- Player Targeting & Melee Combat
- Editor Phase Setup Utilities
- Player Stat Providers
- Equipment Manager & Slots
- Package Manifest Modules
- Package Manifest Dependencies
- Item Economy Sell & Upgrade
- Stage Layout Building (Editor)
- Enemy Data Scene Setup (Editor)
- Phase6 Setup & Enemy Colliders (Editor)
- Phase5 Setup & Level Growth (Editor)
- Inventory Panel UI
- Gear Page UI
- Phase11 Layout Setup (Editor)
- Attack Cooldown System
- Loot Table Generation
- Room Controller Logic
- Phase11 Scene Objects (Editor)
- Class Data & Weapon Definitions
- Packages Lock Dependencies A
- Phase2 Setup & Stat Rules (Editor)
- Player Facing & Motor
- Game Flow Controller
- Character Animator VFX
- Phase9 Setup UI (Editor)
- Stage Controller
- Stage Complete Screen UI
- Stage Failure Handling
- Packages Lock Dependencies B
- Editor UI Sprite Setup
- Enemy Data Definitions
- Enemy Pool Reference
- Stat Modifier Collection
- Stage Data & Registry
- Phase13 Setup & First Clear Bonus (Editor)
- Combat Damage Calculation
- Enemy Controller Logic
- Enemy Motor & Brain
- Health Bar View
- Package Manifest Modules B
- Packages Lock Dependencies C
- Packages Lock Dependencies D
- Packages Lock Dependencies E
- Health Bar Style
- Phase1 Scene Builder (Editor)
- Item Registry & Tile Painter
- Room Exit Logic
- Virtual Joystick Input
- Item Tooltip UI
- Phase3 Setup (Editor)
- Rarity Table & Stat Calculator
- Player Input Channel
- Camera Follow 2D
- Health Component
- Modal Panel UI
- Hub & Stage Select UI
- Phase10 Setup (Editor)
- Stage Manager Setup
- Phase1 Scene Builder Details (Editor)
- Player Reference & Registrar
- Stat Type Definitions
- Docs: Phase10-12 Reference Cards
- Packages Lock Dependencies F
- Touch Action Button Input
- Phase12 Setup UI Feedback (Editor)
- Phase12 Setup Enemy Data (Editor)
- Phase4 Setup (Editor)
- Projectile Pool References
- Stat Block
- Enemy Perception
- Enemy Stats
- Player Attack Router
- Packages Lock Dependencies G
- Packages Lock Dependencies H
- Packages Lock Dependencies I
- Packages Lock Dependencies J
- Camera Shake
- Enemy Attack Base
- Button Press Feedback
- Reward Claimer & Phase8 Setup (Editor)
- Stage Authoring Tools (Editor)
- Debug Confirm Tap Button
- Inventory Manager
- Stage Rewards
- Save Storage Interface
- Training Dummy Debug Tool
- Currency Wallet
- Save Data Structures
- Save Manager Balances & Capacity
- Save Manager Lifecycle
- Docs: Health Bar & Feedback Rationale
- Docs: Item Economy & Stat Design Rationale
- Player Class Files (cross-refs)
- Projectile Behavior
- Docs: Input & Swipe UI Rationale
- Packages Lock Dependencies K
- Packages Lock Dependencies L
- Placeholder Art Shapes (Editor)
- Stats Debug Overlay
- Spawn Point
- Docs: Stage Failure & Rewards Rationale
- Character Body Renderer
- Hit Flash Effect
- Game Layers Masks
- Difficulty Curve Data
- Rarity Enum
- Editor Phase4 Physics Setup
- Frame Rate Limiter
- HUD Canvas Builder (Editor)
- Physics Matrix Setup (Editor)
- Item Economy Upgrade Result
- Enemy Archetype Types
- Archer Bow Attack
- Player Controller & Attack Interface
- Stat Row View UI
- Packages Lock Dependencies M
- Editor Sprite Child Setup
- Placeholder Art Generator (Editor)
- Inventory Config
- Packages Lock — AI Module
- Packages Lock — Umbra Module
- Packages Lock — Wind Module
- SpriteShape Placeholder Shapes (Editor)
- Enemy Brain States
- Weapon Type Enum
- Docs: Save Manager Rationale
- Project Assembly Files
- Circle & Square Placeholder Art
- FloorTile Placeholder Texture
- Docs: Attack & Item Definitions
- Docs: Object Pooling Notes
- RoundedRect Placeholder UI Art
- SoftShadow Placeholder Sprite
- Vignette Placeholder Effect
- Docs: Save Storage Interface
- Docs: Save Data Versioning
- Docs: Stage Registry & Data
- Docs: Character Body Renderer Notes
- Docs: Boss & Loot Table
- Docs: Frame Rate Limiter Notes
- Docs: Stage Kit & Controller
- Docs: Enemy Event & XP Collector
- Docs: Combat Stat Provider
- Docs: Stage Complete Panel Timing
- Ring Placeholder Icon
- Slash Placeholder Sprite
- Docs: Stage Unlock Progress
- Docs: Bag Page
- Docs: Combat Feedback Channel
- Docs: Damage Number
- Docs: Damage Number Pool
- Docs: Gear Page
- Docs: Health Bar Object
- Docs: Hit Spark
- Docs: Hit Spark Pool
- Docs: Hub Page
- Docs: Stage Select Panel
- Docs: Stages Page
- Docs: Stats Debug Overlay
- Docs: Tab Bar
- Docs: Button Press Feedback
- Docs: Item Economy Config
- Docs: Player Stats Binder
- Docs: Room Exit
- Docs: Swing Arc Visual
- Docs: Bootstrap Validator
- Docs: Placeholder Attack
- Docs: Player Facing
- Docs: Archer Class Data
- Docs: Class Registry
- Docs: Knight Class Data
- Docs: Stat Calculator Pipeline
- Docs: Stat Layer System
- Docs: Stat Rules
- Docs: Stat Type Formatting
- Docs: Line Of Sight Query
- Docs: Damage Calculator
- Docs: Damage Info
- Docs: Damage Result
- Docs: Projectile Pierce Count
- Docs: Difficulty Curve
- Docs: Enemy Brain States
- Docs: Enemy Controller
- Docs: Enemy Data
- Docs: Enemy Melee Attack
- Docs: Enemy Motor
- Docs: Enemy Perception
- Docs: Enemy Ranged Attack
- Docs: Enemy Stats
- Docs: Hit Flash
- Docs: Player Death Handler
- Docs: Level Growth
- Docs: XP Curve
- Docs: XP Particle Spawner
- Docs: Arrow Pool
- Docs: Enemy Bolt Pool
- Docs: Game Systems Container
- Docs: Room Controller
- Docs: Stage Root
- Docs: Bag Button
- Docs: Currency Wallet
- Docs: Inventory Config
- Docs: Inventory Manager
- Docs: Inventory Panel

## God Nodes (most connected - your core abstractions)
1. `EquipmentInstance` - 71 edges
2. `EquipmentManager` - 47 edges
3. `Health` - 44 edges
4. `GearPage` - 42 edges
5. `SaveManager` - 41 edges
6. `RoomController` - 41 edges
7. `InventoryPanel` - 41 edges
8. `RPG.Items` - 40 edges
9. `RarityTable` - 39 edges
10. `StageData` - 39 edges

## Surprising Connections (you probably didn't know these)
- `Unity Editor Version 6000.6.0f1` --conceptually_related_to--> `Phase 12 — Reference Card`  [INFERRED]
  ProjectSettings/ProjectVersion.txt → Docs/Phase12-Setup.md
- `Unity Editor Version 6000.6.0f1` --conceptually_related_to--> `Phase 1 — Reference Card`  [INFERRED]
  ProjectSettings/ProjectVersion.txt → Docs/Phase1-Setup.md
- `Phase 6 — Reference Card` --references--> `Phase 10 — Reference Card`  [INFERRED]
  Docs/Phase6-Setup.md → Docs/Phase10-Setup.md
- `VirtualJoystick` --semantically_similar_to--> `SwipePageView`  [INFERRED] [semantically similar]
  Docs/Phase1-Setup.md → Docs/Phase11-Setup.md
- `StageFailureHandler` --semantically_similar_to--> `StageCompleteScreen`  [INFERRED] [semantically similar]
  Docs/Phase10-Setup.md → Docs/Phase9-Setup.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Sequential Unity RPG implementation phases (1-12), each phase's tool building on prior phases' systems** — docs_phase1_setup, docs_phase2_setup, docs_phase3_setup, docs_phase4_setup, docs_phase5_setup, docs_phase6_setup, docs_phase7_setup, docs_phase8_setup, docs_phase9_setup, docs_phase10_setup, docs_phase11_setup, docs_phase12_setup [EXTRACTED 1.00]
- **Shared enemy prefab composition: every archetype wires the same set of components around one damage pipeline** — docs_phase4_setup_enemystats, docs_phase4_setup_health, docs_phase4_setup_hitflash, docs_phase4_setup_enemyperception, docs_phase4_setup_enemymotor, docs_phase4_setup_enemymeleeattack, docs_phase4_setup_enemyrangedattack, docs_phase4_setup_enemybrain, docs_phase4_setup_enemycontroller [EXTRACTED 1.00]
- **Hub UI: swipeable tabbed pages moved stats/bag/stage-list out of the play area** — docs_phase11_setup_hubscreen, docs_phase11_setup_tabbar, docs_phase11_setup_pageviewport, docs_phase11_setup_stagespage, docs_phase11_setup_gearpage, docs_phase11_setup_bagpage, docs_phase11_setup_hubpage [EXTRACTED 1.00]

## Communities (208 total, 80 thin omitted)

### Community 0 - "Camera & Combat Feedback Core"
Cohesion: 0.06
Nodes (14): RPG.Core.Combat, RPG.Enemies, RPG.Core.Events, RPG.Player.Combat, RPG.UI.HUD, RPG.Core.Pooling, RPG.Combat.Projectiles, RPG.Player.Input (+6 more)

### Community 1 - "Component Pooling & VFX"
Cohesion: 0.06
Nodes (19): ComponentPool, Vector2, Color, TextMesh, Vector2, Vector3, DamageNumber, DamageNumberPool (+11 more)

### Community 2 - "Economy & Class Debug Data"
Cohesion: 0.10
Nodes (10): RPG.Progression, RPG.Inventory, RPG.Economy, RPG.Stats, RPG.Loot, RPG.Items, RPG.Stages, system (+2 more)

### Community 3 - "Swipe UI & Hub Screen Widgets"
Cohesion: 0.06
Nodes (23): Button, Color, Image, List, RectTransform, Text, HubScreen, Stages (+15 more)

### Community 4 - "Player Targeting & Melee Combat"
Cohesion: 0.07
Nodes (28): Collider2D, List, Vector2, CombatQueries, IDamageable, IsAlive, Transform, HashSet (+20 more)

### Community 5 - "Editor Phase Setup Utilities"
Cohesion: 0.14
Nodes (14): RPG.EditorTools, RPG.UI.Controls, RPG.UI, RPG.DebugTools, RPG.Save, RPG.Core, system_io, unityeditor (+6 more)

### Community 6 - "Player Stat Providers"
Cohesion: 0.07
Nodes (17): SimpleStatProvider, List, PlayerStats, BaseStats, Current, CurrentClass, ClassData, SpriteRenderer (+9 more)

### Community 7 - "Equipment Manager & Slots"
Cohesion: 0.08
Nodes (24): Dictionary, IEnumerable, IReadOnlyList, EquipmentManager, AllEquipped, EquippedWeapon, SupportedSlots, EquipResult (+16 more)

### Community 8 - "Package Manifest Modules"
Cohesion: 0.05
Nodes (40): com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.imageconversion, com.unity.modules.imgui, com.unity.modules.jsonserialize, com.unity.modules.physics, com.unity.modules.physics2d (+32 more)

### Community 9 - "Package Manifest Dependencies"
Cohesion: 0.05
Nodes (41): dependencies, com.unity.device-simulator.devices, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.modules.accessibility, com.unity.modules.adaptiveperformance, com.unity.modules.ai, com.unity.modules.androidjni (+33 more)

### Community 10 - "Item Economy Sell & Upgrade"
Cohesion: 0.10
Nodes (19): ItemEconomy, Config, SellResult, NotInBag, Success, UnknownItem, IReadOnlyList, List (+11 more)

### Community 11 - "Stage Layout Building (Editor)"
Cohesion: 0.28
Nodes (14): EnemyData, GameObject, Enemies, Phase12StageLayouts, BoxCollider2D, Color, EnemyData, Sprite (+6 more)

### Community 12 - "Enemy Data Scene Setup (Editor)"
Cohesion: 0.09
Nodes (22): EnemyData, List, MenuItem, Scene, SceneAsset, Phase7SetupBuilder, ArmorDefinition, SetId (+14 more)

### Community 13 - "Phase6 Setup & Enemy Colliders (Editor)"
Cohesion: 0.15
Nodes (15): IList, Object, BoxCollider2D, EnemyData, IList, List, MenuItem, Scene (+7 more)

### Community 14 - "Phase5 Setup & Level Growth (Editor)"
Cohesion: 0.10
Nodes (16): GameObject, MenuItem, Scene, SceneAsset, Sprite, SpriteRenderer, Phase5SetupBuilder, EnemyEventChannel (+8 more)

### Community 15 - "Inventory Panel UI"
Cohesion: 0.11
Nodes (9): Button, Color, GameObject, List, RectTransform, StringBuilder, Text, InventoryPanel (+1 more)

### Community 16 - "Gear Page UI"
Cohesion: 0.13
Nodes (9): Button, Color, GameObject, Image, List, RectTransform, StringBuilder, Text (+1 more)

### Community 17 - "Phase11 Layout Setup (Editor)"
Cohesion: 0.23
Nodes (13): RectTransform, Button, Color, GridLayoutGroup, RectTransform, ScrollRect, Text, TextAnchor (+5 more)

### Community 18 - "Attack Cooldown System"
Cohesion: 0.09
Nodes (20): AttackCooldown, AttacksPerSecond, CooldownSeconds, IsReady, NormalizedProgress, LayerMask, PlayerFacing, PlayerStats (+12 more)

### Community 19 - "Loot Table Generation"
Cohesion: 0.10
Nodes (14): List, LootGenerator, IReadOnlyList, List, LootTableData, DropChance, GuaranteedDrop, LuckPerEnemyLevel (+6 more)

### Community 20 - "Room Controller Logic"
Cohesion: 0.10
Nodes (16): Coroutine, GameObject, HashSet, IEnumerator, List, RoomController, AliveEnemyCount, IsFinalRoom (+8 more)

### Community 21 - "Phase11 Scene Objects (Editor)"
Cohesion: 0.13
Nodes (9): GameObject, MenuItem, Scene, SceneAsset, Sprite, SpriteRenderer, TextMesh, CombatFeedbackChannel (+1 more)

### Community 22 - "Class Data & Weapon Definitions"
Cohesion: 0.09
Nodes (20): Color, Sprite, ClassData, AllowedWeaponType, BaseAttackArcDegrees, BaseAttackRange, BodyTint, ClassId (+12 more)

### Community 23 - "Packages Lock Dependencies A"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 24 - "Phase2 Setup & Stat Rules (Editor)"
Cohesion: 0.15
Nodes (11): Color, GameObject, MenuItem, Scene, SceneAsset, SpriteRenderer, Phase2SetupBuilder, List (+3 more)

### Community 25 - "Player Facing & Motor"
Cohesion: 0.12
Nodes (12): Vector2, Transform, Vector2, PlayerFacing, Facing, FacingAngle, Rigidbody2D, Vector2 (+4 more)

### Community 26 - "Game Flow Controller"
Cohesion: 0.14
Nodes (8): IEnumerator, GameFlowController, Stages, Button, List, RectTransform, Text, ClassSelectionPanel

### Community 27 - "Character Animator VFX"
Cohesion: 0.18
Nodes (7): ActiveImpulse, GameObject, IEnumerator, Vector2, Vector3, ActiveImpulse, CharacterAnimator

### Community 28 - "Phase9 Setup UI (Editor)"
Cohesion: 0.15
Nodes (15): Button, Color, GameObject, GridLayoutGroup, Image, MenuItem, RectTransform, Scene (+7 more)

### Community 29 - "Stage Controller"
Cohesion: 0.10
Nodes (13): GameObject, List, Transform, Stage, IReadOnlyList, List, Transform, Vector2 (+5 more)

### Community 30 - "Stage Complete Screen UI"
Cohesion: 0.15
Nodes (9): Button, Coroutine, GameObject, IEnumerator, Image, List, RectTransform, Text (+1 more)

### Community 31 - "Stage Failure Handling"
Cohesion: 0.12
Nodes (5): Health, StageFailureHandler, Button, Text, StageFailedScreen

### Community 32 - "Packages Lock Dependencies B"
Cohesion: 0.09
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, url (+14 more)

### Community 33 - "Editor UI Sprite Setup"
Cohesion: 0.17
Nodes (13): Color, Image, Sprite, Text, TextAnchor, Transform, EditorSetupUtility, Button (+5 more)

### Community 34 - "Enemy Data Definitions"
Cohesion: 0.10
Nodes (21): Color, GameObject, EnemyData, Archetype, Attack, AttackRange, AttackSpeed, AttackWindupSeconds (+13 more)

### Community 35 - "Enemy Pool Reference"
Cohesion: 0.14
Nodes (11): Dictionary, GameObject, Transform, Vector3, EnemyPool, EnemyPoolReference, Current, GameObject (+3 more)

### Community 36 - "Stat Modifier Collection"
Cohesion: 0.13
Nodes (14): List, List, List, StatCalculator, List, StatLayer, Enchantment, Equipment (+6 more)

### Community 37 - "Stage Data & Registry"
Cohesion: 0.12
Nodes (15): GameObject, StageData, DisplayName, EnemyLevel, GoldModifier, IsBossStage, LootModifier, StageNumber (+7 more)

### Community 38 - "Phase13 Setup & First Clear Bonus (Editor)"
Cohesion: 0.17
Nodes (6): GameObject, MenuItem, Scene, SceneAsset, Phase13SetupBuilder, FirstClearBonus

### Community 39 - "Combat Damage Calculation"
Cohesion: 0.16
Nodes (7): ICombatStatProvider, GameObject, Vector2, DamageInfo, DamageResult, IDamageDealtListener, DamageCalculator

### Community 40 - "Enemy Controller Logic"
Cohesion: 0.14
Nodes (9): Collider2D, Color, GameObject, IEnumerator, SpriteRenderer, EnemyController, GameObject, Vector3 (+1 more)

### Community 41 - "Enemy Motor & Brain"
Cohesion: 0.19
Nodes (10): LayerMask, EnemyBrain, Current, Rigidbody2D, Vector2, EnemyMotor, CurrentVelocity, HasDestination (+2 more)

### Community 42 - "Health Bar View"
Cohesion: 0.17
Nodes (5): GameObject, SpriteRenderer, Vector2, Vector3, HealthBarView

### Community 43 - "Package Manifest Modules B"
Cohesion: 0.10
Nodes (19): com.unity.modules.animation, com.unity.modules.assetbundle, com.unity.modules.audio, com.unity.modules.imageconversion, com.unity.modules.imgui, com.unity.modules.jsonserialize, com.unity.modules.physics, com.unity.modules.physics2d (+11 more)

### Community 44 - "Packages Lock Dependencies C"
Cohesion: 0.10
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 45 - "Packages Lock Dependencies D"
Cohesion: 0.11
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 46 - "Packages Lock Dependencies E"
Cohesion: 0.11
Nodes (20): dependencies, depth, source, version, dependencies, depth, source, version (+12 more)

### Community 47 - "Health Bar Style"
Cohesion: 0.11
Nodes (17): Color, Vector2, HealthBarStyle, BackgroundColor, Border, FillColor, HideDelaySeconds, HideWhenDead (+9 more)

### Community 48 - "Phase1 Scene Builder (Editor)"
Cohesion: 0.17
Nodes (9): Camera, MenuItem, Object, SceneAsset, Phase1SceneBuilder, AudioListener, index, name (+1 more)

### Community 49 - "Item Registry & Tile Painter"
Cohesion: 0.15
Nodes (11): Dictionary, IReadOnlyList, List, ItemRegistry, Items, Button, Color, Image (+3 more)

### Community 50 - "Room Exit Logic"
Cohesion: 0.15
Nodes (11): Collider2D, Color, Coroutine, GameObject, IEnumerator, LayerMask, SpriteRenderer, Vector3 (+3 more)

### Community 51 - "Virtual Joystick Input"
Cohesion: 0.15
Nodes (12): Camera, Canvas, PointerEventData, RectTransform, Vector2, JoystickMode, Fixed, Floating (+4 more)

### Community 52 - "Item Tooltip UI"
Cohesion: 0.18
Nodes (10): Canvas, RectTransform, StringBuilder, Text, Vector2, ItemTooltip, PointerEventData, ItemTooltipTrigger (+2 more)

### Community 53 - "Phase3 Setup (Editor)"
Cohesion: 0.20
Nodes (9): CircleCollider2D, GameObject, MenuItem, Scene, SceneAsset, Sprite, SpriteRenderer, Vector2 (+1 more)

### Community 54 - "Rarity Table & Stat Calculator"
Cohesion: 0.23
Nodes (9): Rarity, EquipmentStatCalculator, Color, IReadOnlyList, List, RarityEntry, RarityTable, Entries (+1 more)

### Community 55 - "Player Input Channel"
Cohesion: 0.16
Nodes (7): Vector2, KeyboardInputWriter, Vector2, PlayerInputChannel, AttackHeld, HasBufferedPress, Move

### Community 56 - "Camera Follow 2D"
Cohesion: 0.22
Nodes (6): Camera, Transform, Vector2, Vector3, CameraFollow2D, Target

### Community 57 - "Health Component"
Cohesion: 0.15
Nodes (8): Health, CurrentHealth, HealthFraction, IsAlive, MaxHealth, Transform, Vector2, HitFeedbackEmitter

### Community 58 - "Modal Panel UI"
Cohesion: 0.16
Nodes (6): Coroutine, GameObject, IEnumerator, ModalPanel, IsOpen, CanvasGroup

### Community 59 - "Hub & Stage Select UI"
Cohesion: 0.15
Nodes (10): RectTransform, HubPage, PageName, Button, Color, Image, List, RectTransform (+2 more)

### Community 60 - "Phase10 Setup (Editor)"
Cohesion: 0.21
Nodes (7): Button, Color, MenuItem, Scene, SceneAsset, Vector2, Phase10SetupBuilder

### Community 61 - "Stage Manager Setup"
Cohesion: 0.17
Nodes (7): StageEventChannel, Transform, StageManager, CurrentStage, IsStageActive, StageProgressState, HighestUnlockedStage

### Community 62 - "Phase1 Scene Builder Details (Editor)"
Cohesion: 0.21
Nodes (10): BoxCollider2D, CircleCollider2D, Color, GameObject, Image, Rigidbody2D, Sprite, SpriteRenderer (+2 more)

### Community 63 - "Player Reference & Registrar"
Cohesion: 0.17
Nodes (9): Health, Transform, Vector2, PlayerReference, Damageable, Exists, Health, Position (+1 more)

### Community 64 - "Stat Type Definitions"
Cohesion: 0.17
Nodes (12): StatType, Attack, AttackSpeed, CritChance, CritDamage, Defense, DefensePenetration, DodgeChance (+4 more)

### Community 65 - "Docs: Phase10-12 Reference Cards"
Cohesion: 0.18
Nodes (16): Phase 10 — Reference Card, Phase 11 — Reference Card, HubScreen, ModalPanel, Phase 12 — Reference Card, Sprites regenerated at 4x in place because Phase 1 pixel sizes were being upscaled on high-DPI phones, Phase 1 — Reference Card, Phase 2 — Reference Card (+8 more)

### Community 66 - "Packages Lock Dependencies F"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 67 - "Touch Action Button Input"
Cohesion: 0.16
Nodes (8): ActionType, PointerEventData, Vector3, ActionType, Attack, TouchActionButton, IsHeld, IPointerUpHandler

### Community 68 - "Phase12 Setup UI Feedback (Editor)"
Cohesion: 0.20
Nodes (8): Button, Health, Image, SpriteRenderer, Text, Sprite, GameObject, Shadow

### Community 69 - "Phase12 Setup Enemy Data (Editor)"
Cohesion: 0.23
Nodes (5): EnemyData, MenuItem, Scene, SceneAsset, Phase12SetupBuilder

### Community 70 - "Phase4 Setup (Editor)"
Cohesion: 0.20
Nodes (7): Color, MenuItem, Scene, SceneAsset, Transform, Vector2, Phase4SetupBuilder

### Community 71 - "Projectile Pool References"
Cohesion: 0.21
Nodes (6): ProjectilePool, ProjectilePoolReference, Current, Vector2, EnemyRangedAttack, ProjectileRange

### Community 72 - "Stat Block"
Cohesion: 0.16
Nodes (9): StatBlock, Attack, AttackSpeed, CritChance, CritDamage, Defense, MaxHealth, MoveSpeed (+1 more)

### Community 73 - "Enemy Perception"
Cohesion: 0.14
Nodes (9): LayerMask, Transform, Vector2, EnemyPerception, EyePosition, HasMemory, HasVisual, LastKnownPosition (+1 more)

### Community 74 - "Enemy Stats"
Cohesion: 0.20
Nodes (9): List, EnemyStats, Current, Data, GoldReward, IsElite, Level, XpReward (+1 more)

### Community 75 - "Player Attack Router"
Cohesion: 0.16
Nodes (7): Vector2, PlayerStats, Vector2, WeaponType, PlayerAttackRouter, Active, CanAttack

### Community 76 - "Packages Lock Dependencies G"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 77 - "Packages Lock Dependencies H"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 78 - "Packages Lock Dependencies I"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 79 - "Packages Lock Dependencies J"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 80 - "Camera Shake"
Cohesion: 0.22
Nodes (3): Health, Vector3, CameraShake

### Community 81 - "Enemy Attack Base"
Cohesion: 0.19
Nodes (10): Color, IEnumerator, LayerMask, SpriteRenderer, Vector2, EnemyAttackBase, AttackRange, IsAttacking (+2 more)

### Community 82 - "Button Press Feedback"
Cohesion: 0.20
Nodes (6): Button, PointerEventData, RectTransform, Vector3, ButtonPressFeedback, IPointerExitHandler

### Community 83 - "Reward Claimer & Phase8 Setup (Editor)"
Cohesion: 0.21
Nodes (6): MenuItem, Scene, SceneAsset, Phase8SetupBuilder, ClaimResult, RewardClaimer

### Community 84 - "Stage Authoring Tools (Editor)"
Cohesion: 0.36
Nodes (6): BoxCollider2D, GameObject, MenuItem, Sprite, SpriteRenderer, StageAuthoringTools

### Community 85 - "Debug Confirm Tap Button"
Cohesion: 0.23
Nodes (8): Button, Color, Image, Text, ConfirmTapButton, Confirmed, IsArmed, UnityEvent

### Community 86 - "Inventory Manager"
Cohesion: 0.15
Nodes (11): IReadOnlyList, List, InventoryManager, Capacity, Count, ExpansionsBought, FreeSlots, IsFull (+3 more)

### Community 87 - "Stage Rewards"
Cohesion: 0.15
Nodes (8): IReadOnlyList, List, StageRewards, EnemiesDefeated, GoldEarned, IsEmpty, Items, XpEarned

### Community 89 - "Training Dummy Debug Tool"
Cohesion: 0.21
Nodes (6): Camera, Collider2D, GameObject, IEnumerator, SpriteRenderer, TrainingDummy

### Community 90 - "Currency Wallet"
Cohesion: 0.29
Nodes (6): CurrencyType, Gems, Gold, CurrencyWallet, Gems, Gold

### Community 91 - "Save Data Structures"
Cohesion: 0.29
Nodes (5): List, EquippedItemEntry, RewardStorageEntry, SaveData, SettingsData

### Community 93 - "Save Manager Lifecycle"
Cohesion: 0.24
Nodes (5): SaveManager, HasSave, InitialInventoryCapacity, SaveLocation, Storage

### Community 94 - "Docs: Health Bar & Feedback Rationale"
Cohesion: 0.18
Nodes (11): HealthBarView, HitFeedbackEmitter, Health bar/feedback added to scene Player, not prefab, because the prefab is stale (later phases only patched the scene instance), Health bars built from SpriteRenderers, not a world-space Canvas, to avoid per-enemy mesh rebuild cost on mobile, HitFeedbackEmitter listens to the victim's Health rather than attack code, so one component covers every damage source uniformly, CameraShake, CharacterAnimator, EnemyAttackBase (Telegraphed/Executed) (+3 more)

### Community 95 - "Docs: Item Economy & Stat Design Rationale"
Cohesion: 0.20
Nodes (11): ItemEconomy (TrySell/TryUpgrade), Selling is bag-only so the confirmation moment is always about something already in storage, not an active loadout, Upgrading multiplies base stats only, never enchantments, per the design rule EquipmentStatCalculator was already written around, IStatModifierSource, PlayerStats, PlayerLevel, Enchantments are deliberately excluded from the item stat formula so upgrading only ever raises base stats, EquipmentInstance (+3 more)

### Community 97 - "Projectile Behavior"
Cohesion: 0.31
Nodes (3): Vector2, Projectile, Speed

### Community 98 - "Docs: Input & Swipe UI Rationale"
Cohesion: 0.20
Nodes (10): Drift-with-no-input bug: GetAxisRaw bypassed gamepad dead zone, and Rigidbody2D velocity was never cleared each FixedUpdate, PageViewport (ScrollRect + SwipePageView + RectMask2D), SwipePageView rides on Unity ScrollRect instead of a hand-rolled drag handler so swipe and tap-through don't conflict, SwipePageView, KeyboardInputWriter, PlayerController, PlayerInputChannel (shared asset), PlayerMotor (+2 more)

### Community 99 - "Packages Lock Dependencies K"
Cohesion: 0.20
Nodes (10): dependencies, depth, source, version, dependencies, depth, source, version (+2 more)

### Community 100 - "Packages Lock Dependencies L"
Cohesion: 0.20
Nodes (10): dependencies, depth, source, version, dependencies, depth, source, version (+2 more)

### Community 101 - "Placeholder Art Shapes (Editor)"
Cohesion: 0.22
Nodes (9): Shape, Circle, FloorTile, Ring, RoundedRect, Slash, SoftShadow, Square (+1 more)

### Community 102 - "Stats Debug Overlay"
Cohesion: 0.36
Nodes (4): StatsDebugOverlay, Rect, GUIStyle, KeyCode

### Community 103 - "Spawn Point"
Cohesion: 0.25
Nodes (7): Color, EnemyData, GameObject, Transform, SpawnPoint, EnemyData, SpawnAsElite

### Community 104 - "Docs: Stage Failure & Rewards Rationale"
Cohesion: 0.22
Nodes (9): Death rule keeps stage restart local, never rolls back unlocks or banked XP/loot (§36), StageFailureHandler, StageManager, StageRewardCollector.Pending, RewardClaimer, Rewards are claimed/banked before the reveal animation plays, so quitting mid-animation costs nothing, StageCompleteScreen, StageEventChannel.StageCompleted (+1 more)

### Community 105 - "Character Body Renderer"
Cohesion: 0.29
Nodes (4): GameObject, SpriteRenderer, Transform, CharacterBody

### Community 106 - "Hit Flash Effect"
Cohesion: 0.39
Nodes (3): Color, SpriteRenderer, HitFlash

### Community 107 - "Game Layers Masks"
Cohesion: 0.25
Nodes (6): GameLayers, EnemyMask, HazardMask, PlayerMask, WallMask, BootstrapValidator

### Community 109 - "Rarity Enum"
Cohesion: 0.25
Nodes (7): Rarity, Common, Epic, Legendary, Mythic, Rare, Uncommon

### Community 110 - "Editor Phase4 Physics Setup"
Cohesion: 0.33
Nodes (5): CircleCollider2D, Health, Rigidbody2D, Sprite, SpriteRenderer

### Community 112 - "HUD Canvas Builder (Editor)"
Cohesion: 0.33
Nodes (5): Canvas, RectTransform, CanvasScaler, EventSystem, StandaloneInputModule

### Community 113 - "Physics Matrix Setup (Editor)"
Cohesion: 0.40
Nodes (4): MenuItem, PhysicsMatrixSetup, collidesWith, layer

### Community 114 - "Item Economy Upgrade Result"
Cohesion: 0.33
Nodes (6): UpgradeResult, MaxLevel, NotEnoughGold, NotOwned, Success, UnknownItem

### Community 115 - "Enemy Archetype Types"
Cohesion: 0.33
Nodes (5): EnemyArchetype, Charger, Melee, Ranged, Tank

### Community 116 - "Archer Bow Attack"
Cohesion: 0.40
Nodes (4): Vector2, WeaponType, ArcherBowAttack, WeaponType

### Community 117 - "Player Controller & Attack Interface"
Cohesion: 0.40
Nodes (5): IPlayerAttack, CanAttack, PlayerController, Facing, Motor

### Community 118 - "Stat Row View UI"
Cohesion: 0.40
Nodes (3): Color, Text, StatRowView

### Community 119 - "Packages Lock Dependencies M"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.device-simulator.devices

### Community 120 - "Editor Sprite Child Setup"
Cohesion: 0.40
Nodes (4): Color, Sprite, Transform, Vector3

### Community 122 - "Inventory Config"
Cohesion: 0.40
Nodes (4): InventoryConfig, InitialCapacity, MaxCapacity, SlotsPerExpansion

### Community 123 - "Packages Lock — AI Module"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.ai

### Community 124 - "Packages Lock — Umbra Module"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.umbra

### Community 125 - "Packages Lock — Wind Module"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

### Community 126 - "SpriteShape Placeholder Shapes (Editor)"
Cohesion: 0.50
Nodes (4): SpriteShape, Circle, Ring, Square

### Community 127 - "Enemy Brain States"
Cohesion: 0.50
Nodes (4): State, Attack, Chase, Idle

### Community 128 - "Weapon Type Enum"
Cohesion: 0.50
Nodes (3): WeaponType, Bow, Sword

### Community 129 - "Docs: Save Manager Rationale"
Cohesion: 0.50
Nodes (4): Atomic writes (temp file then move) prevent a crash mid-write from corrupting the profile, SaveManager, DeleteSave only removed the file; ResetProfile now clears live state too, and Save() refuses to write with no class chosen, so a wipe actually sticks, GameFlowController

### Community 131 - "Circle & Square Placeholder Art"
Cohesion: 0.67
Nodes (3): Circle.png (Placeholder Art), Square.png (Placeholder Sprite), Placeholder Art Asset

### Community 132 - "FloorTile Placeholder Texture"
Cohesion: 0.67
Nodes (3): FloorTile.png (Placeholder Floor Tile Texture), Placeholder Art Concept, Tile-Based Level Design

### Community 133 - "Docs: Attack & Item Definitions"
Cohesion: 0.67
Nodes (3): ArcherBowAttack, ItemDefinition, KnightSwordAttack

### Community 134 - "Docs: Object Pooling Notes"
Cohesion: 0.67
Nodes (3): ProjectilePool, ComponentPool<T>, XpParticlePool

## Knowledge Gaps
- **654 isolated node(s):** `Square`, `Circle`, `Ring`, `Square`, `Circle` (+649 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1059 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **80 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `EquipmentManager` connect `Equipment Manager & Slots` to `Economy & Class Debug Data`, `Stat Modifier Collection`, `Phase12 Setup Enemy Data (Editor)`, `Player Stat Providers`, `Stat Block`, `Item Economy Sell & Upgrade`, `Enemy Stats`, `Inventory Panel UI`, `Gear Page UI`, `Phase11 Layout Setup (Editor)`, `Item Registry & Tile Painter`, `Reward Claimer & Phase8 Setup (Editor)`, `Attack Cooldown System`, `Save Manager Lifecycle`, `Class Data & Weapon Definitions`, `Inventory Manager`, `Rarity Table & Stat Calculator`, `Stage Manager Setup`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Why does `StageRewardCollector` connect `Loot Table Generation` to `Economy & Class Debug Data`, `Stage Data & Registry`, `Phase13 Setup & First Clear Bonus (Editor)`, `Stats Debug Overlay`, `Player Stat Providers`, `Item Economy Sell & Upgrade`, `Enemy Data Scene Setup (Editor)`, `Phase9 Setup UI (Editor)`, `Phase5 Setup & Level Growth (Editor)`, `Reward Claimer & Phase8 Setup (Editor)`, `Save Manager Lifecycle`, `Rarity Table & Stat Calculator`, `Stage Rewards`, `Phase10 Setup (Editor)`, `Stage Manager Setup`, `Stage Complete Screen UI`, `Stage Failure Handling`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `EnemyStats` connect `Enemy Stats` to `Camera & Combat Feedback Core`, `Enemy Data Definitions`, `Stat Modifier Collection`, `Phase12 Setup Enemy Data (Editor)`, `Phase4 Setup (Editor)`, `Combat Damage Calculation`, `Enemy Controller Logic`, `Enemy Motor & Brain`, `Enemy Perception`, `Stat Block`, `Difficulty Curve Data`, `Player Stat Providers`, `Editor Phase4 Physics Setup`, `Spawn Point`, `Character Body Renderer`, `Enemy Attack Base`, `Phase2 Setup & Stat Rules (Editor)`, `Character Animator VFX`?**
  _High betweenness centrality (0.033) - this node is a cross-community bridge._
- **What connects `Square`, `Circle`, `Ring` to the rest of the system?**
  _654 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Camera & Combat Feedback Core` be split into smaller, more focused modules?**
  _Cohesion score 0.05745814307458143 - nodes in this community are weakly interconnected._
- **Should `Component Pooling & VFX` be split into smaller, more focused modules?**
  _Cohesion score 0.05714285714285714 - nodes in this community are weakly interconnected._
- **Should `Economy & Class Debug Data` be split into smaller, more focused modules?**
  _Cohesion score 0.10482180293501048 - nodes in this community are weakly interconnected._