using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds a player's base stats plus resolved modifier effects. Effects remain target-agnostic
/// while in the backpack, then become concrete AppliedModifierEffects when the player assigns them.
/// </summary>
public partial class PlayerStats
{
    private PlayerBaseStats _baseStats = PlayerBaseStats.Default;
    private Weapon _weapon;
    private int _pendingWardCharges;
    private int _pendingAttackBonusDamage;
    private float _pendingAttackMultiplier = 1.0f;

    public float SprintMultiplier { get; set; } = 2.0f;
    public float HpRegenRate { get; set; } = 0.0f;

    private readonly List<AppliedModifierEffect> _modifiers = new();
    private readonly List<Modifier> _backpack = new();

    /// <summary>The player's equipped weapon (provides stat channels plus ability).</summary>
    public Weapon Weapon
    {
        get => _weapon;
        set
        {
            int oldMaxHp = MaxHp;
            _weapon = value;
            AdjustCurrentHpForMaxChange(oldMaxHp);
            SyncInventoryCapacity();
        }
    }

    public PlayerInventory Inventory { get; } = new(2);

    public int MaxHp => (int)GetEffectiveStatValue(StatTarget.MaxHp);
    public int AttackDamage => (int)GetEffectiveStatValue(StatTarget.AttackDamage);
    public float MoveSpeed => GetEffectiveStatValue(StatTarget.MoveSpeed);
    public float AttackRange => GetEffectiveStatValue(StatTarget.AttackRange);
    public float JumpHeight => GetEffectiveStatValue(StatTarget.JumpHeight);
    public float JumpCount => GetEffectiveStatValue(StatTarget.JumpCount);
    public int DesiredInventorySlotCount => ClampInventorySlots((int)GetEffectiveStatValue(StatTarget.InventorySlots));
    public float RawItemUsesPerTurn => GetEffectiveStatValue(StatTarget.ItemUsesPerTurn);
    public int ItemUsesPerTurn => ClampItemUsesPerTurn((int)RawItemUsesPerTurn);
    public int InventorySlotCount => Inventory.Capacity;
    public int PendingWardCharges => _pendingWardCharges;
    public int PendingAttackBonusDamage => _pendingAttackBonusDamage;
    public float PendingAttackMultiplier => _pendingAttackMultiplier;

    // Current mutable state.
    public int CurrentHp { get; set; }
    public bool IsAlive => CurrentHp > 0;

    public IReadOnlyList<AppliedModifierEffect> Modifiers => _modifiers;
    public IReadOnlyList<Modifier> Backpack => _backpack;

    public PlayerStats()
    {
        CurrentHp = _baseStats.MaxHp;
        SyncInventoryCapacity();
    }

    public void SetBaseStats(PlayerBaseStats baseStats)
    {
        int oldMaxHp = MaxHp;
        _baseStats = baseStats;
        AdjustCurrentHpForMaxChange(oldMaxHp);
        SyncInventoryCapacity();
    }

    public void AddModifier(AppliedModifierEffect effect)
    {
        if (effect == null)
            return;

        int oldMaxHp = MaxHp;
        _modifiers.Add(effect);
        AdjustCurrentHpForMaxChange(oldMaxHp);
        SyncInventoryCapacity();
    }

    public void AddToBackpack(Modifier modifier) => _backpack.Add(modifier);
    public bool RemoveFromBackpack(Modifier modifier) => _backpack.Remove(modifier);
    public bool HasBackpackModifier(Modifier modifier) => modifier != null && _backpack.Contains(modifier);

    public void ResetHp() => CurrentHp = MaxHp;

    public float GetEffectiveStatValue(StatTarget target) => ComputeStat(target, GetBase(target));

    public WeaponStatChannel GetWeaponChannel(StatTarget target)
    {
        return Weapon?.GetChannel(target);
    }

    public bool ApplyBackpackModifier(ModifierAssignmentPlan plan)
    {
        if (Weapon == null || plan == null || !plan.IsComplete || !_backpack.Contains(plan.Modifier))
            return false;

        int oldMaxHp = MaxHp;
        foreach (var effect in plan.BuildAppliedEffects())
            Weapon.GetChannel(effect.Target).Add(effect);

        _backpack.Remove(plan.Modifier);
        AdjustCurrentHpForMaxChange(oldMaxHp);
        SyncInventoryCapacity();
        return true;
    }

    public float PreviewStatWithEffects(StatTarget target, IReadOnlyList<AppliedModifierEffect> extraEffects)
    {
        return ComputeStat(target, GetBase(target), extraEffects);
    }

    public int PreviewCurrentHpWithEffects(IReadOnlyList<AppliedModifierEffect> extraEffects)
    {
        int beforeMaxHp = MaxHp;
        int afterMaxHp = (int)PreviewStatWithEffects(StatTarget.MaxHp, extraEffects);

        if (afterMaxHp > beforeMaxHp)
            return CurrentHp + (afterMaxHp - beforeMaxHp);

        return Math.Min(CurrentHp, afterMaxHp);
    }

