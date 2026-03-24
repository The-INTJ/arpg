using System.Collections.Generic;
using System.Linq;
using System;

namespace ARPG;

public static partial class StatTargetInfo
{
    public static readonly StatTarget[] All =
    {
        StatTarget.MaxHp,
        StatTarget.AttackDamage,
        StatTarget.MoveSpeed,
        StatTarget.AttackRange,
        StatTarget.InventorySlots,
        StatTarget.ItemUsesPerTurn
    };

    public static string DisplayName(StatTarget target) => target switch
    {
        StatTarget.MaxHp => "HP",
        StatTarget.AttackDamage => "ATK",
        StatTarget.MoveSpeed => "SPD",
        StatTarget.AttackRange => "Range",
        StatTarget.InventorySlots => "Item Slots",
        StatTarget.ItemUsesPerTurn => "Item Uses",
        _ => target.ToString()
    };

    public static bool IsDiscrete(StatTarget target) => target switch
    {
        StatTarget.MaxHp => true,
        StatTarget.AttackDamage => true,
        StatTarget.InventorySlots => true,
        StatTarget.ItemUsesPerTurn => true,
        _ => false
    };

    public static string FormatStatValue(StatTarget target, float value)
    {
        return IsDiscrete(target) ? $"{(int)value}" : $"{value:0.#}";
    }

    public static string FormatStatValueWithProgress(StatTarget target, float value)
    {
        if (!IsDiscrete(target))
            return FormatStatValue(target, value);

        int effective = (int)value;
        if (Math.Abs(value - effective) < 0.001f)
            return $"{effective}";

        return $"{value:0.#} ({effective} effective)";
    }

    public static string DescribeTargetChoice(IReadOnlyList<StatTarget> targets)
    {
        if (targets.Count == All.Length)
            return "Any Stat";

        if (targets.Count == 1)
            return DisplayName(targets[0]);

        return string.Join("/", targets.Select(DisplayName));
    }
}
