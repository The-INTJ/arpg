# Next Feature Slices

## Completed

- **Enemy Sight / Aggro** — Enemies spot player at 7-unit range, show "!" indicator, auto-initiate combat after 0.6s delay
- **Archetype Selection** — Pre-game screen: Fighter (tanky), Archer (fast/ranged), Mage (glass cannon). Sets base stats.
- **Abilities** — One per archetype on Q key. Cleave (2x damage), Snipe (3x), Fireball (2.5x). Turn-based cooldowns.
- **Modifier System** — `+N`, `+N%`, `×M`, `−N%` operators stack on PlayerStats. Computed on the fly with proper ordering.
- **Loot Drops** — Enemy death spawns glowing orb with random modifier. Bobbing animation, pickup on walk-over.

---

## Up Next

### 6. Multiple Rooms / Floor Progression
After clearing + exiting, generate a new room instead of VictoryScreen. Track floor number. Difficulty scales (more enemies, tougher stats).
- **No new scenes needed** (reuses Game.tscn).

### 7. Enemy Variety
2-3 enemy types with different stats/behaviors and sprite colors: melee brute (high HP), ranged (farther sight), fast (lower HP, higher damage).
- **No new scenes needed.**

### 8. Death / Game Over Screen
Proper defeat screen with run stats (floor reached, enemies killed, modifiers collected) and restart.
- **New scene needed:** `scenes/GameOver.tscn`

### 9. Minimap
Corner overlay showing room layout, player dot, enemy dots. Orthographic SubViewport camera.
- **No new scenes needed.**

### 10. Sound Effects
Attack hit, enemy death, door unlock, combat enter/exit, loot pickup, footsteps.
- **No new scenes needed**, but need audio files in `audio/`.

### 11. Status Effects / Buff Display
Show active modifiers as icons or a list in the HUD. Temporary buffs (e.g., from abilities) vs permanent modifiers.
- **No new scenes needed.**

### 12. Story Encounters
Occasional non-combat events between rooms: NPC dialogue, treasure chests, traps. Simple branching choices.
- **New scene needed:** `scenes/Encounter.tscn`
