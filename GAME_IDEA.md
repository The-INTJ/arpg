# Game Concept Document

This document describes the long-term vision for the game. It defines the core philosophy, gameplay structure, and major systems we plan to build toward. It is intentionally conceptual rather than technical — implementation details live in code and CLAUDE.md.

---

## High-Level Concept

A tactical roguelike RPG built around Baldur's Gate–style turn-based combat, procedural exploration, and a player-driven modifier system that allows characters to evolve in unusual and sometimes broken ways.

Players explore procedurally generated maps that occasionally contain handcrafted story encounters. Combat uses spatial positioning similar to Baldur's Gate, but character builds are created through generic stat modifiers rather than traditional items or skill trees.

**The core fantasy is build experimentation:** players gradually assemble strange or powerful combinations by applying simple stat operators to combat variables.

Runs reward experimentation and exploration. Speedrunning is possible but not required.

---

## Design Pillars

### 1. Build Experimentation (Primary Pillar)

The most important element of the game is player-created builds.

Instead of finding predefined gear like "Sword of Fire" or "Boots of Speed," players find **modifiers** — simple operators:

- `+10%`
- `×2`
- `+1`
- `−20%`

Players apply these modifiers to any valid **combat variable**:

- damage
- weapon reach
- attack count
- area of effect
- movement speed
- projectile count
- ability cooldown
- etc.

This creates characters like:

- extremely long-reach melee fighters
- multi-projectile archers
- massive area-of-effect spellcasters
- extremely fast but fragile characters

Builds are expected to sometimes become extremely powerful or even broken — that is part of the design. The interesting challenge becomes *how fast or efficiently* a player can reach that power.

### 2. Tactical Combat (Baldur's Gate Style)

Turn-based tactical combat where spatial positioning matters.

Key characteristics:

- Turn-based (not real-time)
- Spatial positioning on the battlefield matters
- Attack ranges and area-of-effect matter
- Multiple enemies and allies may participate in encounters

Because combat is turn-based and spatially defined, modifiers that affect weapon size, reach, area, or positioning remain visually and mechanically meaningful. This also avoids problems common in real-time games where extreme speed builds could break AI or movement systems.

### 3. Exploration and Procedural Maps

Each level is procedurally generated, but procedural systems may insert handcrafted content.

Maps contain:

- combat encounters
- exploration paths
- occasional story locations
- rewards / modifier drops
- optional encounters

Procedural generation includes slots where handcrafted story snippets can appear. Over time, player choices in story events may influence future generation — narrowing or expanding what kinds of events appear later.

### 4. Light Story and World Interaction

The game contains story elements and NPC interactions, but story is not the primary focus.

Story moments function similarly to those in games like Magicka:

- players encounter NPCs
- dialogue occurs
- small events may unfold
- players may influence outcomes

These encounters may provide rewards (modifiers, items, information). The goal is to create a sense that the player is traveling through a world, not simply clearing combat arenas.

### 5. Player-Controlled Pace

Unlike games like Risk of Rain, the game does **not** force players to move quickly.

Players may choose to:

- explore more thoroughly
- grind for additional modifiers
- build extremely powerful characters before progressing

Skilled players can pursue fast runs by advancing with fewer upgrades. This creates two valid playstyles:

- **Optimization runs** — slow, build-heavy, exhaustive exploration
- **Speed runs** — fast, skill-focused, minimal upgrades

### 6. Multiplayer (Core Consideration)

Multiplayer is a core design consideration from day one, not an afterthought.

The turn-based combat system is inherently compatible with multiplayer. Players should be able to:

- join the same run together
- participate in the same encounters
- fight alongside each other in turn-based battles
- explore cooperatively

Design decisions should account for multiplayer from the start — data structures, turn order, combat flow, and exploration should all be built with the assumption that multiple players will participate. This doesn't mean every feature must ship with multiplayer immediately, but nothing should be designed in a way that makes multiplayer painful to add later.

---

## Core Loop

1. Explore a procedural map
2. Encounter enemies or story events
3. Fight turn-based battles
4. Collect modifiers and rewards
5. Apply modifiers to shape the character build
6. Progress deeper into the world
7. Fight bosses
8. Continue until the run ends

Each run produces different builds and experiences because modifier placement is controlled by the player.

---

## Character Structure

Players begin a run by selecting an **archetype**:

- Fighter
- Archer
- Mage
- (additional archetypes later)

Each archetype starts with:

- a weapon
- basic stats
- possibly a small set of abilities

Over the course of a run, modifiers applied to the character dramatically change how that archetype plays. A fighter could become a long-range melee fighter, a rapid multi-hit attacker, or a large-area sweeping damage dealer.

---

## Modifier System (Central Mechanic)

Modifiers are simple operators rather than predefined upgrades.

Examples:

| Operator | Example |
|----------|---------|
| Add flat  | `+2` |
| Add percent | `+10%` |
| Multiply | `×1.5` |
| Subtract percent | `−25%` |

When a player obtains a modifier, they decide:

1. **Which stat** it affects (damage, reach, attack count, etc.)
2. **Which target** it applies to (the character, a weapon, an ability, etc.)

Modifiers can be stored and applied later, allowing players to plan builds strategically.

---

## Enemy and Reward System

Enemies can drop:

- modifiers
- items
- story progression triggers

Rewards also come from:

- exploration (chests, hidden areas)
- NPC encounters
- story events
- boss fights

Higher difficulty encounters may offer better modifier rewards.

---

## Boss Encounters

Bosses are important progression milestones. Possible structures:

- bosses appearing after several levels
- bosses at the end of each level
- optional bosses encountered during exploration

Boss fights test the player's current build. Boss rewards may include powerful modifiers or unique rewards.

---

## Procedural Story Integration (Long-Term)

In the long term, the game world may track how players interact with story encounters. These interactions could influence:

- which story events appear later
- which factions appear
- what rewards become available
- how the world evolves during the run

This allows procedural generation to feel responsive to player actions.

---

## Design Philosophy

The design intentionally emphasizes:

- **Player creativity** — builds emerge from player decisions, not predefined gear
- **Emergent builds** — a small number of variables + flexible modifiers = unexpected combinations
- **Strategic experimentation** — players discover surprising synergies and develop their own playstyles

Instead of designing hundreds of predefined abilities or items, the game focuses on a smaller number of variables and flexible modifiers that players combine in unexpected ways.
