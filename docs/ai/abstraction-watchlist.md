# Abstraction Watchlist

> **Direction change:** Priorities have been reordered for the real-time exploration-first direction. See `direction.md` and `architecture-plan.md`.

This file tracks the places where today's implementation is most likely to break down as the repo transitions to the new direction.

## Priority 1: Direction-Critical Changes

### `scripts/combat/CombatManager.cs` **(DEPRECATED)**

Current state:
- Turn-based combat orchestrator: movement lock, turn alternation, single-target damage exchange
- This is the single largest architectural dead end in the codebase

What to do:
- Do NOT extend this system
- Salvage: camera shake, damage number spawning, combat entry/exit camera behavior (for bosses)
- Replace with: real-time `CombatSystem` using hitbox/hurtbox model (see `architecture-plan.md`)

### `scripts/core/TurnManager.cs` **(DEPRECATED)**

Current state:
- State machine with turn-based states (Exploring, PlayerTurn, EnemyTurn, Busy, Victory, Defeat)

What to do:
- Do NOT extend
- The concept of a game-phase tracker may be lightly reusable, but the turn-specific states are dead ends
- May be removed entirely if combat is seamless with exploration

### `scripts/combat/Enemy.cs` — Needs Real-Time AI

Current state:
- Data bag with turn-based damage resolution methods
- No behavior or movement of its own

What to do:
- Preserve: HP/stats model, elite/boss flags, visual root pattern, effect badges
- Replace: `ResolveIncomingDamage` exchange model with hitbox-based damage
- Add: AI state machine (idle → patrol → alert → chase → attack → stagger → leash)
- Add: Navigation/pathfinding for real-time chase behavior

### `scripts/world/AggroSystem.cs` — Needs Behavior Change

Current state:
- Triggers a mode swap from exploration to turn-based combat

What to do:
- Preserve: sight range checking, LOS validation, delay-before-engagement
- Change: aggro should trigger enemy pursuit/attack behavior, NOT a game-state mode change

## Priority 2: Structural Splits Needed

### `scripts/world/GameManager.cs`

Current problem (unchanged from before, now more urgent):
- Owns too many responsibilities: world setup, HUD, aggro, combat entry, room progression, pause, loot
- Is the main multi-agent merge-conflict magnet
- Contains turn-based assumptions throughout

Likely future split:
- `ExcursionManager` — thin composition root for an excursion scene
- `WorldBuilder` — island/chunk generation and assembly
- `EnemyManager` — enemy spawning and lifecycle
- `ExcursionHud` — HUD presentation
- `PickupSystem` — loot and reward spawning/collection

### `scripts/core/GameState.cs`

Current problem:
- Static global state with linear room progression tracking
- Will need hub/excursion state, world restoration state, NPC discovery flags

Likely future split:
- `RunState` with explicit persistent vs transient data
- Hub progression state
- Save/load layer (later)

### `scripts/player/PlayerStats.cs`

Current problem:
- Owns stat math AND inventory state
- Will grow further as abilities get real-time cooldowns and new modifier targets are added

Likely future split:
- `PlayerBuild` or `Loadout`
- `Inventory` (separate from stat math)
- Stat calculation service

## Priority 3: Systems To Extend

### Modifier Pipeline

Current state: Sound math, correct operator ordering, flexible/fixed targets.

Needed extensions:
- New stat targets: movement speed, dash distance, hitbox size, attack speed, cooldown reduction, traversal properties
- Real-time cooldown tracking (seconds, not turns)
- Ability definitions as data with modifiable properties

### Scene Slice System

Current state: Working slice architecture with anchors.

Needed extensions:
- Island chunk scenes (larger than current room slices)
- Connection point anchors (where chunks link to other chunks)
- Traversal anchors (where players can reach from/to)
- Reward anchors (energy nodes, chests, secrets)

### Monster Effect System

Current state: Composable effect hooks with tiers and badges.

Needed extensions:
- Replace turn-based hooks (`OwnerTurnStarted`, `OwnerTurnEnded`) with real-time equivalents
- New hooks: `OnHit`, `OnDamaged`, `PeriodicTick`, `AuraPulse`, `OnDeath`
- Possibly extend to player buffs/debuffs (same pattern, different direction)

## Priority 4: Drift Risks (Unchanged)

### Scene Versus Code Ownership
- Pick one canonical ownership model per reusable thing
- Default to `.tscn` scenes for reuse; code creation only when runtime generation is clearly better

### Styling Drift
- Route all stable colors through `Palette`

### Input Drift
- Use `GameKeys` for all action names in gameplay code and UI text

## Multi-Agent Collision Areas

If several contributors are working at once, these files will collide first:

- `scripts/world/GameManager.cs`
- `scripts/player/PlayerStats.cs`
- `scripts/core/GameState.cs`
- `scripts/combat/CombatManager.cs` (until replaced)
- `scenes/Game.tscn`

Land new work by creating smaller owned units around these files instead of stacking into them.