    public int PreviewInventorySlotCountWithEffects(IReadOnlyList<AppliedModifierEffect> extraEffects)
    {
        int desiredCapacity = ClampInventorySlots((int)PreviewStatWithEffects(StatTarget.InventorySlots, extraEffects));
        return Math.Max(desiredCapacity, Inventory.MinimumRequiredCapacity);
    }

    public int PreviewItemUsesPerTurnWithEffects(IReadOnlyList<AppliedModifierEffect> extraEffects)
    {
        return ClampItemUsesPerTurn((int)PreviewStatWithEffects(StatTarget.ItemUsesPerTurn, extraEffects));
    }

    public void QueueWard(int charges)
    {
        _pendingWardCharges += Math.Max(0, charges);
    }

    public void QueueAttackBonusDamage(int amount)
    {
        _pendingAttackBonusDamage += Math.Max(0, amount);
    }

    public void QueueAttackMultiplier(float multiplier)
    {
        _pendingAttackMultiplier *= Math.Max(1.0f, multiplier);
    }

    public int ConsumePreparedAttackDamage(int baseDamage, out string feedback)
    {
        int resolvedDamage = Math.Max(0, baseDamage);
        List<string> notes = null;

        if (_pendingAttackBonusDamage > 0)
        {
            resolvedDamage += _pendingAttackBonusDamage;
            notes ??= new List<string>();
            notes.Add($"+{_pendingAttackBonusDamage} damage");
            _pendingAttackBonusDamage = 0;
        }

        if (_pendingAttackMultiplier > 1.001f)
        {
            resolvedDamage = Math.Max(0, (int)MathF.Round(resolvedDamage * _pendingAttackMultiplier));
            notes ??= new List<string>();
            notes.Add($"x{_pendingAttackMultiplier:0.#} power");
            _pendingAttackMultiplier = 1.0f;
        }

        feedback = notes == null ? string.Empty : $"Empowered attack: {string.Join(", ", notes)}";
        return resolvedDamage;
    }

    public int ResolveIncomingDamage(int amount, out string feedback)
    {
        int resolvedDamage = Math.Max(0, amount);

        if (resolvedDamage > 0 && _pendingWardCharges > 0)
        {
            _pendingWardCharges--;
            feedback = "A ward absorbs the hit.";
            return 0;
        }

        feedback = string.Empty;
        return resolvedDamage;
    }

    private float GetBase(StatTarget target) => target switch
    {
        StatTarget.MaxHp => _baseStats.MaxHp,
        StatTarget.AttackDamage => _baseStats.AttackDamage,
        StatTarget.MoveSpeed => _baseStats.MoveSpeed,
        StatTarget.AttackRange => _baseStats.AttackRange,
        StatTarget.JumpHeight => _baseStats.JumpHeight,
        StatTarget.JumpCount => _baseStats.JumpCount,
        StatTarget.InventorySlots => _baseStats.InventorySlots,
        StatTarget.ItemUsesPerTurn => _baseStats.ItemUsesPerTurn,
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

    private void SyncInventoryCapacity()
    {
        Inventory.SetCapacity(DesiredInventorySlotCount);
    }

    private static int ClampInventorySlots(int value)
    {
        return Math.Max(1, value);
    }

    private static int ClampItemUsesPerTurn(int value)
    {
        return Math.Max(1, value);
    }

    /// <summary>
    /// Applies modifiers in order: +N -> +N% -> xM -> -N%.
    /// Includes weapon-applied effects, general effects, and optional preview effects.
    /// Minimum result is 1.
    /// </summary>
    private float ComputeStat(StatTarget target, float baseValue, IReadOnlyList<AppliedModifierEffect> extraEffects = null)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;

        void Accumulate(AppliedModifierEffect effect)
        {
            if (effect == null || effect.Target != target)
                return;

            switch (effect.Op)
            {
                case ModifierOp.FlatAdd:
                    flat += effect.Value;
                    break;
                case ModifierOp.PercentAdd:
                    percentAdd += effect.Value;
                    break;
                case ModifierOp.Multiply:
                    multiply *= effect.Value;
                    break;
                case ModifierOp.PercentReduce:
                    percentReduce += effect.Value;
                    break;
            }
        }

        if (Weapon != null)
        {
            foreach (var channel in Weapon.Channels)
            {
                foreach (var effect in channel.Effects)
                    Accumulate(effect);
            }
        }

        foreach (var effect in _modifiers)
            Accumulate(effect);

        if (extraEffects != null)
        {
            foreach (var effect in extraEffects)
                Accumulate(effect);
        }

        float result = baseValue + flat;
        result *= (1 + percentAdd / 100f);
        result *= multiply;
        result *= (1 - percentReduce / 100f);
        return StatTargetInfo.ClampValue(target, result);
    }
}
