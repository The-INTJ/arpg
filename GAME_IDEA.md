# Game Concept Document

This document describes the long-term vision for the game. It defines the core philosophy, gameplay structure, and major systems we plan to build toward. It is intentionally conceptual rather than technical — implementation details live in code, `CLAUDE.md`, and `docs/ai/`.

For the full architectural direction and AI agent guidance, see `docs/ai/direction.md`.

---

## High-Level Concept

A real-time, exploration-first action RPG / roguelite set on a shattered planet of fractured floating islands. Players traverse a vertical world of broken landscapes, discovering routes, navigating danger, recovering power, and restoring the planet piece by piece — while building expressive combat loadouts through a universal modifier system.

**The core fantasy is spatial curiosity and build expression:** players explore a fractured world where movement is the primary verb, combat is sparse but dangerous, and modifiers physically change how they interact with space.

The central question driving every moment: **"Where can I go?"**

---

## Design Pillars

### 1. Traversal and Spatial Curiosity (Primary Pillar)

The most important element is movement through the fractured world.

The world constantly presents visible possibilities: ledges, underhangs, caves, hidden drops, energy nodes, broken bridges, strange structures, and routes that may or may not be immediately accessible. Movement should feel good enough that simple traversal is fun.

Traversal mechanics include:
- Responsive ground movement with weight and momentum
- Jump, dash, ledge grab
- Dark energy platform creation (later)
- Underside exploration and vertical route discovery
- Gap-crossing as a core spatial challenge

The player's thought loop should be: Where can I reach? What path is safest? What is hidden below this island? Do I risk this enemy or avoid it? What does this modifier enable now?

### 2. Build Experimentation Through Modifiers

Instead of finding predefined gear, players find **modifiers** — simple operators:

- `+10%`
- `×2`
- `+1`
- `−20%`

Players apply these modifiers to **any valid stat target**:

- damage, weapon reach, attack count, area of effect
- movement speed, dash distance, jump height
- projectile count, projectile speed
- ability cooldown, ability scale
- traversal-affecting properties

**Any modifier can affect any ability.** This is the flagship mechanic. Players produce strange and expressive builds that alter movement, spacing, scale, range, timing, and utility in ways they can feel physically in the world.

This creates characters like:
- Massive-reach melee fighters who swing through crowds
- Multi-projectile archers covering wide areas
- Speed-stacked explorers who can reach otherwise inaccessible routes
- Heavy-damage glass cannons who must position carefully

Builds are expected to sometimes become extremely powerful — that is part of the design. The interesting dimension is how efficiently and creatively a player reaches that power through exploration.

### 3. Real-Time Combat That Supports Movement

Combat exists to shape and punctuate exploration, not replace it. Key characteristics:

- **Real-time** with animation-driven timing
- Positioning, spacing, and spatial awareness are central
- Enemies are dangerous and consequential — not constant filler
- The player often chooses whether to engage or avoid
- Encounters happen in the same space as exploration — no mode swap
- Hitbox/hurtbox model with wind-up, active frames, and recovery

Combat is not endless swarm-clearing. Individual enemies or small groups create real pressure. Elites are important and reliably rewarding. The world shapes combat: edges, gaps, verticality, and underhangs all matter.

### 4. World Restoration and Hub Expansion

The planet's core fractured, scattering fragments across the world. The player's meta-progression is restoring the planet:

- **Excursions** are focused on movement, exploration, combat, and fragment pursuit
- **Hub returns** are where story, NPC interaction, upgrades, and slower reflection happen
- As fragments are returned, the hub grows: NPCs appear, services unlock, the world visibly heals
- Restored areas alter traversal and open new routes

Players discover NPCs in the fractured world during exploration. Those NPCs later appear in or expand the hub, providing new functionality and narrative depth.

### 5. Optional Narrative Depth

Story enriches the experience without slowing the core loop:

- Players who want deep engagement find a lot of story, lore, and NPC interaction in the hub
- Players who want faster runs or cleaner mechanical play are fully supported
- No mandatory mid-run story interruptions
- Story moments function like discoveries — found, not forced

### 6. Multiplayer (Core Consideration)

Multiplayer is a core design consideration from day one. Data structures, combat flow, and exploration should all be built with the assumption that multiple players may participate. This doesn't mean every feature ships with multiplayer immediately, but nothing should be designed in a way that makes multiplayer painful to add later.

---

## Core Loop

1. Depart hub into a fractured region
2. Explore islands — discover routes, energy nodes, secrets, NPCs
3. Navigate dangerous enemies — fight, avoid, or outmaneuver
4. Collect modifiers and apply them to shape the build
5. Pursue the region's fragment objective
6. Return to hub with fragment and discoveries
7. Hub grows: NPCs arrive, services unlock, world repairs
8. Venture deeper into new regions

Each run produces different builds and exploration experiences.

---

## Reward Structure

Exploration first, risk-taking second, farming last:

- **Energy nodes and world pickups** — primary traversal/progression fuel (~60%)
- **Elite drops** — reliable high-value combat rewards (~25%)
- **Hidden chests/caves/ruins** — exploration rewards
- **NPC discoveries** — long-term hub/world rewards
- **Common enemy drops** — supplemental, not dominant (~15%)

---

## Health, Recovery, and Risk

Health does not automatically regenerate. This creates meaningful tension:

- Limited potion system
- Modest restoration at key milestones (fragments, thresholds)
- Possible modifiers/builds that alter sustain
- Skilled exploration reduces risk indirectly

Too little recovery makes exploration miserable. Too much makes enemies irrelevant. The balance should reward smart play and efficient pathing.

---

## Boss Encounters

Bosses are important build and mastery checks. They test:

- Movement mastery and dodge timing
- Reading telegraphs
- Ability synergy
- Positioning in vertical or fractured spaces

Balance should not require grinding. Extra fighting can make bosses easier, but never mandatory. A player who explored intelligently and played well should be able to succeed.

---

## Character Structure

Players begin a run by selecting an **archetype**:

- Fighter
- Archer
- Mage
- (additional archetypes later)

Each archetype starts with a weapon, basic stats, and possibly a starting ability. Over the course of a run, modifiers dramatically change how that archetype plays.

---

## What This Game Is NOT

- **Not a wave-clearer** — progression should not revolve around killing large numbers of enemies
- **Not a mob-farming loop** — exploration, not enemy density, drives rewards
- **Not a timer-pressured escalation game** — the player controls the pace
- **Not a turn-based tactics game** — combat is real-time and movement-continuous
- **Not two separate games** — exploration and combat share the same space and systems

---

## Design Philosophy

The design intentionally emphasizes:

- **Spatial curiosity** — the world invites exploration through visible possibility
- **Player creativity** — builds emerge from player decisions, not predefined gear
- **Tangible build expression** — modifiers change how the game physically feels, not just numbers
- **Meaningful danger** — enemies are threats, not loot faucets
- **World investment** — restoring the planet makes the player care about the space
- **Movement as the primary verb** — every system supports or rewards moving through the world

The litmus test for every system: **Does this make the player more excited to move through the world, discover routes, and feel their build physically changing how they interact with space?**
