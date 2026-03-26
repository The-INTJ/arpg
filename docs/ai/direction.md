# Project Direction: Real-Time Exploration-First Action RPG

> **This document is the authoritative source for game direction.** When other docs contradict this one, this document wins. AI agents should read this before making architectural or design decisions.

## Status

**Effective immediately.** The prototype was built around turn-based combat. That direction is now deprecated. All new work should assume real-time combat and exploration-first design.

## Core Identity

The game is a **real-time, exploration-first, movement-centered action RPG / roguelite** built around traversing a shattered planet made of fractured floating islands.

The central experiential question is: **"Where can I go?"**

Not "What enemy pack do I farm next?" Not "How do I survive a turn-based encounter?" Not "How fast can I clear this wave?"

This is not a Risk of Rain clone. Not a Baldur's Gate-in-space clone. It is:

- Movement-driven traversal in a fractured vertical world
- Meaningful but sparse and dangerous combat
- Exploration as the primary source of reward and progression
- Build expression through modifier + ability combinations
- Hub-based story and world restoration between excursions

## Why Turn-Based Combat Was Removed

The game's strongest pillars are real-time movement, spatial traversal, crossing gaps between islands, vertical and underside exploration, and physical expression of build changes. Turn-based combat acts as a hard interruption to momentum — it pulls the player out of the game rather than deeper into it.

**Architecture, systems, UX, and future prototyping should all assume combat is real-time.**

## Why Exploration Is Core, Not Mob-Farming

The player fantasy is moving through a fractured world that used to be whole, discovering routes and hidden spaces, navigating danger, recovering power, and restoring the planet piece by piece.

The project should be structured so that:

- Exploration is the main source of progression and resources
- Enemies are dangerous and consequential, not constant filler
- Combat is meaningful, not repetitive clearing
- The world matters mechanically, not just aesthetically

The player's thought loop should be: Where can I reach? What path is safest? What is hidden below this island? Do I risk this enemy or avoid it? What does this modifier enable now?

## Design Pillars (Priority Order)

### Pillar 1: Traversal and Spatial Curiosity

Movement should feel good enough that simple traversal is fun. The world should constantly present visible possibilities: ledges, underhangs, caves, hidden drops, energy nodes, broken bridges, strange structures, and routes that may or may not be immediately accessible.

### Pillar 2: Real-Time Combat That Supports Movement

Combat exists to shape and punctuate exploration, not replace it. Enemies should matter. Encounters should be threatening. Positioning, movement, and spatial awareness should be central. Combat is not endless swarm-clearing.

### Pillar 3: Build Experimentation Through Modifiers

**Any modifier can affect any ability.** This is the flagship mechanic. Players should produce strange and expressive builds that alter movement, spacing, scale, range, timing, or utility in ways they can feel physically in the world.

"Any mod on any ability" does not mean anarchic chaos. It means a broad combinatorial system with smart internal consistency — categories, tags, stacks, additive vs multiplicative effects, scaling rules, and cost/cooldown/risk tradeoffs.

### Pillar 4: World Restoration and Hub Expansion

Returning fragments and healing the planet should visually and mechanically change the hub and broader world. NPCs appear in the hub after being found in exploration. World repair should be visible and rewarding.

### Pillar 5: Optional Narrative Depth

Story enriches the experience without slowing the core loop. Deep engagement is available but never required. Hub is where story lives, not mid-run interruptions.

## Hub Structure

- **Excursions / runs** are focused on movement, exploration, combat, and objective pursuit
- **Hub returns** are where time effectively pauses — story, NPC interaction, upgrades, and slower reflection happen here
- The hub is a restored/stabilizing central area of the fractured planet
- As the player restores core fragments, areas stabilize, NPCs spread outward, and the world visibly changes

## Enemy Design Philosophy

Enemies are dangerous enough to matter, sparse enough to remain readable and fair. Not swarm-clearing.

Enemies act as:
- Obstacles and route guardians
- Pressure points and skill checks
- Meaningful risk/reward decisions (engage or avoid?)

Elites should be important and reliably rewarding. Common enemies provide supplemental resources, but exploration is the more reliable progression path.

## Reward Structure

Exploration first, risk-taking second, farming last:

- **Energy nodes and world pickups** — primary traversal/progression fuel
- **Elite drops** — reliable high-value combat rewards
- **Hidden chests/caves/ruins** — exploration rewards
- **NPC discoveries** — long-term hub/world rewards
- **Small enemy drops** — supplemental, not dominant

## Health, Recovery, and Risk

Health does not automatically regenerate. Potions matter. Modest restoration at key milestones. This supports meaningful combat risk and survival tension while rewarding successful progression.

Direction:
- Limited potion system
- Modest restoration at key milestones (fragments, thresholds)
- Possible modifiers/builds that alter sustain
- Skilled exploration should reduce risk indirectly

