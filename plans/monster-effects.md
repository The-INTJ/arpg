# Monster Effects

## Goal

Monster effects are an enemy-side combat system that can:

- leave some enemies clean
- roll one or more effects on others
- let rooms boost, guarantee, or bias effect assignment
- expose tags for future build-countering systems
- keep all of this separate from player modifiers

The current implementation includes Stage 3 content: five live effects, tiered behavior, room-by-room progression, stacked-roll decay, and a room-rule HUD readout.

## Runtime Model

### `MonsterEffectDefinition`

Static effect data:

- identity and display metadata
- badge text and badge color
- tags
- base roll weight
- resolution priority
- duplicate and incompatibility rules
- threat by tier
- per-tier description formatter
- lifecycle and combat hooks

Definitions live in `scripts/MonsterEffectDefinitions.cs`.

### `MonsterEffectInstance`

Per-enemy runtime state:

- owner
- chosen tier
- turn counters
- trigger count
- last trigger turn
- a small keyed state bag for effect-specific counters and pools
- expiration flag

### `MonsterEffectRollContext`

Spawn-time inputs:

- room number
- active room profile
- boss flag
- preferred tags
- blocked tags
- blocked effect ids

This is the future extension seam for adaptive map logic that wants to punish or bias against specific player build gaps.

### `RoomMonsterEffectProfile`

Room-scoped generation config:

- `NormalRules`
- `BossRules`
- guaranteed grant rules
- effect-id weight multipliers
- tag weight multipliers
- player-facing room name and description

Each scope uses `MonsterEffectSpawnRules`, which currently contains:

- optional effect chance
- decay chance for continuing into another optional effect
- optional-roll suppression after guaranteed grants
- threat budget
- max optional effects
- max total effects
- max allowed tier
- optional count weights
- tier weights
- boss-safe weighting multipliers

### `MonsterEffectAssignmentPlan`

Final spawn-time result for one enemy:

- ordered effect assignments
- source per assignment (`Innate`, `Granted`, `Optional`)
- total threat consumed

The `Enemy` converts the plan into live `MonsterEffectInstance`s and sorts them by resolution priority.

## Assignment Pipeline

1. Start with an empty assignment plan.
2. Apply guaranteed room grants first.
3. Respect threat budget and total-effect caps.
4. Roll the desired optional effect count.
5. Attempt each optional slot behind a decaying probability gate.
6. If guaranteed effects were already applied, further suppress the optional roll chance.
7. Roll a compatible definition by weight, including tag and boss-safe modifiers.
8. Roll a tier that fits the active room rules and remaining budget.
9. Instantiate effects on the enemy in priority order.

This supports:

- enemies with zero effects
- occasional stacked enemies
- room-wide guaranteed effects
- boss-only grants
- random subsets
- tag-biased future adaptive generation

## Combat Hooks

Enemy-owned hooks currently exist for:

- combat start
- owner turn start
- owner turn end
- incoming damage resolution
- outgoing damage resolution

`CombatManager` resolves damage through these hooks before HP changes are finalized, and effect feedback is pushed back into the combat HUD.

## Live Effects

### `Invulnerable`

- badge: `INV`
- tags: `Defense`, `Opener`, `PunishesBurst`, `BossSafe`
- priority: 10
- tier 0: negate the first damaging hit
- tier 1: negate damage during the first two enemy turns
- tier 2: negate damage on alternating enemy turns

### `Bulwark`

- badge: `BLW`
- tags: `Defense`, `Attrition`, `BossSafe`
- priority: 20
- tier 0: 5 block through the first turn
- tier 1: 8 block for each of the first two turns
- tier 2: 12 block for each of the first three turns

### `Thorns`

- badge: `THN`
- tags: `Retaliation`, `PunishesBurst`
- priority: 100
- tier 0: 1 retaliation damage
- tier 1: 3 retaliation damage
- tier 2: 5 retaliation damage

### `Enraged`

- badge: `ENR`
- tags: `Opener`, `PunishesSustain`
- priority: 60
- tier 0: +2 damage on the first enemy attack
- tier 1: +4 damage on the first two enemy attacks
- tier 2: +6 damage on every enemy attack

### `Leech`

- badge: `LEC`
- tags: `Attrition`, `PunishesSustain`, `BossSafe`
- priority: 120
- tier 0: heal 2 after a successful hit
- tier 1: heal 5 after a successful hit
- tier 2: heal 8 after a successful hit

## Room Progression

### Room 1 - `Wild Den`

- 50% optional effect chance
- tier 0 only
- occasional second effect, but still plenty of clean enemies

### Room 2 - `Guarded Hall`

- higher optional effect chance
- tiers 0-1
- `Defense` effects are weighted up
- a random subset of enemies is guaranteed `Bulwark` tier 1

### Room 3 - `Pressure Chamber`

- high optional effect pressure
- tiers 1-2
- normals can still roll zero, one, or two effects
- the boss is guaranteed `Bulwark` tier 2 before optional rolls

## Presentation

- active effects render as `Label3D` badges above each enemy
- badges include tier suffixes when the tier is above 0
- badges flash when triggered and fade on expiry
- the enemy HP display includes plain-language effect descriptions so players can read what each badge means in-run
- the HUD includes a dedicated room-rule label using the room profile's name and description

## Supporting Balance Tweaks

- room-1 baseline monster effect chance is now 50%
- enemy baseline HP and damage were bumped slightly
- room scaling for enemy HP and damage was increased
- player movement speed was nudged up overall
- movement now ramps up to full speed in 0.30s and ramps down in 0.15s

## Future Expansion Path

Good next steps after playtest feedback:

- innate per-archetype monster effects
- richer forbidden-pair rules
- more boss-only weighting and tag policies
- true hover or inspect affordances for effect badges
- adaptive room generation that supplies preferred and blocked tags through `MonsterEffectRollContext`
