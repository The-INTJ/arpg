# Inventory And Items

## Current System

The game now has a basic room-item system layered on top of the modifier/weapon loop.

Today it works like this:

- the player has a small run inventory owned by `PlayerStats`
- inventory capacity starts at 2 slots
- one cave chest is spawned per room and releases an item pickup when opened
- elite enemies can also drop item pickups on death
- pickups auto-store into the first open slot
- hotkeys are reserved from `Z` through `M`, but only the first two slots are active by default because capacity is 2

Core files:

- `scripts/PlayerInventory.cs`
- `scripts/InventoryItem.cs`
- `scripts/ItemPickup.cs`
- `scripts/GameManager.cs`
- `scripts/CombatManager.cs`
- `scripts/GameKeys.cs`
- `project.godot`

## Hotkeys

Input actions exist for:

- `item_slot_1` through `item_slot_7`

Current keyboard mapping is:

- `Z`, `X`, `C`, `V`, `B`, `N`, `M`

The item bar is built in code by `GameManager`, and the currently reachable slots are determined by inventory capacity, not by how many actions exist in the input map.

## Pickup Flow

`GameManager` asks `MapGenerator` for enemy spawn positions plus the room's cave chest anchor.

- the returned positions are used for enemies
- the returned chest anchor is used to place the room's `CaveChest`
- tougher enemies may also spawn an `ItemPickup` next to their normal modifier loot when they die

`ItemPickup` is auto-collect, not button-confirmed:

1. player walks into the pickup
2. pickup tries to add itself to the first open inventory slot
3. on success, the pickup disappears and `GameManager` updates status text
4. on failure, the pickup stays in the world and shows that inventory is full

If the player frees a slot while still standing inside the pickup, the pickup retries automatically.

## Current Item Types

Current consumables now include:

- `HealingTonic`
  - restores HP immediately
  - can be used in exploration
- `EmberBomb`
  - deals direct damage to the current combat target
  - can only be used when there is an active combat target on the player turn
  - consumes the turn
- `DeeprootFlask`
  - larger heal than `HealingTonic`
- `StarfireBomb`
  - larger direct-damage bomb than `EmberBomb`
- `SannosShield`
  - the next incoming hit on the player deals 0 damage
  - can be prepared in exploration and carried into combat
- `MarauderDraught`
  - the player's next attack gains flat bonus damage
- `GiantSeal`
  - the player's next attack gains a damage multiplier

The item bar shows:

- hotkey
- item name
- short description

## Combat Integration

`CombatManager` now exposes two simple item-facing entry points:

- `PlayerUseDamageItem(int damage)`
- `PlayerUseUtilityItem()`

These are intentionally small for now. They exist so item use can plug into the current combat loop without bypassing turn handoff.

Prepared items such as `SannosShield`, `MarauderDraught`, and `GiantSeal` are stored on `PlayerStats` as pending combat boons so they can persist across exploration and into the next fight.

## Important Design Intent

The current consumables are not the long-term target shape.

Primary intended direction:

- permanent items
- usable skills
- cooldown-based item actions

Consumables are still valid and useful, but future contributors should not hardcode assumptions like:

- all items are destroyed on use
- all item actions are immediate one-shot effects
- item slots will always stay at 2
- item logic belongs permanently inside `GameManager`

## Likely Future Evolution

When the item system grows, likely seams include:

- item definition versus item instance
- cooldown state on the item itself
- action resolution shared with combat abilities
- permanent loadout items versus consumables
- moving inventory ownership out of `PlayerStats` into a more focused run/loadout model

For now, keep the implementation simple, but preserve that direction when changing the system.
