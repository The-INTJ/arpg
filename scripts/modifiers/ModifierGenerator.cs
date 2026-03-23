using Godot;

namespace ARPG;

/// <summary>
/// Generates random modifiers for loot drops.
/// </summary>
public static class ModifierGenerator
{
    public static Modifier Random()
    {
        // Flexible loot: the player picks the target later.
        // Keep values generic so one modifier can reasonably land on any stat.
        int roll = GD.RandRange(0, 9);
        ModifierOp op = roll < 7 ? ModifierOp.FlatAdd : ModifierOp.PercentAdd;
        float value = op switch
        {
            ModifierOp.FlatAdd => GD.RandRange(1, 2),
            ModifierOp.PercentAdd => GD.RandRange(10, 20),
            _ => 1
        };

        return Modifier.Flexible(op, value);
    }
}
