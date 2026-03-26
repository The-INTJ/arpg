using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public static partial class StatTargetInfo
{
    private static readonly StatTargetMetadata[] OrderedMetadata = new[]
    {
        new StatTargetMetadata(StatTarget.MaxHp, "Max HP", IsDiscrete: true, MinimumValue: 1.0f, DisplayOrder: 10),
        new StatTargetMetadata(StatTarget.AttackDamage, "Attack Damage", IsDiscrete: true, MinimumValue: 1.0f, DisplayOrder: 20),
        new StatTargetMetadata(StatTarget.MoveSpeed, "Move Speed", IsDiscrete: false, MinimumValue: 1.0f, DisplayOrder: 30),
        new StatTargetMetadata(StatTarget.AttackReach, "Attack Reach", IsDiscrete: false, MinimumValue: 0.25f, DisplayOrder: 40),
        new StatTargetMetadata(StatTarget.AttackSize, "Attack Size", IsDiscrete: false, MinimumValue: 0.25f, DisplayOrder: 50),
        new StatTargetMetadata(StatTarget.JumpHeight, "Jump Height", IsDiscrete: false, MinimumValue: 0.1f, DisplayOrder: 60),
        new StatTargetMetadata(StatTarget.JumpCount, "Jump Count", IsDiscrete: false, MinimumValue: 1.0f, DisplayOrder: 70),
        new StatTargetMetadata(StatTarget.InventorySlots, "Item Slots", IsDiscrete: true, MinimumValue: 1.0f, DisplayOrder: 80),
        new StatTargetMetadata(StatTarget.ItemUsesPerTurn, "Item Uses", IsDiscrete: true, MinimumValue: 1.0f, DisplayOrder: 90),
    }
        .OrderBy(metadata => metadata.DisplayOrder)
        .ToArray();

    private static readonly Dictionary<StatTarget, StatTargetMetadata> MetadataByTarget = OrderedMetadata
        .ToDictionary(metadata => metadata.Target);

    private static readonly Dictionary<ModifierOp, StatTarget[]> FlexibleTargetsByOp = new()
    {
        [ModifierOp.FlatAdd] = BuildFlexibleTargets(ModifierOp.FlatAdd),
        [ModifierOp.PercentAdd] = BuildFlexibleTargets(ModifierOp.PercentAdd),
        [ModifierOp.Multiply] = BuildFlexibleTargets(ModifierOp.Multiply),
        [ModifierOp.PercentReduce] = BuildFlexibleTargets(ModifierOp.PercentReduce),
    };

    public static IReadOnlyList<StatTargetMetadata> Ordered => OrderedMetadata;
    public static readonly StatTarget[] All = OrderedMetadata.Select(metadata => metadata.Target).ToArray();

    public static string DisplayName(StatTarget target) => GetMetadata(target).DisplayName;

    public static bool IsDiscrete(StatTarget target) => GetMetadata(target).IsDiscrete;

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
        if (MatchesTargets(targets, All))
            return "Any Stat";

        if (MatchesTargets(targets, FlexibleTargetsFor(ModifierOp.PercentAdd)))
            return "Any Eligible Stat";

        if (targets.Count == 1)
            return DisplayName(targets[0]);

        return string.Join("/", targets.Select(DisplayName));
    }

    public static StatTargetMetadata GetMetadata(StatTarget target)
    {
        return MetadataByTarget.TryGetValue(target, out StatTargetMetadata metadata)
            ? metadata
            : throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown stat target.");
    }

    public static IReadOnlyList<StatTarget> FlexibleTargetsFor(ModifierOp op)
    {
        return FlexibleTargetsByOp.TryGetValue(op, out StatTarget[] targets)
            ? targets
            : Array.Empty<StatTarget>();
    }

    public static float ClampValue(StatTarget target, float value)
    {
        return Math.Max(GetMetadata(target).MinimumValue, value);
    }

    private static StatTarget[] BuildFlexibleTargets(ModifierOp op)
    {
        return OrderedMetadata
            .Where(metadata => metadata.AllowsFlexibleModifier(op))
            .Select(metadata => metadata.Target)
            .ToArray();
    }

    private static bool MatchesTargets(IReadOnlyList<StatTarget> targets, IReadOnlyList<StatTarget> expected)
    {
        if (targets.Count != expected.Count)
            return false;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != expected[i])
                return false;
        }

        return true;
    }
}
