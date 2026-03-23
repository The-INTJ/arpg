# Abstraction Watchlist

This file tracks the places where today's simple implementation is most likely to break down as the repo scales.

## Priority 1 Hotspots

### `scripts/GameManager.cs`

Current problem:

- owns too many responsibilities
- is the main multi-agent merge-conflict magnet
- mixes world setup, HUD, aggro, combat entry, room progression, pause integration, and loot callbacks

Likely future split:

- `RunFlowController` or `RoomFlowController`
- `GameHudController`
- `AggroSystem`
- `EncounterSpawner`
- a thin gameplay composition root that wires them together

### `scripts/CombatManager.cs`

Current problem:

- assumes one player and one enemy target
- assumes no movement during combat
- assumes attack and ability are the only player actions

Likely future split:

- combat state model
- action resolution pipeline
- turn order / initiative system
- target selection system
- combat presentation layer for camera and VFX

### `scripts/GameState.cs`

Current problem:

- static global state is convenient, but will become brittle for richer run state, saves, multiplayer, or meta-progression

Likely future split:

- explicit run/session model
- save/load layer
- clearer ownership of transient versus persistent state

## Priority 2 Drift Risks

### Scene Versus Code Ownership

Current problem:

- some reusable nodes exist as scene files, but runtime creation happens directly in code
- this makes it unclear whether the canonical source is the `.tscn` file or the C# constructor logic

Examples:

- `scenes/ModifyStatsSimple.tscn` exists, but `GameManager` uses `new ModifyStatsSimple()`
- `scenes/LootPickup.tscn` exists, but `GameManager` uses `new LootPickup()`
- `scenes/PauseScreen.tscn` exists, but `Game.tscn` embeds a node with the script directly

Recommended rule:

- pick one canonical ownership model per reusable thing
- if a scene exists for reuse, prefer instancing the scene
- if code-only construction is the intent, keep the scene out of the critical path and document that choice

### Styling Drift

Current problem:

- `Palette` is supposed to centralize colors, but runtime scripts and scene resources still introduce direct color values

Recommended rule:

- route all stable world and UI colors through `Palette`
- only allow local one-off colors when the value is truly ephemeral presentation logic

### Input Drift

Current problem:

- `GameKeys` centralizes action names for some inputs, but movement still uses raw strings

Recommended rule:

- use `GameKeys` for all action names that appear in gameplay code or UI text

## Priority 3 Domain Boundaries To Strengthen

### Player Build And Inventory

Current problem:

- `PlayerStats` already owns stat math, current HP, weapon slots, and backpack modifiers
- future items, equipment, temporary buffs, and multiplayer loadouts can easily turn it into a second god object

Likely future split:

- `PlayerBuild` or `Loadout`
- `Inventory`
- temporary combat effects or status layer
- stat calculation service or aggregator

### Enemy Definitions

Current problem:

- enemy stats and presentation are composed ad hoc in `GameManager` and `Enemy`
- boss logic is a one-off mutation path, not a reusable enemy definition model

Likely future split:

- enemy definition/data object
- enemy factory or scene variants
- behavior-specific scripts

### Room Generation

Current problem:

- `MapGenerator` handles wall placement and spawn-set selection, while `GameManager` decides encounter counts and boss designation

Likely future split:

- room layout generation
- encounter generation
- progression-based difficulty rules

## Multi-Agent Collision Areas

If several contributors are working at once, these files will collide first:

- `scripts/GameManager.cs`
- `scripts/PlayerStats.cs`
- `scripts/GameState.cs`
- `scripts/CombatManager.cs`
- `scenes/Game.tscn`

Try to land new work by creating or extending smaller owned units around those files instead of stacking more unrelated logic into them.

## Concrete Risks Already Visible

- `scripts/GameManager.cs` is far past the repo's preferred script size guideline.
- `scripts/VictoryScreen.cs` reloads `Game.tscn` directly and does not reset run state, which can preserve stale room/player data.
- input handling for combat and loot shares the same action keys, which may cause overlap pressure as interaction complexity grows.
- current combat and state flow are not yet multiplayer-shaped even though the project wants multiplayer-aware foundations.

## Suggested Documentation Growth

As the project expands, this folder should eventually gain:

- `combat.md`
- `inventory-and-items.md`
- `rooms-and-progression.md`
- `ui-ownership.md`
- `multiplayer-readiness.md`

Do not add those early just to be comprehensive. Add them when a real subsystem has enough moving parts to deserve its own map.
