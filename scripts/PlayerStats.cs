using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds a player's base stats plus modifier stacks. Effective stats are computed on the fly.
/// Archetype sets the base values; weapon-applied modifiers and general modifiers layer on top.
/// </summary>
public partial class PlayerStats
{
    // Base values set by archetype.
    private int _baseMaxHp = 15;
    private int _baseAttackDamage = 4;
    private float _baseMoveSpeed = 5.0f;
    private float _baseAttackRange = 3.5f;

    public float SprintMultiplier { get; set; } = 2.0f;
    public float HpRegenRate { get; set; } = 0.2f; // 1 HP per 5 seconds

    private readonly List<Modifier> _modifiers = new();
    private readonly List<Modifier> _backpack = new();

    /// <summary>The player's equipped weapon (provides stat channels plus ability).</summary>
    public Weapon Weapon { get; set; }
    public PlayerInventory Inventory { get; } = new(2);

    public int MaxHp => (int)GetEffectiveStatValue(StatTarget.MaxHp);
    public int AttackDamage => (int)GetEffectiveStatValue(StatTarget.AttackDamage);
    public float MoveSpeed => GetEffectiveStatValue(StatTarget.MoveSpeed);
    public float AttackRange => GetEffectiveStatValue(StatTarget.AttackRange);

    // Current mutable state.
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
    public bool HasBackpackModifier(Modifier mod) => mod != null && _backpack.Contains(mod);

    public void ResetHp() => CurrentHp = MaxHp;

    public float GetEffectiveStatValue(StatTarget target) => ComputeStat(target, GetBase(target));

    public WeaponStatChannel GetWeaponChannel(StatTarget target)
    {
        return Weapon?.GetChannel(target);
    }

    /// <summary>
    /// Applies a backpack modifier into its matching weapon stat channel.
    /// The modifier stops living in the backpack and starts affecting the weapon immediately.
    /// </summary>
    public bool ApplyBackpackModifier(Modifier modifier)
    {
        if (Weapon == null || modifier == null || !_backpack.Contains(modifier))
            return false;

        int oldMaxHp = MaxHp;
        Weapon.GetChannel(modifier.Target).Add(modifier);
        _backpack.Remove(modifier);
        AdjustCurrentHpForMaxChange(oldMaxHp);
        return true;
    }

    /// <summary>
    /// Preview what a stat would be if a modifier were applied to the weapon.
    /// </summary>
    public float PreviewStatWithModifier(StatTarget target, Modifier modifier)
    {
        if (modifier == null || Weapon == null)
            return GetEffectiveStatValue(target);

        var channel = Weapon.GetChannel(modifier.Target);
        channel.Add(modifier);
        float result = GetEffectiveStatValue(target);
        channel.Remove(modifier);
        return result;
    }

    public int PreviewCurrentHpWithModifier(Modifier modifier)
    {
        int beforeMaxHp = MaxHp;
        int afterMaxHp = (int)PreviewStatWithModifier(StatTarget.MaxHp, modifier);

        if (afterMaxHp > beforeMaxHp)
            return CurrentHp + (afterMaxHp - beforeMaxHp);

        return Math.Min(CurrentHp, afterMaxHp);
    }

    private float GetBase(StatTarget target) => target switch
    {
        StatTarget.MaxHp => _baseMaxHp,
        StatTarget.AttackDamage => _baseAttackDamage,
        StatTarget.MoveSpeed => _baseMoveSpeed,
        StatTarget.AttackRange => _baseAttackRange,
        _ => 0
    };

    private void AdjustCurrentHpForMaxChange(int oldMaxHp)
    {
        int newMaxHp = MaxHp;
        if (newMaxHp > oldMaxHp)
            CurrentHp += newMaxHp - oldMaxHp;
        else if (CurrentHp > newMaxHp)
            CurrentHp = newMaxHp;
    }

    /// <summary>
    /// Applies modifiers in order: +N -> +N% -> xM -> -N%.
    /// Includes weapon-applied modifiers and general modifiers. Minimum result is 1.
    /// </summary>
    private float ComputeStat(StatTarget target, float baseValue)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;

        void Accumulate(Modifier mod)
        {
            if (mod == null || mod.Target != target)
                return;

            switch (mod.Op)
            {
                case ModifierOp.FlatAdd:
                    flat += mod.Value;
                    break;
                case ModifierOp.PercentAdd:
                    percentAdd += mod.Value;
                    break;
                case ModifierOp.Multiply:
                    multiply *= mod.Value;
                    break;
                case ModifierOp.PercentReduce:
                    percentReduce += mod.Value;
                    break;
            }
        }

        // Weapon-applied modifiers.
        if (Weapon != null)
        {
            foreach (var channel in Weapon.Channels)
            {
                foreach (var modifier in channel.Modifiers)
                    Accumulate(modifier);
            }
        }

        // General modifiers.
        foreach (var mod in _modifiers)
            Accumulate(mod);

        float result = baseValue + flat;
        result *= (1 + percentAdd / 100f);
        result *= multiply;
        result *= (1 - percentReduce / 100f);
        return Math.Max(1, result);
    }
}