## Boss Design

Bosses test movement mastery, reading telegraphs, ability synergy, and positioning in vertical/fractured spaces. Balance should not require grinding — extra fighting can make bosses easier, but never mandatory.

## NPC Discovery Model

- Player discovers NPCs in the fractured world during exploration
- Those NPCs later appear in or expand the hub
- Hub gains new functionality and narrative depth over time
- This avoids mandatory mid-run NPC interaction

## World Restoration Model

The planet core fractured into pieces. Each fragment is restored in stages:
- Fragment states and restored region states
- Hub/world areas become less fractured as restoration progresses
- Restored areas alter traversal and visuals
- NPC placement and unlocked services connect to restoration milestones

---

## Architectural Direction

### Remove Turn-Based Assumptions

The game should no longer be structured around:
- Entering enemy zone triggers a separate turn loop
- Action queues
- Turn order controllers as central gameplay managers
- Heavy mode swaps between exploration and combat resolution

AI agents should identify which parts of existing turn-based code are reusable (cooldown systems, attack sequencing, enemy AI phases, boss scripting) and which are architectural dead ends.

The codebase should move toward **continuous simulation**:
- Player controller with real-time movement
- Combat interaction system (hitboxes, hurtboxes, attack timing)
- Enemy AI/state machines
- Ability execution pipeline
- Modifier application layer
- Resource and progression systems
- Traversal interaction systems

### Separate Runtime From Meta Systems

**Runtime / excursion systems:**
- World traversal, enemy placement, combat, pickups
- Health, potions, energy collection, fragment pursuit
- Region progression, environmental hazards
- Movement upgrades active during current run

**Hub / meta systems:**
- NPC presence and dialogue
- World restoration state, unlocked regions/routes
- Persistent unlocks, modifier pools
- Discovered NPC registry, story progression
- Hub upgrades and services

These should be architecturally distinct.

### Data-Driven Modifiers and Abilities

The modifier/ability system should be designed around extensibility:
- Ability definitions as resources/data assets
- Modifier definitions as composable data with explicit application rules
- Runtime stat/effect pipeline for resolving final values
- Tagging system for what a modifier can affect and how

Categories to support: size scaling, duration scaling, range scaling, collision scaling, projectile/melee/aura/movement effect distinctions, cost/cooldown/risk tradeoffs.

### Combat Preserves Movement Continuity

Combat happens in the same traversal space the player is exploring. No isolated arenas (except possibly scripted bosses). This means:
- Attacks in world space
- Enemy aggro and leash logic
- Combat readable across vertical terrain
- Collision and pathing around uneven environments
- Combat interactions near edges, gaps, and undersides

### World Must Support Curiosity

Fully noise-randomized terrain will not create the intended exploratory feel. The world should be generated from handcrafted chunks + procedural arrangement:
- Geography is readable
- Landmarks are visible
- Routes are layered
- Some secrets are optional but rewarding
- The environment communicates possibility

---

## Anti-Goals

AI agents should guard against these failure modes:

1. **Swarm-clearer by accident** — Don't let all progression revolve around killing large numbers of enemies
2. **Mandatory mid-run story interruption** — Don't make the best play require frequent dialogue during exploration
3. **Over-randomized geometry** — Don't assume noise-based generation will feel exploratory
4. **Technically broad but experientially bland modifiers** — "Any mod on any ability" only matters if combinations are visible, legible, and fun
5. **Premature expansion into two games** — Build one strong core identity first
6. **Overengineering around future scale** — Prove the feel and architecture first

---

## Development Priorities

In order:

1. **Movement feel** — Before deep content, ensure moving through the game feels good
2. **Real-time combat feel** — Basic combat must feel responsive, dangerous, and spatially coherent
3. **Traversal + reward loop** — Exploration must pay off through energy, secrets, or discoveries
4. **Modifier system foundations** — Smallest version demonstrating the design hook
5. **Hub + restoration loop** — Fragment return, world change, NPC/hub expansion structure

Everything else comes later.

---

## Vertical Slice Target

The smallest slice proving the direction works:

- One small but interesting fractured exploration area
- Satisfying movement
- At least one gap-crossing / traversal mechanic
- One or two dangerous enemies
- One elite or mini-boss
- Basic energy node / environmental reward system
- One or two abilities
- A few meaningful modifiers that clearly alter feel
- Simple return-to-hub loop
- One visible world restoration consequence

Success criteria: A player experiences the slice and thinks "I want to keep exploring this world, and my build changes how I move and fight in it."

---

## Litmus Test

For every proposed system, ask:

**Does this make the player more excited to move through the world, discover routes, and feel their build physically changing how they interact with space?**

If not, it is probably secondary, mis-scoped, or the wrong direction.
