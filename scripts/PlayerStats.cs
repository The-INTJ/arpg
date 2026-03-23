using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds a player's base stats + modifier stack. Effective stats are computed on the fly.
/// Archetype sets the base values; modifiers layer on top.
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

    // Effective stats (base + modifiers)
    public int MaxHp => (int)ComputeStat(StatTarget.MaxHp, _baseMaxHp);
    public int AttackDamage => (int)ComputeStat(StatTarget.AttackDamage, _baseAttackDamage);
    public float MoveSpeed => ComputeStat(StatTarget.MoveSpeed, _baseMoveSpeed);
    public float AttackRange => ComputeStat(StatTarget.AttackRange, _baseAttackRange);

    // Current mutable state
    public int CurrentHp { get; set; }
    public bool IsAlive => CurrentHp > 0;

    public IReadOnlyList<Modifier> Modifiers => _modifiers;

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
        // If max HP increased, grant the extra HP
        if (mod.Target == StatTarget.MaxHp && MaxHp > oldMaxHp)
            CurrentHp += MaxHp - oldMaxHp;
    }

    public void ResetHp() => CurrentHp = MaxHp;

    /// <summary>
    /// Applies modifiers in order: +N → +N% → ×M → −N%. Minimum result is 1.
    /// </summary>
    private float ComputeStat(StatTarget target, float baseValue)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;

        foreach (var mod in _modifiers)
        {
            if (mod.Target != target) continue;
            switch (mod.Op)
            {
                case ModifierOp.FlatAdd: flat += mod.Value; break;
                case ModifierOp.PercentAdd: percentAdd += mod.Value; break;
                case ModifierOp.Multiply: multiply *= mod.Value; break;
                case ModifierOp.PercentReduce: percentReduce += mod.Value; break;
            }
        }

        float result = baseValue + flat;
        result *= (1 + percentAdd / 100f);
        result *= multiply;
        result *= (1 - percentReduce / 100f);
        return Math.Max(1, result);
    }
}
