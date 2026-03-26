# Vertical Slice Plan

> **Goal:** Prove the game's identity with the smallest playable build. Read `direction.md` first.

## Success Criteria

A player experiences this slice and thinks: **"I want to keep exploring this world, and my build changes how I move and fight in it."**

## What The Slice Contains

1. **One fractured island region** with multiple traversal paths, at least one hidden area, and visible routes the player can't yet reach
2. **Satisfying real-time movement** — responsive, weighty, with at least one traversal mechanic beyond basic jump (dash or ledge grab)
3. **Two dangerous real-time enemies** — one common, one elite/mini-boss. Both with chase AI and attack patterns
4. **Energy nodes and one hidden chest** — exploration rewards visible and findable through curiosity
5. **Two abilities** — basic attack + one special, both with real-time cooldowns and hitboxes
6. **Three modifiers** — at least one that visibly changes attack feel (range, size, speed) and one that changes movement
7. **A simple hub** — safe zone, one NPC slot, one visible world-change when a fragment is returned
8. **The return loop** — collect fragment in the island → return to hub → see visible change → go out again

## Milestone Breakdown

### Milestone 1: Movement Feel

**Goal:** Moving through the world feels good on its own.

- [ ] Polish PlayerController: tune acceleration, jump arc, air control
- [ ] Add dash/dodge: short burst, directional, brief invincibility
- [ ] Build one test island chunk: multiple elevation levels, at least one gap requiring dash or careful jump, one underside area
- [ ] Void recovery works cleanly with island geometry
- [ ] Camera follows smoothly across vertical transitions

**Playtest check:** Is just running and jumping around this island enjoyable for 60 seconds?

### Milestone 2: Real-Time Combat

**Goal:** Fighting one enemy feels responsive and dangerous.

- [ ] Implement hitbox/hurtbox system: Area3D nodes, overlap signals
- [ ] Player basic attack: animation with wind-up → active hitbox → recovery
- [ ] Player takes damage from enemy contact/attack hitboxes
- [ ] One enemy type with AI: Idle → Alert → Chase → Attack → Stagger states
- [ ] Health bar on player and enemy
- [ ] Damage numbers (reuse existing DamageNumber.tscn)
- [ ] Death: enemy drops loot, player dies → restart
- [ ] Health does not auto-regenerate

**Playtest check:** Is fighting this one enemy tense? Does positioning matter? Does the player want to dodge?

### Milestone 3: Elite + Traversal Rewards

**Goal:** Exploration pays off and a harder enemy tests mastery.

- [ ] Elite enemy variant: more HP, different attack pattern, telegraphed strong attack
- [ ] Energy node pickups: placed in the island, some on main paths, some requiring exploration
- [ ] Hidden chest in a non-obvious location rewarding a modifier
- [ ] One potion pickup for health management
- [ ] Energy counter in HUD
- [ ] Elite drops a valuable reward (modifier or fragment piece)

**Playtest check:** Does the player explore the island before fighting the elite? Do they find the hidden chest? Does the elite feel like a real threat?

### Milestone 4: Modifier Expression

**Goal:** Modifiers change how combat and movement feel.

- [ ] One ability beyond basic attack (e.g., charged slam, projectile, dash-strike)
- [ ] Ability cooldown in real-time (seconds, not turns)
- [ ] Three modifier drops that affect: attack range/size, movement speed, ability cooldown or damage
- [ ] Modifier assignment screen (reuse ModifyStatsSimple, adapt for new stat targets)
- [ ] After applying a modifier, the player can feel the difference immediately

**Playtest check:** After picking up a +range modifier and applying it, does the player's next attack visibly reach further? Does a speed modifier make traversal noticeably different?

### Milestone 5: Hub + Restoration Loop

**Goal:** The return-to-hub loop feels rewarding.

- [ ] Hub scene: small safe area, distinct from excursion
- [ ] One NPC present in hub (or discovered during excursion and then appears in hub)
- [ ] Fragment objective in the island: a glowing object to collect
- [ ] Returning fragment to hub triggers visible change (a broken bridge repairs, new area opens, visual restoration)
- [ ] Player can re-enter the island from hub
- [ ] Persistent state: fragment stays returned across runs

**Playtest check:** Does returning the fragment feel like an accomplishment? Does the player want to go back out and find more?

## What To Defer

These are important for the full game but not needed for the vertical slice:

- Multiple island regions / procedural arrangement
- Full NPC dialogue system
- Save/load persistence
- Multiple archetypes diverging meaningfully
- Multiplayer
- Sound design / music
- Full modifier data-driven pipeline (hardcoded is fine for 3 modifiers)
- Boss encounters (the elite serves as the skill check)
- World restoration at scale
- Item/consumable system rework
- Modifier pool unlocking meta-progression

## Risk Flags

| Risk | Mitigation |
|------|-----------|
| Real-time combat hitbox tuning takes too long | Start with generous hitboxes, tighten later. Overshoot on player power first. |
| Island chunk feels too small or empty | Lean into verticality. A small footprint with multiple layers feels bigger than a flat expanse. |
| Modifier expression is too subtle | Pick dramatic stat changes for the slice (2x range, +50% speed). Subtlety comes later. |
| Hub feels pointless with one NPC | Make the world-change dramatic and visible. One big visual shift > three subtle ones. |
| Movement doesn't feel good | This is the highest priority. If movement isn't fun by milestone 1, stop and fix it before proceeding. |

## Scope Control Rule

If any milestone takes more than twice the expected effort, stop and assess whether the approach is wrong rather than pushing through. The slice exists to prove direction, not to be polished content.
