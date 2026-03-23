# AI Docs

This folder is the living system map for AI contributors.

`AGENTS.md` is the policy file. It tells agents how to behave.

This folder explains how the game is actually put together today:

- `architecture.md`: current scene graph, code boundaries, shared systems, and state ownership
- `patterns.md`: repo conventions, Godot usage patterns, and current implementation habits
- `flows.md`: runtime flows from boot through combat, loot, rooms, pause, and victory
- `abstraction-watchlist.md`: hotspots, drift risks, and the next seams to carve out as the project scales

## Read Order For Agents

Read these in this order before making broad or cross-cutting changes:

1. `AGENTS.md`
2. `docs/ai/architecture.md`
3. `docs/ai/flows.md`
4. `docs/ai/patterns.md`
5. `docs/ai/abstraction-watchlist.md`

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
- If you add runtime-created UI or nodes in code, document where they are created and who owns them.
- If you add a reusable system, give it a single obvious home instead of splitting logic across many temporary helpers.

## Current Stage

The repo is still in vertical-slice mode:

- one main gameplay scene
- simple room progression
- single-target combat
- hardcoded numbers
- increasing investment in persistent player state and modifier systems

That is fine for now. The goal of these docs is to help the project grow without losing clarity.
