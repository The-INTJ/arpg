# Modifier System, Weapons & Item Hotbar

## Overview

Replace the current "auto-apply modifier on walk-over" loot flow with an intentional modifier system. Loot drops become **items** (modifiers or active items) that go into a **backpack**. The player decides when and where to apply stat modifiers via a dedicated screen. Active items provide abilities on a hotbar.

---

## Phase 1: Loot Interaction (E to Equip / Q to Stash)

### Current behavior
- Enemy dies -> LootPickup spawns -> player walks over it -> modifier auto-applies to PlayerStats.

### New behavior
- Enemy dies -> LootPickup spawns with a modifier (unchanged).
- Player walks near the loot (within interaction range, not auto-pickup).
- A prompt appears: `(E) Equip  |  (Q) Backpack`
- **(E)** opens the ModifyStatsSimple scene as an overlay (blur + pause).
- **(Q)** puts the modifier into the player's backpack list, loot orb disappears.

### Changes needed

1. **LootPickup.cs** — Stop auto-applying on `BodyEntered`. Instead:
   - Track when the player is in range (enter/exit signals).
   - Show the E/Q prompt label when in range.
   - On E press: signal GameManager to open ModifyStatsSimple, passing the modifier.
   - On Q press: add to backpack, emit signal, QueueFree.

2. **Backpack** — Add a `List<Modifier>` to `PlayerStats` (or a new `Inventory` class on `PlayerController`). Simple list for now; capacity limits later.
   ```
   PlayerStats.Backpack : List<Modifier>
   PlayerStats.AddToBackpack(Modifier mod)
   PlayerStats.RemoveFromBackpack(Modifier mod)
   ```

3. **GameManager.cs** — Handle the "open modify screen" signal. Instantiate/show the ModifyStatsSimple overlay.

---

## Phase 2: Weapons with Two Modifier Slots

### Concept
Each archetype starts with a **weapon** that has two modifier slots. The weapon defines the archetype's attack style:
- Fighter: Sword (melee, Cleave ability)
- Archer: Bow (ranged, Snipe ability)
- Mage: Staff (ranged, Fireball ability — the "spell" is cast through the staff)

The weapon's ability is tied to it, not to the archetype directly. This means a future weapon swap could change your ability.

### Data model

```csharp
public class Weapon
{
    public string Name { get; }
    public AbilityType Ability { get; }
    public Modifier[] Slots { get; } = new Modifier[2]; // exactly 2 slots

    public Weapon(string name, AbilityType ability)
    {
        Name = name;
        Ability = ability;
    }
}
```

### Archetype defaults
| Archetype | Weapon | Ability | Slot 1 (default) | Slot 2 (default) |
|-----------|--------|---------|-------------------|-------------------|
| Fighter | Iron Sword | Cleave | +2 ATK | +3 HP |
| Archer | Longbow | Snipe | +0.5 Range | +1 ATK |
| Mage | Oak Staff | Fireball | +1 ATK | +5% ATK |

### Changes needed

1. **Weapon.cs** — New file with the data model above.
2. **PlayerController** — Add `public Weapon Weapon { get; private set; }`. Set in `_Ready()` based on archetype.
3. **Ability system** — Ability is now sourced from the weapon, not the archetype. `Ability.ForWeapon(weapon)` instead of `Ability.ForArchetype(archetype)`. Keep the archetype-based factory as a fallback/convenience.
4. **PlayerStats** — Weapon slot modifiers are included in the modifier stack (computed alongside backpack modifiers). The weapon's slots are "equipped modifiers" — they participate in `ComputeStat` just like backpack-applied modifiers.

---

## Phase 3: ModifyStatsSimple Screen (The Core UI)

### Layout

```
+--------------------------------------------------+
|              [blurred game behind]                |
|                                                   |
|  +--- WEAPON: Iron Sword ---+    +--- YOU ---+   |
|  |  [Slot 1: +2 ATK      ] |    | HP:  20   |   |
|  |  [Slot 2: +3 HP       ] |    | ATK: 7    |   |
|  +---------------------------+    | SPD: 4.5  |   |
|                                   | Range: 3  |   |
|  +--- BACKPACK ---+              +-----------+   |
|  |  +5% ATK       |                              |
|  |  +1 SPD        |                              |
|  |  +3 HP         |                              |
|  +-----------------+                              |
|                                                   |
|  Drag a modifier from BACKPACK onto a weapon slot |
+--------------------------------------------------+
```

### Behavior

1. **Open trigger**: pressing (E) on a loot pickup, or "View Stats" in pause menu.
   - If opened from loot: the new modifier is already in the backpack (auto-stashed before opening).
   - If opened from pause: just shows current state for rearranging.

2. **Blur effect**: Apply a `BackBufferCopy` + shader blur on a `ColorRect` behind the UI, or use a `SubViewport` approach. Simpler alternative: just use a dark semi-transparent overlay (like the pause screen) with 0.7 alpha — revisit blur later if it matters.

3. **WEAPON section**: Shows weapon name as a heading, with two "slot" panels beneath. Each slot shows its current modifier text (or "Empty" if unoccupied).

