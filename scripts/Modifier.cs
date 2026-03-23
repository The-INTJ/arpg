using System.Collections.Generic;

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
    AttackRange
}

public partial class Modifier
{
    public ModifierOp Op { get; }
    public StatTarget Target { get; }
    public float Value { get; }

    public Modifier(ModifierOp op, StatTarget target, float value)
    {
        Op = op;
        Target = target;
        Value = value;
    }

    public string Description => Describe(Op, Target, Value);

    public static string Describe(ModifierOp op, StatTarget target, float value)
    {
        return $"{GetOpSymbol(op)}{FormatValue(op, target, value)} {StatTargetInfo.DisplayName(target)}";
    }

    public static string DescribeCombined(StatTarget target, IEnumerable<Modifier> modifiers)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;
        bool hasMultiply = false;

        foreach (var modifier in modifiers)
        {
            if (modifier == null || modifier.Target != target)
                continue;

            switch (modifier.Op)
            {
                case ModifierOp.FlatAdd:
                    flat += modifier.Value;
                    break;
                case ModifierOp.PercentAdd:
                    percentAdd += modifier.Value;
                    break;
                case ModifierOp.Multiply:
                    multiply *= modifier.Value;
                    hasMultiply = true;
                    break;
                case ModifierOp.PercentReduce:
                    percentReduce += modifier.Value;
                    break;
            }
        }

        var parts = new List<string>();
        if (flat != 0)
            parts.Add(Describe(ModifierOp.FlatAdd, target, flat));
        if (percentAdd != 0)
            parts.Add(Describe(ModifierOp.PercentAdd, target, percentAdd));
        if (hasMultiply && multiply != 1)
            parts.Add(Describe(ModifierOp.Multiply, target, multiply));
        if (percentReduce != 0)
            parts.Add(Describe(ModifierOp.PercentReduce, target, percentReduce));

        return parts.Count > 0 ? string.Join(", ", parts) : "(empty)";
    }

    private static string GetOpSymbol(ModifierOp op) => op switch
    {
        ModifierOp.FlatAdd => "+",
        ModifierOp.PercentAdd => "+",
        ModifierOp.Multiply => "x",
        ModifierOp.PercentReduce => "-",
        _ => ""
    };

    private static string FormatValue(ModifierOp op, StatTarget target, float value) => op switch
    {
        ModifierOp.PercentAdd => $"{value:0}%",
        ModifierOp.PercentReduce => $"{value:0}%",
        ModifierOp.Multiply => $"{value:0.#}",
        ModifierOp.FlatAdd when target == StatTarget.MoveSpeed || target == StatTarget.AttackRange => $"{value:0.#}",
        _ => $"{(int)value}"
    };
}
