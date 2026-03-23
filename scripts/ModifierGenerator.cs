using Godot;

namespace ARPG;

/// <summary>
/// Generates random modifiers for loot drops.
/// </summary>
public static class ModifierGenerator
{
    private static readonly StatTarget[] Targets =
    {
        StatTarget.MaxHp, StatTarget.AttackDamage,
        StatTarget.MoveSpeed, StatTarget.AttackRange
    };

    public static Modifier Random()
    {
        var target = Targets[GD.RandRange(0, Targets.Length - 1)];

        // Weighted toward flat adds early; percent/multiply will matter more with archetypes
        int roll = GD.RandRange(0, 9);
        ModifierOp op = roll < 6 ? ModifierOp.FlatAdd : ModifierOp.PercentAdd;

        float value = (op, target) switch
        {
            (ModifierOp.FlatAdd, StatTarget.MaxHp) => GD.RandRange(2, 4),
            (ModifierOp.FlatAdd, StatTarget.AttackDamage) => GD.RandRange(1, 2),
            (ModifierOp.FlatAdd, StatTarget.MoveSpeed) => (float)GD.RandRange(0.3, 0.8),
            (ModifierOp.FlatAdd, StatTarget.AttackRange) => (float)GD.RandRange(0.3, 0.6),
            (ModifierOp.PercentAdd, _) => GD.RandRange(5, 15),
            _ => 1
        };

        return new Modifier(op, target, value);
    }
}
