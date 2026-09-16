# Phase 1 — Reference Card

Full step-by-step instructions were given in chat; this is the short version to keep open
in a second window while you work in the Editor.

## Layers (Project Settings > Tags and Layers)
Player, Enemy, PlayerProjectile, EnemyProjectile, Wall, Hazard, PickupVisual, Environment

## Physics 2D collision matrix (only these pairs stay CHECKED)
| Layer            | Collides with                                   |
|------------------|-------------------------------------------------|
| Player           | Enemy, Wall, Hazard, EnemyProjectile, Environment |
| Enemy            | Player, Enemy, Wall, PlayerProjectile, Environment |
| PlayerProjectile | Enemy, Wall                                     |
| EnemyProjectile  | Player, Wall                                    |
| Hazard           | Player, Enemy                                   |
| PickupVisual     | (nothing)                                       |

## Scene: Assets/_Project/Scenes/TestArena.unity
- Main Camera: Orthographic, Position (0, 0, -10), `CameraFollow2D`
  (Visible World Height 12, Use Bounds on, Bounds Size 50 x 50)
- Arena/Floor: Square sprite, scale 50 x 50, Order in Layer -10
- Arena/Walls/*: Square sprites + Box Collider 2D, layer **Wall**
- Player (tag Player, layer Player): Sprite Renderer (Circle), Rigidbody2D
  (Dynamic, Gravity 0, Freeze Rotation Z, Interpolate), Circle Collider 2D (r 0.4),
  `PlayerMotor`, `PlayerFacing`, `PlaceholderAttack`, `PlayerController`
  - Player/AimPivot (empty at 0,0) -> AimIndicator (Square, pos 0.5,0, scale 0.5 x 0.12)
- HUD Canvas: Screen Space Overlay, Scale With Screen Size, 1080 x 1920, Match 0.5
  - MoveInputArea (stretched over left half, Image alpha 0) + `VirtualJoystick`
    - JoystickBackground (anchor middle-center, 300 x 300) -> JoystickHandle (120 x 120)
  - AttackButton (bottom-right, 220 x 220, Image) + `TouchActionButton`
- DevTools: `KeyboardInputWriter`, `BootstrapValidator`

## Shared asset
Assets/_Project/Data/Input/PlayerInputChannel.asset
Create via: right-click > Create > RPG > Input > Player Input Channel
Assigned to: PlayerController, VirtualJoystick, TouchActionButton, KeyboardInputWriter

## Key tuning fields
| Value              | Where                          | Default |
|--------------------|--------------------------------|---------|
| Move speed         | PlayerMotor > Base Move Speed  | 5       |
| Visible height     | CameraFollow2D                 | 12      |
| Camera smoothing   | CameraFollow2D > Smooth Time   | 0.12    |
| Attack rate        | PlaceholderAttack > Cooldown   | 1/sec   |
| Joystick dead zone | VirtualJoystick                | 0.12    |
| Attack buffer      | PlayerInputChannel             | 0.2 s   |
