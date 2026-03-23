# Next Feature Slices

Small, self-contained additions roughly ordered by build-on-each-other dependency.

---

## 1. Enemy Sight / Encounter Trigger
Enemies detect the player at a distance and "aggro" — a visual indicator (exclamation mark, glow pulse) shows they've spotted you. Walking into their sight range auto-initiates combat instead of requiring you to press E first. This lays groundwork for stealth and positioning mattering.
- **No new scenes needed.**

## 2. Archetype Selection Screen
Pre-game screen: pick Fighter / Archer / Mage. Each sets different base values on `PlayerStats`. First consumer of the stats architecture.
- **New scene needed:** `scenes/ArchetypeSelect.tscn`

## 3. Abilities (One Per Archetype)
Each archetype gets one unique ability beyond basic attack. Fighter: Cleave (hit all in range). Archer: Snipe (double range). Mage: Fireball (AoE). Bound to Q.
- **No new scenes needed.**

## 4. Modifier System (Core)
Implement `+N`, `+N%`, `×M`, `−N%` modifier operators from the vision doc. Modifiers attach to `PlayerStats` and stack.
- **No new scenes needed.**

## 5. Loot Drops
Enemies drop a modifier pickup on death (glowing orb). Walking over it adds a random modifier to stats.
- **New scene needed:** `scenes/LootPickup.tscn`

## 6. Multiple Rooms / Floor Progression
After clearing + exiting, generate a new room instead of VictoryScreen. Track floor number. Difficulty scales.
- **No new scenes needed** (reuses Game.tscn).

## 7. Enemy Variety
2-3 enemy types with different stats/behaviors: melee brute (high HP, low damage), ranged (attacks from farther), fast (lower HP but hits harder). Different sprite colors/shapes.
- **No new scenes needed.**

## 8. Minimap
Corner overlay showing room layout, player dot, enemy dots. Orthographic SubViewport camera.
- **No new scenes needed.**

## 9. Sound Effects
Attack hit, enemy death, door unlock, combat enter/exit, footsteps.
- **No new scenes needed**, but need audio files in `audio/`.

## 10. Death / Game Over Screen
Proper defeat screen with stats summary and restart option.
- **New scene needed:** `scenes/GameOver.tscn`

---

## Scene Creation Summary

| Slice | Needs New Scene? |
|-------|-----------------|
| 1     | No              |
| 2     | Yes — `ArchetypeSelect.tscn` |
| 3–4   | No              |
| 5     | Yes — `LootPickup.tscn` |
| 6–9   | No              |
| 10    | Yes — `GameOver.tscn` |
