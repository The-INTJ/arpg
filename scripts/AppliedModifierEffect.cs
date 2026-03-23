using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// A modifier effect that has been assigned to a concrete stat target.
/// Stat computation only consumes these resolved effects.
/// </summary>
public partial class AppliedModifierEffect
{
    public Modifier SourceModifier { get; }
    public ModifierEffect Effect { get; }
    public StatTarget Target { get; }
    public ModifierOp Op => Effect.Op;
    public float Value => Effect.Value;
    public string Description => Effect.DescribeAssigned(Target);

    public AppliedModifierEffect(Modifier sourceModifier, ModifierEffect effect, StatTarget target)
    {
        SourceModifier = sourceModifier;
        Effect = effect;
        Target = target;
    }

    public static string DescribeCombined(StatTarget target, IEnumerable<AppliedModifierEffect> effects)
    {
        float flat = 0;
        float percentAdd = 0;
        float multiply = 1;
        float percentReduce = 0;
        bool hasMultiply = false;

        foreach (var effect in effects)
        {
            if (effect == null || effect.Target != target)
                continue;

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
                    hasMultiply = true;
                    break;
                case ModifierOp.PercentReduce:
                    percentReduce += effect.Value;
                    break;
            }
        }

        var parts = new List<string>();
        if (flat != 0)
            parts.Add(ModifierEffect.DescribeAssigned(ModifierOp.FlatAdd, flat, target));
        if (percentAdd != 0)
            parts.Add(ModifierEffect.DescribeAssigned(ModifierOp.PercentAdd, percentAdd, target));
        if (hasMultiply && multiply != 1)
            parts.Add(ModifierEffect.DescribeAssigned(ModifierOp.Multiply, multiply, target));
        if (percentReduce != 0)
            parts.Add(ModifierEffect.DescribeAssigned(ModifierOp.PercentReduce, percentReduce, target));

        return parts.Count > 0 ? string.Join(", ", parts) : "(empty)";
    }
}
