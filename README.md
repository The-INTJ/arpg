# Fractured World — Real-Time Exploration Action RPG

A real-time, exploration-first action RPG / roguelite built in **Godot 4.6 with C# (.NET 8.0)**. The player traverses a shattered planet of fractured floating islands, restoring the world piece by piece while building expressive combat loadouts through a universal modifier system.

## Core Identity

The central question driving every system: **"Where can I go?"**

This is not a wave-clearer. Not a mob-farming loop. Not a turn-based tactics game. It is a movement-driven exploration game in a fractured vertical world where combat is real, sparse, and dangerous — and where your build physically changes how you move through and interact with space.

## Design Pillars

1. **Traversal & spatial curiosity** — Movement feels good. The world constantly presents visible possibilities: ledges, underhangs, caves, hidden drops, broken bridges.
2. **Real-time combat that supports movement** — Combat punctuates exploration, not replaces it. Enemies are dangerous. Positioning and spatial awareness are central.
3. **Build experimentation through modifiers** — Any modifier can affect any ability. Players produce strange and expressive builds that alter movement, spacing, scale, range, timing, and utility.
4. **World restoration & hub expansion** — Returning fragments heals the planet. The hub grows with NPCs, services, and visible world repair.
5. **Optional narrative depth** — Story enriches the experience without slowing the core loop.

See `GAME_IDEA.md` for the full design vision.

## Current State

The prototype has a working game loop (menu → archetype select → explore → fight → win) built around **turn-based combat**, which is now a **deprecated direction**. The codebase is being redirected toward real-time combat and exploration-first design.

### What works today
- WASD movement with gravity, acceleration, coyote time, and jump
- Procedural map generation with scene slices (caves, platforms, trees)
- Archetype selection (Fighter, Archer, Mage) with divergent base stats
- Full modifier system: random generation, backpack, assignment to stat targets
- Monster effect system with tiered combat hooks
- Item/consumable system with hotbar
- Developer tools and god mode
- Procedural pixel-art character sprites

### What's changing
- Turn-based combat → real-time combat with hitbox/hurtbox model
- Room-based linear progression → open fractured-world exploration
- Enemy farming as primary progression → exploration as primary progression
- No hub → hub-based meta-progression with NPC discovery and world restoration

## Tech Stack

- **Engine:** Godot 4.6
- **Language:** C# / .NET 8.0
- **All Godot-extending C# classes must be `partial`**

## Build & Run

```bash
dotnet build          # Build from project root
# Then open in Godot Editor → F5
```

Main scene: `res://scenes/MainMenu.tscn`

## Controls

| Action | Key |
|--------|-----|
| Move | WASD |
| Jump | Space |
| Attack | E |
| Ability | Q |
| Items | Z through M |
| Pause | Escape |

## Project Structure

```
scenes/                    — .tscn scene files
scripts/
  core/                    — Shared utilities, singletons, global state
  player/                  — Player controller, stats, inventory, weapons, archetypes
  combat/                  — Combat system, enemies, abilities, damage numbers
  modifiers/               — Modifier/stat math system
  monster_effects/         — Enemy effect system
  world/                   — Game scene controller, map generation, camera
  ui/                      — Screen-level UI scripts
  dev/                     — Developer tools, god mode
docs/ai/                   — Architecture docs and AI agent guidance
plans/                     — Development roadmap
```

## Documentation

| Document | Purpose |
|----------|---------|
| `CLAUDE.md` | AI agent instructions and codebase rules |
| `GAME_IDEA.md` | Full game design vision |
| `docs/ai/direction.md` | Game direction and architectural redirection plan |
| `docs/ai/current-state-assessment.md` | Codebase audit: reusable vs dead ends |
| `docs/ai/architecture-plan.md` | Target architecture for the new direction |
| `docs/ai/vertical-slice-plan.md` | MVP plan to prove the game's identity |
| `docs/ai/architecture.md` | Current architecture map |
| `docs/ai/flows.md` | Runtime flow documentation |
| `docs/ai/patterns.md` | Code conventions and patterns |
| `docs/ai/abstraction-watchlist.md` | Scaling risks and hotspots |

## License

Private project — not open source.