4. **YOU section**: Shows computed effective stats — reads from `PlayerStats.MaxHp`, `.AttackDamage`, `.MoveSpeed`, `.AttackRange`. Updates live as modifiers change.

5. **BACKPACK section**: Lists all modifiers in the backpack. Each is a draggable panel.

6. **Drag-and-drop flow**:
   - Player drags a backpack modifier onto a weapon slot.
   - A confirmation popup appears showing:
     ```
     Before: +2 ATK    ->    After: +5% ATK
     ATK: 7            ->    ATK: 8
     (E) Confirm  |  (Q) Cancel
     ```
   - The old modifier (if any) returns to the backpack.
   - Stats update immediately on confirm.

7. **Close**: Press Escape to close and resume game.

### Implementation plan

1. **ModifyStatsSimple.cs** — Script attached to the existing scene. Builds all UI in `_Ready()`:
   - Dark overlay ColorRect
   - Weapon panel (VBox with heading + 2 slot Controls)
   - You panel (VBox with stat labels)
   - Backpack panel (VBox/ScrollContainer with modifier items)
   - Confirmation popup (hidden by default)

2. **Drag-and-drop**: Use Godot's built-in drag/drop on Controls:
   - Backpack items implement `_GetDragData()` returning the Modifier.
   - Weapon slots implement `_CanDropData()` and `_DropData()`.
   - On drop: show confirmation, apply on E, revert on Q.

3. **Integration with GameManager**:
   - GameManager instantiates and holds a reference to ModifyStatsSimple.
   - `OpenModifyScreen(Modifier newItem = null)` — if newItem is provided, add to backpack first.
   - Screen pauses the tree when shown, unpauses when closed.

---

## Phase 4: Item Hotbar (Active Items)

### Concept

Not all loot is stat modifiers. **Active items** are one-use or multi-use items that provide activated abilities (e.g., heal potion, throwable bomb, shield charge). They do NOT affect base stats — they sit on a **hotbar** and are activated during combat.

### Data model

```csharp
public enum ItemType
{
    HealPotion,     // restore HP
    ThrowingKnife,  // bonus damage to target
    ShieldCharge,   // block next attack
    SpeedScroll,    // temporary speed boost
    // ...more as needed
}

public class ActiveItem
{
    public ItemType Type { get; }
    public string Name { get; }
    public string Description { get; }
    public int Uses { get; set; }        // -1 = infinite
    public int MaxUses { get; }
    public bool IsConsumable => MaxUses > 0;
}
```

### Hotbar rules
- **Hotbar size**: starts at 2 slots, could be increased by modifiers/upgrades.
- **Active vs held**: Only items on the hotbar can be used. Others sit in the backpack doing nothing.
- **No swapping in combat**: Cannot move items between backpack and hotbar during combat (or it costs a turn — TBD).
- **Keybinds**: Number keys 1-4 (or configurable) for hotbar slots.

### UI layout (in-game HUD)
```
[1: Heal Potion x3]  [2: Shield Charge x1]  [3: ---]  [4: ---]
```
Shown at the bottom of the screen during gameplay and combat.

### UI layout (in ModifyStatsSimple screen)
Add a HOTBAR section:
```
+--- HOTBAR ---+
| [1] Heal Pot |   <- drag items here from backpack
| [2] (empty)  |
+--------------+
```

### Implementation plan

1. **ActiveItem.cs** — New file with data model.
2. **ItemGenerator.cs** — Random active item creation (like ModifierGenerator for modifiers).
3. **Inventory expansion** — `PlayerStats` or separate `Inventory` holds both `List<Modifier>` and `List<ActiveItem>` for the backpack, plus `ActiveItem[]` for the hotbar.
4. **Loot drops** — ModifierGenerator sometimes returns an ActiveItem instead of a Modifier. Or have separate drop logic.
5. **Hotbar HUD** — New UI element in GameManager's CanvasLayer showing hotbar slots.
6. **Combat integration** — Using a hotbar item is an action during PlayerTurn (like attack or ability). TurnManager treats it as a player action.
7. **ModifyStatsSimple** — Add hotbar section for managing which items are active.

---

## Implementation Order

| Step | Description | Depends on |
|------|-------------|------------|
| 1 | Backpack data structure on PlayerStats | — |
| 2 | Weapon data model + archetype defaults | — |
| 3 | Refactor LootPickup to E/Q interaction | Step 1 |
| 4 | ModifyStatsSimple: basic layout (weapon slots, you panel, backpack list) | Steps 1, 2 |
| 5 | ModifyStatsSimple: drag-and-drop with confirmation | Step 4 |
| 6 | Wire weapon modifiers into PlayerStats.ComputeStat | Step 2 |
| 7 | Pause menu "View Stats" button opens ModifyStatsSimple | Step 4 |
| 8 | ActiveItem data model | — |
| 9 | Item hotbar HUD | Step 8 |
| 10 | Combat integration for active items | Steps 8, 9 |
| 11 | ModifyStatsSimple: hotbar management section | Steps 5, 9 |

Steps 1-7 are the immediate priority (the modifier system). Steps 8-11 (item hotbar) come after the modifier flow is solid.
