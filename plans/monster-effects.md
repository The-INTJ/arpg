# Monster Effects

## Goal

Add a dedicated enemy-side effect system that can:

- roll effects on some enemies and leave others clean
- support room-driven guarantees and weighting later
- attach tags for future "punish this build gap" systems
- keep combat logic separate from the player modifier stack

This Part 1 implementation ships the foundation plus three live effects:

- `Invulnerable`
- `Bulwark`
- `Thorns`

## Runtime Model

### `MonsterEffectDefinition`

Static data for an effect:

- `Id`
- `Name`
- `BadgeText`
- `BadgeColor`
- `BaseWeight`
- `Tags`
- `ThreatByTier`
- duplicate policy
- incompatible effect ids
- lifecycle and combat hooks

Definitions live in `scripts/MonsterEffectDefinitions.cs`.

### `MonsterEffectInstance`

Per-enemy runtime state:

- owning enemy
- chosen tier
- turns started / ended
- trigger count
- last trigger turn
- small key/value state dictionary for future custom behavior
- expiration flag

Instances are created from a spawn-time assignment plan and then live on the `Enemy`.

### `MonsterEffectRollContext`

Spawn-time inputs:

- room number
- room profile
- boss flag
- preferred tags
- blocked tags
- blocked effect ids

This is the seam future dynamic systems should use when they want to bias monster effects without bypassing the generator.

### `RoomMonsterEffectProfile`

Room-scoped generation rules:

- base optional-effect chance
- optional effect count weights
- tier weights
- guaranteed grant rules
- effect-id weight multipliers
- tag weight multipliers
- threat budget
- optional/max total effect caps

Part 1 uses a single default profile for all rooms:

- `30%` chance to roll optional effects
- only one optional effect
- only tier `0`
- no guaranteed grants enabled yet

### `MonsterEffectAssignmentPlan`

Final spawn-time result for one enemy:

- ordered list of assigned effects
- source per effect (`Granted` or `Optional`)
- total threat consumed

The `Enemy` converts this into live `MonsterEffectInstance`s.

## Assignment Pipeline

1. Start with an empty `MonsterEffectAssignmentPlan`.
2. Apply guaranteed room grants first.
3. Respect threat budget and max-total-effect limits.
4. Roll whether optional effects happen from the room profile.
5. Roll a compatible effect by weight.
6. Roll a valid tier that still fits the budget.
7. Create runtime instances on the enemy.

The pipeline already supports:

- enemies with zero effects
- future guaranteed effects on every monster
- future boss-only effects
- future random subsets
- future tag-biased rolling

## Combat Hooks

Enemy-owned hooks currently exist for:

- combat start
- owner turn start
- owner turn end
- incoming damage resolution
- outgoing damage resolution

`CombatManager` now asks the enemy to resolve combat through its effects before HP is finalized.

## Part 1 Live Effects

### `Invulnerable`

- badge: `INV`
- tags: `Defense`, `Opener`, `PunishesBurst`, `BossSafe`
- behavior: negates the first damaging hit in combat, then expires

### `Bulwark`

- badge: `BLW`
- tags: `Defense`, `Attrition`, `BossSafe`
- behavior: reduces incoming damage by `1` until the enemy finishes its first turn, then expires

### `Thorns`

- badge: `THN`
- tags: `Retaliation`, `PunishesBurst`
- behavior: deals `1` retaliation damage back to the player whenever the enemy is hit

## Presentation

- active effects render as `Label3D` badges above the enemy's head
- badges are spaced so they do not conflict with the aggro indicator or boss label
- triggered effects flash
- expiring effects fade out
- combat feedback text is sent back through `CombatManager` so the HUD status line can explain what happened

## Next Expansion Path

Part 3 can extend this system by:

- adding more definitions and tags
- expanding `ThreatByTier`
- turning on real tier weights per room
- enabling guaranteed room grants
- raising max total effects on selected enemies or bosses
- adding room-rule HUD text sourced from `RoomMonsterEffectProfile`
