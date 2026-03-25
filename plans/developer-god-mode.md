# Developer God Mode Follow-Ons

This slice ships the scene-owned developer runtime, god-flight movement, and the first wave of exploration effect toggles. The same panel/model should be extended for the next round of combat-facing tools instead of creating parallel debug paths.

## Current V1 Surface

- Movement controls live in the god-mode panel and are session-local.
- The effect registry is owner-scoped, grouped, and ready for future regrouping.
- Explore-only effect coverage currently includes:
  - encounter watch / commit
  - zone bounds guard
  - void fall penalty
  - active zone detection
  - explore regen
  - bridge energy requirement
  - camera collision

## Next Combat/Debug Actions

- Enemy monster effect editing:
  - replace an enemy's monster effect plan mid-fight
  - add/remove one effect instance without rebuilding the whole encounter
  - refresh badges and effect summaries immediately after mutation
- Direct combat injections:
  - deal damage to player
  - deal damage to target enemy
  - heal player or target enemy
  - set HP directly for scripted repro cases
- Turn/debug flow:
  - force player turn
  - force enemy retaliation
  - skip retaliation once
  - tick or clear ability cooldowns
- Progression shortcuts:
  - add dark energy directly
  - force bridge readiness
  - jump active room label/state to the room containing the player

## UI / Model Notes

- Keep using `DeveloperEffectKind` for the panel model.
- When combat actions land, prefer `Action` rows first and only add `Value` rows where a numeric editor is genuinely useful.
- Add live regrouping only after the fixed-group workflow feels solid; the registry already stores `CurrentGroupId` separately so the UI can grow into that without a data rewrite.
