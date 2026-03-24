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
- current action surface is still hand-authored: attack, weapon ability, and a tiny item-action path

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

- reusable pickups are already scene-backed
- reusable world geometry is only starting to move toward scene ownership through scene slices
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

- `PlayerStats` already owns stat math, current HP, weapon slots, backpack modifiers, and the current item inventory
- the current item MVP is consumable-based, but the intended direction is permanent items with cooldown skills
- future items, equipment, temporary buffs, and multiplayer loadouts can easily turn it into a second god object

Likely future split:

- `PlayerBuild` or `Loadout`
- `Inventory`
- `ItemInstance` or cooldown-bearing item state
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
- room geometry was previously almost entirely hand-built in code, which made visual iteration slower than it needs to be

Current progress:

- reusable world scene slices are now a live seam
- the cave pocket is the first slice instanced by `MapGenerator`

Likely future split:

- room layout generation
- room layout scenes composed from reusable slices
- encounter generation
- progression-based difficulty rules

Best next targets:

- full ridge and mesa layout scenes
- repeated platform/ramp feature groups
- tree or rock dressing clusters
- distant chunk variants

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
- item behavior is still a kind-switch in `scripts/GameManager.cs`, which is acceptable for now but will not scale to many item skills.
- `scripts/PlayerStats.cs` now owns both modifier math and inventory state, which increases cross-system merge pressure.
- current combat and state flow are not yet multiplayer-shaped even though the project wants multiplayer-aware foundations.

## Suggested Documentation Growth

As the project expands, this folder should eventually gain:

- `combat.md`
- `rooms-and-progression.md`
- `ui-ownership.md`
- `multiplayer-readiness.md`

`inventory-and-items.md` exists now because the subsystem crossed that threshold. Keep it updated as item intent and implementation evolve.
