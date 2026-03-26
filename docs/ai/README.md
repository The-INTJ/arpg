# AI Docs

This folder is the living system map for AI contributors.

`AGENTS.md` is the policy file. It tells agents how to behave.

## Direction Change Notice

**The project has redirected from turn-based combat to real-time exploration-first design.** Read `direction.md` before making any architectural or design decisions. Documents written before this redirect may contain references to turn-based systems — those references describe deprecated architecture unless explicitly noted as reusable.

## Document Index

### Direction & Planning (read first for new work)

- `direction.md`: **authoritative game direction** — design pillars, architectural rules, anti-goals, development priorities, and the litmus test for every proposed system
- `current-state-assessment.md`: codebase audit — what's reusable, what's partially reusable, what's a dead end under the new direction
- `architecture-plan.md`: target architecture proposal — core systems, ownership boundaries, data flow, and migration path
- `vertical-slice-plan.md`: MVP plan — milestones, success criteria, what to defer, risk flags

### Current State (describes what exists today)

- `architecture.md`: current scene graph, code boundaries, shared systems, and state ownership
- `../assets-pipeline.md`: asset creation, import, wrapper-scene ownership, and physics-vs-visual rules
- `rooms-and-slices.md`: current room-building contract, scene-slice rules, and world-authoring seams
- `patterns.md`: repo conventions, Godot usage patterns, and current implementation habits
- `flows.md`: runtime flows from boot through combat, loot, rooms, pause, and victory (**describes deprecated turn-based flows**)
- `inventory-and-items.md`: current item inventory implementation, hotkeys, combat behavior, and intended future direction
- `blender-mcp.md`: BlenderMCP server usage guide
- `abstraction-watchlist.md`: hotspots, drift risks, and next seams to carve out

## Read Order For Agents

### For new features or architectural work:
1. `AGENTS.md`
2. `docs/ai/direction.md`
3. `docs/ai/current-state-assessment.md`
4. `docs/ai/architecture-plan.md`
5. `docs/ai/architecture.md` (current state)
6. Relevant domain doc for the area you're touching

### For small local changes:
1. `AGENTS.md`
2. `docs/ai/direction.md` (just the litmus test and anti-goals)
3. The doc most related to the area you're touching

## Source Of Truth

Use this precedence when docs and code disagree:

1. `docs/ai/direction.md` (game direction and design rules)
2. Runtime code and scene references (current implementation)
3. `AGENTS.md` (agent behavior policy)
4. This folder (architecture, flows, patterns)
5. `plans/` (forward-looking notes)

If you discover drift, update this folder in the same change when practical.

## When To Update These Docs

Update this folder whenever a change does one of these:

- adds a new scene, manager, gameplay loop, or shared system
- changes node paths or scene ownership assumptions
- changes the order of a player-facing flow
- introduces a new abstraction boundary
- removes or replaces a hotspot listed in `abstraction-watchlist.md`
- migrates a system from the old turn-based architecture to the new real-time architecture

## Multi-Agent Working Rules

When multiple humans and AIs are working here, prefer narrow ownership:

- Treat `scripts/GameManager.cs` as a coordination hotspot. Avoid mixing unrelated features in the same change.
- Treat `scripts/PlayerStats.cs`, `scripts/GameState.cs`, and `scripts/CombatManager.cs` as shared core systems. Read their current callers before editing them.
- Prefer `.tscn` scene files for UI work. Only create UI controls in code when that is genuinely the best Godot pattern for the job.
- If you add runtime-created nodes in code, document where they are created and who owns them.
- If you add a reusable system, give it a single obvious home instead of splitting logic across many temporary helpers.

## Current Stage

The repo is in **direction transition**:

- The old turn-based vertical slice is complete but deprecated
- New direction: real-time exploration-first action RPG
- Next milestone: movement feel + real-time combat prototype (see `vertical-slice-plan.md`)
- Existing modifier/stat systems are reusable and should be preserved
- Turn-based combat code should not be extended, only mined for reusable parts
