# ARPG MVP Specification

## What This Is

A complete start-to-finish gameplay loop in Godot 4.6 .NET/C#. Not a framework, not an architecture — a runnable loop.

## The Loop (Definition of Done)

1. Game launches to main menu
2. Press Play → loads game scene
3. A generated 3D room layout appears (random selection from hardcoded templates)
4. Player (capsule) moves with WASD in top-down 3D view
5. Walk near an enemy, press Attack button
6. Turn-based combat: player hits enemy, enemy hits back
7. Kill 3 enemies → exit barrier opens
8. Walk to exit → Victory screen appears

If all 8 steps work, the MVP is done. Everything else is out of scope.

## Hard Constraints

- **One** kind of enemy (static, no AI, no movement)
- **One** player action: Attack
- **One** victory path: kill 3, reach exit
- **One** map theme: gray primitive meshes
- **One** progression variable: kill count
- **No**: inventory, save system, pathfinding, animation framework, behavior trees, status effects, equipment, networking, event bus, dependency injection, data-driven definitions, generic combat engine, UI framework

## Stats (Hardcoded)

| Stat | Value |
|------|-------|
| Player HP | 15 |
| Player Attack | 5 |
| Enemy HP | 5 |
| Enemy Attack | 2 |
| Kills to unlock exit | 3 |
| Enemies spawned | 4-5 |

## Scene Structure

```
MainMenu.tscn       — one Play button
Game.tscn            — the entire game
VictoryScreen.tscn   — win text + optional restart
```

### Game.tscn Node Tree

```
Node3D (GameRoot)
  ├── World
  │   ├── Floor/MapRoot
  │   ├── ExitDoor (barrier + trigger)
  │   ├── Enemies (container)
  │   ├── Player (CharacterBody3D + capsule mesh)
  │   └── CameraRig (fixed angled camera)
  └── CanvasLayer
      └── HUD (HP, kill count, Attack button, status text)
```

## Scripts

| Script | Responsibility |
|--------|---------------|
| `MainMenu.cs` | Play button → load Game.tscn |
| `VictoryScreen.cs` | Display win, optional restart/quit |
| `GameManager.cs` | Kill count, unlock exit trigger, victory/defeat |
| `TurnManager.cs` | Turn state enum (PlayerTurn/EnemyTurn/Busy/Victory/Defeat), block input when not player turn |
| `PlayerController.cs` | WASD movement, HP, attack action, receive damage |
| `Enemy.cs` | HP, attack damage, die, attack player |
| `MapGenerator.cs` | Pick from 3-5 hardcoded layouts, place floor/walls/obstacles, return spawn positions |
| `ExitDoor.cs` | Locked/unlocked state, collision trigger → victory |

## Implementation Phases

Build in this exact order. Project must be runnable after each phase.

### Phase 1: App Skeleton
- Create MainMenu.tscn with Play button
- Create empty Game.tscn
- Create VictoryScreen.tscn
- Wire Play button → Game scene, placeholder → Victory scene
- **Checkpoint**: project boots and transitions between scenes

### Phase 2: Player in Empty Room
- Add floor plane to Game.tscn
- Add player CharacterBody3D with capsule mesh
- Add fixed-angle camera rig (top-down-ish, ~45 degrees)
- WASD movement via `_PhysicsProcess()`
- **Checkpoint**: player walks around a flat plane

### Phase 3: One Enemy + Combat
- Add one enemy (CharacterBody3D or StaticBody3D with box mesh)
- Add Attack button to HUD
- Proximity check: enable Attack when near enemy
- Click Attack → enemy loses HP → enemy attacks back → apply damage to player
- **Checkpoint**: can fight and kill one enemy

### Phase 4: Turn Gating
- Add TurnManager with state enum
- Disable Attack button except during PlayerTurn
- Add brief delay/transition for EnemyTurn
- Add defeat check (player HP <= 0)
- **Checkpoint**: combat feels sequential, not instant

### Phase 5: Multiple Enemies + Kill Count
- Spawn 4-5 enemies in Game.tscn
- GameManager tracks kill count
- Dead enemies removed from scene
- HUD shows kill count
- **Checkpoint**: can kill multiple enemies, count displayed

### Phase 6: Locked Exit
- Add barrier mesh near exit area
- Add Area3D trigger behind barrier
- GameManager unlocks exit at 3 kills (barrier disappears)
- Entering trigger → load VictoryScreen.tscn
- **Checkpoint**: full loop works end-to-end

### Phase 7: Minimal Map Generation
- MapGenerator picks from 3-5 hardcoded wall/obstacle layouts
- Random placement of enemies in valid open positions
- Player spawn at one end, exit at other
- **Checkpoint**: each run looks slightly different

## HUD Elements

- Player HP (text)
- Kill count: "Kills: X/3"
- Attack button (enabled only when in range + player turn)
- Status text: "Find and defeat 3 enemies" → "Exit unlocked!"

## Camera

Fixed angle, top-down-ish (isometric-ish). Follows player. No free-look, no mouse camera control. Mouse wheel zoom is optional stretch only.

## Combat Flow

```
PlayerTurn:
  → Attack button enabled if enemy in range
  → Click Attack → nearest enemy takes damage
  → If enemy dead: remove, increment kills, check exit unlock
  → Transition to EnemyTurn

EnemyTurn:
  → Each surviving enemy in range attacks player for fixed damage
  → If player dead: defeat
  → Transition to PlayerTurn
```
