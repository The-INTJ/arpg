# AI Docs

This folder is the living system map for AI contributors.

`AGENTS.md` is the policy file. It tells agents how to behave.

This folder explains how the game is actually put together today:

- `architecture.md`: current scene graph, code boundaries, shared systems, and state ownership
- `../assets-pipeline.md`: asset creation, import, wrapper-scene ownership, and physics-vs-visual rules
- `rooms-and-slices.md`: current room-building contract, scene-slice rules, and world-authoring seams
- `patterns.md`: repo conventions, Godot usage patterns, and current implementation habits
- `flows.md`: runtime flows from boot through combat, loot, rooms, pause, and victory
- `inventory-and-items.md`: current item inventory implementation, hotkeys, combat behavior, and intended future direction
- `abstraction-watchlist.md`: hotspots, drift risks, and the next seams to carve out as the project scales

## Read Order For Agents

Read these in this order before making broad or cross-cutting changes:

1. `AGENTS.md`
2. `docs/ai/architecture.md`
3. `docs/assets-pipeline.md` if your change touches models, materials, world slices, actor visuals, or collision ownership
4. `docs/ai/rooms-and-slices.md` if your change touches room building, reusable world geometry, or level-authoring seams
5. `docs/ai/flows.md`
6. `docs/ai/inventory-and-items.md` if your change touches item flow, inventory, or hotkeys
7. `docs/ai/patterns.md`
8. `docs/ai/abstraction-watchlist.md`

For small local changes, read `AGENTS.md` plus the doc most related to the area you are touching.

## Source Of Truth

Use this precedence when docs and code disagree:

1. Runtime code and scene references
2. `AGENTS.md`
3. This folder
4. `plans/`

If you discover drift, update this folder in the same change when practical.

## When To Update These Docs

Update this folder whenever a change does one of these:

- adds a new scene, manager, gameplay loop, or shared system
- changes node paths or scene ownership assumptions
- changes the order of a player-facing flow
- introduces a new abstraction boundary
- removes or replaces a hotspot listed in `abstraction-watchlist.md`

## Multi-Agent Working Rules

When multiple humans and AIs are working here, prefer narrow ownership:

- Treat `scripts/GameManager.cs` as a coordination hotspot. Avoid mixing unrelated features in the same change.
- Treat `scripts/PlayerStats.cs`, `scripts/GameState.cs`, and `scripts/CombatManager.cs` as shared core systems. Read their current callers before editing them.
- Prefer `.tscn` scene files for UI work. Only create UI controls in code when that is genuinely the best Godot pattern for the job, and document that choice if you do it.
- If you add runtime-created nodes in code, document where they are created and who owns them.
- If you add a reusable system, give it a single obvious home instead of splitting logic across many temporary helpers.

## Current Stage

The repo is still in vertical-slice mode:

- one main gameplay scene
- simple room progression
- single-target combat
- hardcoded numbers
- increasing investment in persistent player state and modifier systems

That is fine for now. The goal of these docs is to help the project grow without losing clarity.
