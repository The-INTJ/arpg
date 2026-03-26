# Target Architecture Plan

> **Context:** This describes the architecture the project should grow toward under the new real-time exploration-first direction. Read `direction.md` and `current-state-assessment.md` first.

## High-Level Shape

The game has two distinct runtime contexts connected by scene transitions:

```
Hub Scene                          Excursion Scene
┌─────────────────────┐            ┌──────────────────────────────┐
│ HubManager          │            │ ExcursionManager             │
│ ├─ NpcRegistry      │  ← run →  │ ├─ WorldBuilder              │
│ ├─ RestorationState │  results   │ ├─ PlayerController          │
│ ├─ Services/Shops   │            │ ├─ EnemyManager              │
│ ├─ PlayerLoadout    │            │ ├─ CombatSystem              │
│ └─ HubUI            │            │ ├─ PickupSystem              │
└─────────────────────┘            │ ├─ FragmentTracker           │
                                   │ ├─ ExcursionHud              │
                                   │ └─ Camera                    │
                                   └──────────────────────────────┘
```

**RunState** bridges the two: persistent data (hub progression, NPC discoveries, restoration level, unlocked modifiers) survives across runs. Transient data (current HP, collected energy, active modifiers) resets.

## Core Systems

### 1. Player Controller (exists, extend)

**Owner:** `scripts/player/PlayerController.cs`
**Responsibility:** Real-time movement, input handling, ability activation, hitbox management.

Current state is solid for basic movement. Extend with:
- **Dash / dodge** — invincibility frames, directional burst
- **Traversal mechanics** — ledge grab, wall slide, dark energy platform creation (later)
- **Attack input** — triggers weapon swing animation, activates hitbox
- **Ability input** — triggers ability with cooldown timer (elapsed time, not turns)
- **Void recovery** — keep existing system, adjust penalty for new health model

The controller should NOT own combat logic. It should emit signals or call into the combat system when attacks connect.

### 2. Combat System (replace CombatManager)

**Owner:** New `scripts/combat/CombatSystem.cs`
**Responsibility:** Damage resolution, hit detection coordination, combat feedback.

This replaces the turn-based CombatManager. Key differences:

- **No mode swap** — combat happens in exploration space, no movement lock
- **Hitbox/hurtbox model** — Area3D nodes on weapons and enemies, overlap detection
- **Animation-driven timing** — attack wind-up, active frames (hitbox on), recovery frames (vulnerable)
- **Damage pipeline:** attacker stats → modifier pipeline → defense/effects → final damage → feedback
- **Multi-target** — hitbox can overlap multiple enemies
- **Signals:** `DamageDealt`, `EntityKilled`, `PlayerHit`

```
Player presses attack
  → PlayerController triggers attack animation
  → Animation enables weapon hitbox
  → Hitbox overlaps enemy hurtbox
  → CombatSystem.ResolveDamage(attacker, defender, attack_data)
  → Damage numbers spawn
  → Enemy AI reacts (stagger, aggro escalation)
```

**Reuse from CombatManager:** Camera shake, damage number spawning, combat entry/exit camera behavior (for bosses only).

### 3. Enemy AI (replace Enemy)

**Owner:** `scripts/combat/EnemyAI.cs` or per-behavior scripts
**Responsibility:** State machine driving enemy behavior in real-time.

Current Enemy is a data bag with turn-based damage resolution. Replace with:

```
States: Idle → Patrol → Alert → Chase → Attack → Stagger → Leash → Dead
```

- **Idle/Patrol** — default behavior, wander or hold position
- **Alert** — player detected within sight range + LOS. Brief delay (reuse aggro delay concept)
- **Chase** — move toward player, respect navigation/terrain
- **Attack** — wind-up → active hitbox → recovery. Different patterns per enemy type
- **Stagger** — interrupted by player hit, brief vulnerability window
- **Leash** — player left aggro range, return to origin over time
- **Dead** — death animation, drop loot, remove

**Reuse from Enemy:** HP/stats model, elite/boss flags, visual root pattern, effect badges.
**Reuse from AggroSystem:** Sight range checking, LOS validation, delay-before-engagement.

### 4. Modifier & Ability Pipeline (extend existing)

**Owner:** `scripts/modifiers/` and `scripts/player/`
**Responsibility:** Composable stat modification, ability definitions, runtime stat resolution.

The existing modifier math is sound. Extend toward:

**Ability as data:**
```
AbilityDefinition
  - Name, Description
  - CooldownSeconds (float, not turns)
  - DamageMultiplier
  - HitboxShape, HitboxSize, HitboxOffset
  - AnimationKey
  - Tags (melee, ranged, aoe, movement, projectile)
  - ModifiableProperties (list of what modifiers can affect)
```

**New modifier targets:**
- Movement speed, dash distance, dash cooldown
- Hitbox size, attack range, attack speed
- Projectile count, projectile speed, projectile spread
- AoE radius, duration
- Cooldown reduction
- Traversal-affecting properties (jump height, wall slide speed)

**Runtime resolution:**
```
Base value → weapon channel modifiers → temporary buffs → final effective value
```

This is the existing pipeline. It just needs more stat targets and real-time cooldown tracking.

### 5. World Builder (replace MapGenerator)

**Owner:** `scripts/world/WorldBuilder.cs`
**Responsibility:** Assembling explorable regions from island chunks.

