using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public static partial class MonsterEffectRoller
{
    public static int RollWeightedInt(IEnumerable<WeightedIntOption> options, int fallback = 0)
    {
        var validOptions = options?
            .Where(option => option.Weight > 0.0f)
            .ToArray() ?? System.Array.Empty<WeightedIntOption>();

        if (validOptions.Length == 0)
            return fallback;

        float totalWeight = validOptions.Sum(option => option.Weight);
        float roll = GD.Randf() * totalWeight;

        foreach (var option in validOptions)
        {
            roll -= option.Weight;
            if (roll <= 0.0f)
                return option.Value;
        }

        return validOptions[^1].Value;
    }
}
