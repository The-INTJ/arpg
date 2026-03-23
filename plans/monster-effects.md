# Monster Effects

## Goal

Add a dedicated enemy-side effect system that can:

- roll effects on some enemies and leave others clean
- support room-driven guarantees and weighting later
- make extra effects progressively less likely as one enemy stacks them
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

- a `NormalRules` block
- a `BossRules` block
- guaranteed grant rules
- effect-id weight multipliers
- tag weight multipliers
- per-scope threat budget
- per-scope optional/max total effect caps
- per-scope optional-effect chance
- per-scope additional-roll decay
- per-scope "guaranteed effect reduces optional chance" multiplier
- per-scope boss-safe weighting knobs

Each scope uses `MonsterEffectSpawnRules` so the normal/boss split is explicit instead of being flattened into one large constructor.

Room profiles now vary by room:

- room 1 is intentionally tame
- room 2 increases overall effect pressure
- room 3 increases both pressure and boss weighting toward `BossSafe` effects

All current rooms still cap at tier `0`.

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
4. Roll the desired optional-effect count from the active spawn rules.
5. Attempt each optional effect behind a decaying probability gate.
6. If guaranteed effects were already applied, reduce that optional chance further before the first optional roll.
7. Roll a compatible effect by weight.
8. Roll a valid tier that still fits the budget.
9. Create runtime instances on the enemy in priority order.

The pipeline already supports:

- enemies with zero effects
- enemies with rare stacked effects
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
- priority: resolves before retaliation-style effects
- behavior: negates the first damaging hit in combat, then expires

### `Bulwark`

- badge: `BLW`
- tags: `Defense`, `Attrition`, `BossSafe`
- priority: resolves after stronger negation, before retaliation
- behavior: reduces incoming damage by `1` until the enemy finishes its first turn, then expires

### `Thorns`

- badge: `THN`
- tags: `Retaliation`, `PunishesBurst`
- priority: resolves after defensive mitigation
- behavior: deals `1` retaliation damage back to the player whenever the hit still deals damage

## Presentation

- active effects render as `Label3D` badges above the enemy's head
- badges are spaced so they do not conflict with the aggro indicator or boss label
- triggered effects flash
- expiring effects fade out
- combat feedback text is sent back through `CombatManager` so the HUD status line can explain what happened
- the enemy HP display now includes plain-language effect descriptions for the current combat target or nearby attackable enemy

## Part 2 Refinements

- `WeightedIntOption` is now a lightweight `record struct`
- trigger logging is shared through a common resolution-context base class instead of duplicated in incoming/outgoing contexts
- badge lookup is instance-based, so future duplicate effects can render safely
- dead legacy enemy attack code was removed so all outgoing damage continues through the effect system
- bosses now actually use the `BossSafe` tag as a weighting hint instead of carrying it as dead metadata

## Next Expansion Path

Part 3 can extend this system by:

- adding more definitions and tags
- expanding `ThreatByTier`
- turning on real tier weights per room and per boss
- enabling guaranteed room grants
- raising max total effects on selected enemies or bosses
- adding room-rule HUD text sourced from `RoomMonsterEffectProfile`
