using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public static partial class StatTargetInfo
{
    public static readonly StatTarget[] All =
    {
        StatTarget.MaxHp,
        StatTarget.AttackDamage,
        StatTarget.MoveSpeed,
        StatTarget.AttackRange,
        StatTarget.InventorySlots
    };

    public static string DisplayName(StatTarget target) => target switch
    {
        StatTarget.MaxHp => "HP",
        StatTarget.AttackDamage => "ATK",
        StatTarget.MoveSpeed => "SPD",
        StatTarget.AttackRange => "Range",
        StatTarget.InventorySlots => "Item Slots",
        _ => target.ToString()
    };

    public static bool IsDiscrete(StatTarget target) => target switch
    {
        StatTarget.MaxHp => true,
        StatTarget.AttackDamage => true,
        StatTarget.InventorySlots => true,
        _ => false
    };

    public static string FormatStatValue(StatTarget target, float value)
    {
        return IsDiscrete(target) ? $"{(int)value}" : $"{value:0.#}";
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
