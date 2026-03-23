using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

/// <summary>
/// A modifier effect before it has been assigned to a specific stat.
/// Future multi-part upgrades can bundle several of these into one Modifier.
/// </summary>
public partial class ModifierEffect
{
    private readonly StatTarget[] _allowedTargets;

    public ModifierOp Op { get; }
    public float Value { get; }
    public IReadOnlyList<StatTarget> AllowedTargets => _allowedTargets;
    public bool RequiresDistinctTarget { get; }
    public bool IsFixedTarget => _allowedTargets.Length == 1;
    public string ShortLabel => $"{GetOpSymbol(Op)}{FormatValue(Op, Value)}";

    public string Description => IsFixedTarget
        ? DescribeAssigned(_allowedTargets[0])
        : $"{ShortLabel} {StatTargetInfo.DescribeTargetChoice(_allowedTargets)}";

    public ModifierEffect(
        ModifierOp op,
        float value,
        IEnumerable<StatTarget> allowedTargets,
        bool requiresDistinctTarget = false)
    {
        if (allowedTargets == null)
            throw new ArgumentNullException(nameof(allowedTargets));

        _allowedTargets = allowedTargets.Distinct().ToArray();
        if (_allowedTargets.Length == 0)
            throw new ArgumentException("Modifier effect must allow at least one target.", nameof(allowedTargets));

        Op = op;
        Value = value;
        RequiresDistinctTarget = requiresDistinctTarget;
    }

    public bool AllowsTarget(StatTarget target, IReadOnlyCollection<StatTarget> existingTargets = null)
    {
        if (Array.IndexOf(_allowedTargets, target) < 0)
            return false;

        if (RequiresDistinctTarget && existingTargets != null && existingTargets.Contains(target))
            return false;

        return true;
    }

    public StatTarget GetFixedTarget()
    {
        if (!IsFixedTarget)
            throw new InvalidOperationException("This effect does not have a single fixed target.");

        return _allowedTargets[0];
    }

    public string DescribeAssigned(StatTarget target) => DescribeAssigned(Op, Value, target);

    public static string DescribeAssigned(ModifierOp op, float value, StatTarget target)
    {
        return $"{GetOpSymbol(op)}{FormatValue(op, value)} {StatTargetInfo.DisplayName(target)}";
    }

    private static string GetOpSymbol(ModifierOp op) => op switch
    {
        ModifierOp.FlatAdd => "+",
        ModifierOp.PercentAdd => "+",
        ModifierOp.Multiply => "x",
        ModifierOp.PercentReduce => "-",
        _ => ""
    };

    private static string FormatValue(ModifierOp op, float value) => op switch
    {
        ModifierOp.PercentAdd => $"{value:0}%",
        ModifierOp.PercentReduce => $"{value:0}%",
        ModifierOp.Multiply => $"{value:0.#}",
        ModifierOp.FlatAdd when value == (int)value => $"{(int)value}",
        _ => $"{value:0.#}"
    };
}
