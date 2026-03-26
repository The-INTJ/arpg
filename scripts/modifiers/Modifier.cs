using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public enum ModifierOp
{
    FlatAdd,       // +N
    PercentAdd,    // +N%
    Multiply,      // xM
    PercentReduce  // -N%
}

public enum StatTarget
{
    MaxHp,
    AttackDamage,
    MoveSpeed,
    AttackReach,
    AttackSize,
    JumpHeight,
    JumpCount,
    InventorySlots,
    ItemUsesPerTurn
}

public partial class Modifier
{
    private readonly ModifierEffect[] _effects;

    public IReadOnlyList<ModifierEffect> Effects => _effects;

    public string Description => string.Join("  +  ", _effects.Select(effect => effect.Description));

    public Modifier(params ModifierEffect[] effects)
    {
        if (effects == null || effects.Length == 0)
            throw new ArgumentException("Modifier must contain at least one effect.", nameof(effects));

        _effects = effects.ToArray();
    }

    public static Modifier Create(params ModifierEffect[] effects) => new(effects);

    public static Modifier Fixed(ModifierOp op, StatTarget target, float value)
    {
        return new Modifier(new ModifierEffect(op, value, new[] { target }));
    }

    public static Modifier Flexible(ModifierOp op, float value)
    {
        return new Modifier(new ModifierEffect(op, value, StatTargetInfo.FlexibleTargetsFor(op)));
    }

    public AppliedModifierEffect[] CreateAppliedEffectsFromFixedTargets()
    {
        var appliedEffects = new AppliedModifierEffect[_effects.Length];
        for (int i = 0; i < _effects.Length; i++)
            appliedEffects[i] = new AppliedModifierEffect(this, _effects[i], _effects[i].GetFixedTarget());

        return appliedEffects;
    }
}
