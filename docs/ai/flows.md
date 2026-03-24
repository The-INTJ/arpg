# Existing Flows

## Boot And New Run

1. `MainMenu` loads `ArchetypeSelect`.
2. `ArchetypeSelect` shows buttons for `Fighter`, `Archer`, and `Mage`.
3. Selecting an archetype calls `GameState.StartNewRun(...)`.
4. The game loads `scenes/Game.tscn`.
5. `PlayerController` creates fresh `PlayerStats` only if `GameState.PersistentStats` is null. Otherwise it reuses the persistent run state.

## Game Scene Startup

`GameManager._Ready()` currently does all of this:

1. Resolve key scene nodes such as player, exit door, camera, HUD labels, pause screen, and modify-stats overlay.
2. Create and add `TurnManager`.
3. Create and add `CombatManager`, then initialize it with player, turn manager, and camera.
4. Build runtime HUD pieces such as ability button, enemy HP display, room label, and item bar.
5. Apply some world styling such as the floor material.
6. Ask `MapGenerator` for room geometry and spawn/chest anchors. The builder can now mix procedural geometry with reusable scene slices such as the cave pocket.
7. Spawn room enemies into `World/Enemies`.
8. Spawn the room's cave chest at the returned chest anchor.
9. Wire pause-to-stats interaction.
10. Populate HUD text.

## Exploration Flow

1. Player movement runs in `PlayerController._PhysicsProcess`.
2. `GameManager._Process()` updates the HUD and item bar every frame.
3. While `TurnManager.IsExploring`:
   - player regen ticks
   - nearby enemies are scanned for aggro
   - the attack button is shown near the nearest in-range enemy
4. Aggro is delayed:
   - enemy enters sight range
   - enemy shows an aggro indicator
   - a short timer counts down
   - combat begins if the enemy is still valid

## Combat Flow

Current combat is still a one-player versus one-enemy exchange:

1. Combat starts either from aggro or pressing attack near an enemy in exploration.
2. `CombatManager.EnterCombat(...)`:
   - stores the target
   - disables player movement
   - zooms the camera
   - sets state to player turn after a short tween
3. On the player turn:
   - `E` attacks
   - `Q` uses the weapon-linked ability if ready
   - item hotkeys use the occupied inventory slot if the item can be used now
4. `CombatManager` applies damage and spawns a floating damage number.
5. If the enemy survives, a short timer triggers retaliation.
6. Enemy retaliation damages the player and returns control to player turn.
7. If the enemy dies:
   - `CombatManager` stores the death position
   - enemy queues free
   - camera exits combat
   - `CombatEnded` signal fires back to `GameManager`

## Item Flow

Current item flow is the MVP version of a larger intended item system:

1. Each room spawns one `CaveChest`, which releases an `ItemPickup` when opened.
2. Walking into the pickup tries to auto-store the item in the first open inventory slot.
3. If inventory is full, the pickup stays in the world and shows an inventory-full prompt.
4. Hotkeys are reserved from `Z` through `M` via `GameKeys`, but the player inventory currently starts with capacity 2, so only `Z` and `X` are active by default.
5. Current item kinds:
   - `HealingTonic`: heals immediately; if used on the player turn in combat, it consumes the turn and still hands off to retaliation
   - `EmberBomb`: combat-only direct damage item; consumes the turn

## Kill, Loot, And Progression Flow

When combat ends because the enemy died:

1. `GameManager.OnCombatEnded()` increments kill count.
2. A `LootPickup` is spawned at the kill position.
3. If all enemies are dead, the exit door unlocks.
4. Status text updates to either room-cleared or enemies-remaining messaging.

Loot interaction flow:

1. Player walks into the loot orb range.
2. The orb shows an `E`/`Q` prompt.
3. `E`:
   - adds the modifier to backpack
   - emits `EquipRequested`
   - opens `ModifyStatsSimple`
4. `Q`:
   - adds the modifier to backpack
   - emits `Stashed`
   - closes the pickup

## Modify Stats Flow

1. `ModifyStatsSimple.Open(...)` receives `PlayerStats`.
2. The tree is paused while the overlay is open.
3. The screen shows:
   - weapon name
   - two weapon slots
   - current effective player stats
   - backpack list
4. Clicking a backpack button starts a pending slot swap.
5. Confirmation preview compares before and after values.
6. Confirm swaps the modifier into the chosen weapon slot and returns the old one to backpack.
7. Closing the screen unpauses the tree.

## Pause Flow

1. `PauseScreen` listens for the pause input action.
2. Showing pause sets `GetTree().Paused = true`.
3. Resume unpauses.
4. View Stats hides pause but leaves the game paused, then asks `GameManager` to open `ModifyStatsSimple`.
5. Quit to Menu unpauses and loads `MainMenu`.

## Room, Victory, And Defeat Flow

1. Clearing a room unlocks `ExitDoor`.
2. Walking into the door trigger:
   - advances `GameState.CurrentRoom` and reloads `Game.tscn`, or
   - loads `VictoryScreen` if the final room was cleared
3. If the player dies, `TurnManager` enters `Defeat`.
4. `GameManager` handles that state change, clears combat UI, and loads `GameOverScreen`.
5. `VictoryScreen` and `GameOverScreen` both reset the run before starting again.

## Current Flow Constraints

These flows all still assume:

- one local player
- one active combat target at a time
- room progression through reloading the same gameplay scene
- persistent player build state stored globally in `GameState`
- items are currently simple consumables even though the longer-term design intent is permanent cooldown-based item skills
