using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds a player's base stats + modifier stack. Effective stats are computed on the fly.
/// Archetype sets the base values; weapon slot modifiers and general modifiers layer on top.
/// </summary>
public partial class PlayerStats
{
    // Base values — set by archetype
    private int _baseMaxHp = 15;
    private int _baseAttackDamage = 4;
    private float _baseMoveSpeed = 5.0f;
    private float _baseAttackRange = 3.5f;

    public float SprintMultiplier { get; set; } = 2.0f;
    public float HpRegenRate { get; set; } = 0.2f; // 1 HP per 5 seconds

    private readonly List<Modifier> _modifiers = new();
    private readonly List<Modifier> _backpack = new();

    /// <summary>The player's equipped weapon (provides 2 modifier slots + ability).</summary>
    public Weapon Weapon { get; set; }
    public PlayerInventory Inventory { get; } = new(2);

    // Effective stats (base + weapon slots + modifiers)
    public int MaxHp => (int)ComputeStat(StatTarget.MaxHp, _baseMaxHp);
    public int AttackDamage => (int)ComputeStat(StatTarget.AttackDamage, _baseAttackDamage);
    public float MoveSpeed => ComputeStat(StatTarget.MoveSpeed, _baseMoveSpeed);
    public float AttackRange => ComputeStat(StatTarget.AttackRange, _baseAttackRange);

    // Current mutable state
    public int CurrentHp { get; set; }
    public bool IsAlive => CurrentHp > 0;

    public IReadOnlyList<Modifier> Modifiers => _modifiers;
    public IReadOnlyList<Modifier> Backpack => _backpack;

    public PlayerStats()
    {
        CurrentHp = _baseMaxHp;
    }

    public void SetBaseStats(int maxHp, int attackDamage, float moveSpeed, float attackRange)
    {
        _baseMaxHp = maxHp;
        _baseAttackDamage = attackDamage;
        _baseMoveSpeed = moveSpeed;
        _baseAttackRange = attackRange;
    }

    public void AddModifier(Modifier mod)
    {
        int oldMaxHp = MaxHp;
        _modifiers.Add(mod);
        if (mod.Target == StatTarget.MaxHp && MaxHp > oldMaxHp)
            CurrentHp += MaxHp - oldMaxHp;
    }

    public void AddToBackpack(Modifier mod) => _backpack.Add(mod);

    public bool RemoveFromBackpack(Modifier mod) => _backpack.Remove(mod);

    public void ResetHp() => CurrentHp = MaxHp;

    /// <summary>
    /// Swaps a modifier from the backpack into a weapon slot. Returns the old slot modifier
    /// (which goes back to backpack), or null if the slot was empty.
    /// </summary>
    public Modifier SwapWeaponSlot(int slotIndex, Modifier newMod)
    {
        if (Weapon == null || slotIndex < 0 || slotIndex >= Weapon.Slots.Length) return null;

        int oldMaxHp = MaxHp;

        var old = Weapon.Slots[slotIndex];
        Weapon.Slots[slotIndex] = newMod;
        _backpack.Remove(newMod);
        if (old != null)
            _backpack.Add(old);

        // Adjust current HP if max changed
        int newMaxHp = MaxHp;
        if (newMaxHp > oldMaxHp)
            CurrentHp += newMaxHp - oldMaxHp;
        else if (CurrentHp > newMaxHp)
            CurrentHp = newMaxHp;

        return old;
    }

    /// <summary>
    /// Preview what a stat would be if a weapon slot were swapped.
    /// </summary>
    public float PreviewStatWithSwap(StatTarget target, int slotIndex, Modifier newMod)
    {
        if (Weapon == null) return ComputeStat(target, GetBase(target));

        var old = Weapon.Slots[slotIndex];
        Weapon.Slots[slotIndex] = newMod;
        float result = ComputeStat(target, GetBase(target));
        Weapon.Slots[slotIndex] = old;
        return result;
    }

    private float GetBase(StatTarget target) => target switch
    {
        StatTarget.MaxHp => _baseMaxHp,
        StatTarget.AttackDamage => _baseAttackDamage,
        StatTarget.MoveSpeed => _baseMoveSpeed,
        StatTarget.AttackRange => _baseAttackRange,
        _ => 0
    };

    /// <summary>
    /// Applies modifiers in order: +N -> +N% -> xM -> -N%.
    /// Includes weapon slot modifiers and general modifiers. Minimum result is 1.
    /// </summary>
    private float ComputeStat(StatTarget target, float baseValue)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;

        void Accumulate(Modifier mod)
        {
            if (mod == null || mod.Target != target) return;
            switch (mod.Op)
            {
                case ModifierOp.FlatAdd: flat += mod.Value; break;
                case ModifierOp.PercentAdd: percentAdd += mod.Value; break;
                case ModifierOp.Multiply: multiply *= mod.Value; break;
                case ModifierOp.PercentReduce: percentReduce += mod.Value; break;
            }
        }

        // Weapon slot modifiers
        if (Weapon != null)
        {
            foreach (var slot in Weapon.Slots)
                Accumulate(slot);
        }

        // General modifiers
        foreach (var mod in _modifiers)
            Accumulate(mod);

        float result = baseValue + flat;
        result *= (1 + percentAdd / 100f);
        result *= multiply;
        result *= (1 - percentReduce / 100f);
        return Math.Max(1, result);
    }
}
