# Current State Assessment

> **Context:** The project is redirecting from turn-based combat to real-time exploration-first design. This document audits every major system for reusability under the new direction. Read `direction.md` first.

## What The Prototype Currently Does

- Menu → archetype select (Fighter/Archer/Mage) → explore procedural rooms → turn-based 1v1 combat → collect modifiers → progress through 3 rooms → victory
- WASD real-time movement with gravity, acceleration, coyote time, jump
- Procedural map generation using scene slices (caves, platforms, trees)
- Full modifier system: random generation, backpack, player-assigned stat targets
- Monster effect system with tiered combat hooks
- Consumable item system with hotbar (Z-M)
- Developer tools and god flight mode
- Procedural pixel-art character sprites via SpriteFactory

## System-By-System Assessment

### Fully Reusable (Keep As-Is)

| System | Why |
|--------|-----|
| **PlayerController movement** | Already real-time. Gravity, acceleration, coyote time, jump — this is the foundation for Pillar 1. Needs expansion (dash, gap-crossing), not replacement. |
| **PlayerStats + modifier math** | Pure data model. Stat computation (`+N → +N% → ×M → −N%`) is direction-agnostic. Will need new stat targets (movement-affecting, traversal-affecting) but the pipeline is sound. |
| **Modifier / ModifierEffect / ModifierGenerator** | Core mechanic. The flexible/fixed target system, operator types, and generation logic all carry forward. |
| **Weapon + WeaponStatChannel** | Modifier-to-stat channeling is reusable. Weapon concept may evolve but the data model works. |
| **Palette, GameKeys, SpriteFactory** | Utility systems. No combat dependency. |
| **Scene slice system** | `RockWallSlice`, `CavePocketSlice`, `PlatformRampSlice`, tree variants — all reusable. The slice/anchor architecture is exactly what the new world system needs more of. |
| **SceneSliceAnchor + AnchorKind** | Marker system for spawn points, chest positions, etc. Extend with new kinds, don't replace. |
| **Physics-owns-visuals pattern** | `VisualRoot` separation on player and enemies. Essential for real-time combat animations. |
| **Developer tools framework** | `DeveloperToolsManager` with effect registry. Direction-agnostic. |
| **Damage number system** | `DamageNumber.tscn` instancing. Works in real-time context too. |

### Partially Reusable (Needs Refactoring)

| System | Reusable Parts | Dead Parts |
|--------|---------------|------------|
| **GameManager** | Scene bootstrapping, world building, pickup spawning, room progression concept. | Turn-based aggro scanning, combat-entry gating, per-frame HUD updates tied to turn state. Needs splitting into `RunFlowController`, `HudController`, `EncounterManager`. |
| **CombatManager** | Camera zoom/shake, damage number spawning, combat entry/exit signaling pattern. | Turn alternation, movement locking, sequential action resolution, single-target assumption. **Most combat logic is a dead end.** Real-time needs hitbox/hurtbox, animation-driven timing, multi-target. |
| **TurnManager** | The concept of a state machine tracking game phases (exploring vs combat vs busy). | Turn-specific states (PlayerTurn, EnemyTurn). Replace with a lighter phase tracker or remove entirely if combat is seamless with exploration. |
| **Enemy** | Stats model (HP, damage, sight range, elite/boss flags), visual root pattern, effect badge display. | `ResolveIncomingDamage` as a turn-based exchange method. Needs AI state machine, patrol/aggro/leash behavior, real-time attack patterns. |
| **AggroSystem** | Sight range checking, line-of-sight validation, delay-before-engagement concept. | The "enter combat mode" trigger. In real-time, aggro means the enemy starts chasing/attacking, not a mode swap. |
| **Ability** | Concept of abilities with cooldowns. | Turn-based cooldown ticking (`TickCooldown()` per turn). Needs elapsed-time cooldowns. |
| **Monster effect system** | Effect definitions, tier system, badge display, composable hooks. | Turn-based lifecycle hooks (`OwnerTurnStarted`, `OwnerTurnEnded`). Needs real-time equivalents (on-hit, on-damaged, periodic tick, aura pulse). |
| **MapGenerator** | Hybrid code+slice building model, layout selection, anchor-based spawning. | Linear room assumption (3 rooms, all-enemies-dead-to-progress). Needs island/chunk generation for open exploration. |
| **Item system** | Item definitions, inventory slots, hotkey mapping, pickup flow. | Combat-only usage restriction, turn consumption. Items should work in real-time with cooldowns. |
| **GameState** | Cross-scene state bridging concept. | Linear room progression tracking. Needs hub/excursion state, world restoration state, NPC discovery flags. |

### Architectural Dead Ends (Remove or Replace)

| System | Why |
|--------|-----|
| **Turn alternation in CombatManager** | Player action → delay → enemy retaliation → loop. Fundamentally incompatible with real-time. |
| **Movement locking during combat** | `SetMovementLocked(true)` on combat entry. Real-time combat requires movement. |
| **Action queue / turn handoff** | Sequential action resolution. Real-time needs parallel player/enemy actions. |
| **Room-as-level progression** | 3 linear rooms with "kill all to unlock door." New direction is open fractured-world exploration. |
| **Aggro-triggers-mode-swap** | AggroSystem entering combat mode. Should become aggro-triggers-enemy-pursuit instead. |

## Key Architectural Questions To Resolve

### Combat
- What is the minimum viable real-time combat? Likely: player swing with hitbox, enemy with HP, enemy with simple chase+attack AI.
- How should attack timing and hit detection work? Animation-driven hitbox activation is the standard approach.
- How do abilities translate? Cooldown timers instead of turn counts. Abilities as real-time actions with wind-up, active frames, recovery.

### Movement
- What traversal mechanics beyond jump? Dash, ledge grab, dark energy platforms, wall slide?
- How does the void recovery system change? Still relevant for falling off islands, but the penalty model may shift.

### World
- Handcrafted island chunks with procedural arrangement is the direction. How large are chunks? How do they connect?
- Verticality: islands have tops, sides, and undersides. How is underside exploration made accessible?
- How do fragments/energy nodes get placed? Per-chunk authored positions vs procedural scatter within chunks.

### Progression
- What persists across death? Hub state, NPC discoveries, world restoration, unlocked modifier pools.
- What resets? Current loadout, energy collected this run, health/potions.
- How does the hub-to-excursion loop work mechanically? Scene change? Seamless transition?

## What To Build First

Based on the direction priorities:

1. **Stabilize movement** — The PlayerController already works. Add dash or a second traversal mechanic. Make movement feel intentional and satisfying.
2. **Prototype real-time combat** — One enemy with chase AI, one player attack with hitbox, damage on contact. No turn manager.
3. **Build one interesting island** — A handcrafted chunk with visible routes, a hidden area, and one or two enemy placements.
4. **Connect modifier to something visible** — One modifier that changes attack range or movement speed in a way the player can see and feel.
5. **Stub the hub** — A safe zone the player returns to. One NPC slot. One visible world-change trigger.

## What Not To Touch Yet

- Multi-enemy combat balancing
- Full ability/modifier data-driven pipeline
- Procedural world generation at scale
- NPC dialogue system
- Save/load
- Multiplayer networking
- Sound design
- Full UI overhaul