Direction: **handcrafted chunks + procedural arrangement**.

```
WorldBuilder
  ├─ IslandChunk (scene)        — authored geometry, anchors, routes
  │   ├─ TraversalAnchors       — where players can reach from/to
  │   ├─ EnemyAnchors           — placed enemy positions
  │   ├─ RewardAnchors          — energy nodes, chests, secrets
  │   └─ ConnectionPoints       — where bridges/gaps to other chunks exist
  ├─ ChunkArranger              — procedural placement logic
  │   ├─ vertical spread
  │   ├─ gap distances
  │   ├─ route connectivity graph
  │   └─ difficulty gradient
  └─ RegionProfile              — biome/difficulty rules for a region
```

**Reuse from MapGenerator:** Scene slice instancing pattern, anchor system.
**Replace:** Linear room layout, wall placement code, fixed 3-room progression.

### 6. Hub System (new)

**Owner:** `scripts/hub/HubManager.cs`
**Responsibility:** Hub scene orchestration, NPC management, restoration display.

```
HubManager
  ├─ NpcRegistry                — discovered NPCs and their current state
  ├─ RestorationTracker         — which fragments returned, world repair level
  ├─ ServiceRegistry            — unlocked shops/upgrades/crafting
  ├─ HubWorldState              — visual state of the hub (repaired areas, new paths)
  └─ HubUI                      — NPC dialogue, loadout management, services
```

The hub is a separate scene from excursions. Player enters hub after returning from a run. Hub state persists across runs.

### 7. Run State (replace GameState)

**Owner:** `scripts/core/RunState.cs`
**Responsibility:** Bridging data between hub and excursion, tracking persistent vs transient state.

```
RunState
  ├─ PersistentState            — survives death
  │   ├─ HubLevel
  │   ├─ DiscoveredNpcs[]
  │   ├─ RestoredFragments[]
  │   ├─ UnlockedModifierPool[]
  │   ├─ UnlockedAbilities[]
  │   └─ UnlockedRegions[]
  ├─ TransientState             — resets on death/return
  │   ├─ CurrentHP
  │   ├─ Potions
  │   ├─ CollectedEnergy
  │   ├─ ActiveModifiers[]
  │   ├─ CurrentLoadout
  │   └─ ExcursionProgress
  └─ SelectedArchetype
```

**Reuse from GameState:** Static cross-scene bridge pattern. Replace the room-number tracking with richer state.

### 8. Progression System (new)

**Owner:** `scripts/core/ProgressionSystem.cs`
**Responsibility:** Energy economy, fragment tracking, unlock gating.

```
Energy sources:
  - World pickups (primary)         ~60% of total
  - Elite/boss drops (secondary)    ~25% of total
  - Common enemy drops (minor)      ~15% of total

Energy sinks:
  - Hub upgrades
  - Service unlocks
  - Modifier pool expansion
  - Loadout slots

Fragment sources:
  - Region objectives (exploration milestones)
  - Boss defeats
  - Hidden world secrets

Fragment sinks:
  - World restoration stages
  - Region unlocks
  - Hub expansion
```

## Data Flow

```
                    ┌─────────────┐
                    │  RunState   │ (persistent + transient)
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
         ┌────────┐  ┌──────────┐  ┌────────┐
         │  Hub   │  │Excursion │  │ Save/  │
         │ Scene  │  │  Scene   │  │ Load   │
         └────────┘  └────┬─────┘  └────────┘
                          │
            ┌─────────────┼──────────────┐
            ▼             ▼              ▼
      ┌───────────┐ ┌──────────┐  ┌───────────┐
      │  Player   │ │  World   │  │  Enemy    │
      │Controller │ │ Builder  │  │ Manager   │
      └─────┬─────┘ └──────────┘  └─────┬─────┘
            │                            │
            ▼                            ▼
      ┌───────────┐              ┌───────────┐
      │  Combat   │◄────────────►│  Enemy AI │
      │  System   │  hit events  │  States   │
      └─────┬─────┘              └───────────┘
            │
            ▼
      ┌───────────┐
      │ Modifier  │
      │ Pipeline  │
      └───────────┘
```

## Ownership Boundaries

| System | Owns | Does NOT Own |
|--------|------|-------------|
| PlayerController | Movement, input, animation triggers | Damage math, enemy behavior |
| CombatSystem | Damage resolution, hit feedback | Who attacks when, enemy AI decisions |
| EnemyAI | Behavior states, attack patterns | Damage numbers, loot drops |
| WorldBuilder | Chunk placement, region assembly | Enemy behavior, combat rules |
| HubManager | NPC state, services, restoration display | Run gameplay, combat |
| RunState | Persistent/transient data bridge | Scene management, gameplay logic |
| ModifierPipeline | Stat math, effect application | UI, input, world placement |

## Migration Path

This architecture doesn't need to be built all at once. The migration should follow the development priorities from `direction.md`:

1. **Movement** — Extend PlayerController. No new systems needed.
2. **Real-time combat** — Build CombatSystem + basic EnemyAI. This is the biggest single change.
3. **Traversal + rewards** — Build one WorldBuilder chunk with reward anchors.
4. **Modifiers in real-time** — Extend modifier targets to affect real-time combat properties.
5. **Hub stub** — Build HubManager with minimal NPC/restoration state.

Each step should produce a playable build. Do not attempt the full architecture in one pass.
