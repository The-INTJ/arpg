using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public partial class MonsterEffectSpawnRules
{
    private readonly WeightedIntOption[] _optionalCountWeights;
    private readonly WeightedIntOption[] _tierWeights;

    public float OptionalEffectChance { get; }
    public float AdditionalEffectDecay { get; }
    public float GuaranteedEffectOptionalChanceMultiplier { get; }
    public float BossSafeWeightMultiplier { get; }
    public float NonBossSafeWeightMultiplier { get; }
    public int ThreatBudget { get; }
    public int MaxOptionalEffects { get; }
    public int MaxTotalEffects { get; }
    public int MaxTier { get; }
    public IReadOnlyList<WeightedIntOption> OptionalCountWeights => _optionalCountWeights;
    public IReadOnlyList<WeightedIntOption> TierWeights => _tierWeights;

    public MonsterEffectSpawnRules(
        float optionalEffectChance,
        float additionalEffectDecay,
        float guaranteedEffectOptionalChanceMultiplier,
        int threatBudget,
        int maxOptionalEffects,
        int maxTotalEffects,
        int maxTier,
        IEnumerable<WeightedIntOption> optionalCountWeights,
        IEnumerable<WeightedIntOption> tierWeights,
        float bossSafeWeightMultiplier = 1.0f,
        float nonBossSafeWeightMultiplier = 1.0f)
    {
        OptionalEffectChance = Math.Clamp(optionalEffectChance, 0.0f, 1.0f);
        AdditionalEffectDecay = Math.Clamp(additionalEffectDecay, 0.0f, 1.0f);
        GuaranteedEffectOptionalChanceMultiplier = Math.Clamp(guaranteedEffectOptionalChanceMultiplier, 0.0f, 1.0f);
        ThreatBudget = Math.Max(0, threatBudget);
        MaxOptionalEffects = Math.Max(0, maxOptionalEffects);
        MaxTotalEffects = Math.Max(0, maxTotalEffects);
        MaxTier = Math.Max(0, maxTier);
        BossSafeWeightMultiplier = Math.Max(0.0f, bossSafeWeightMultiplier);
        NonBossSafeWeightMultiplier = Math.Max(0.0f, nonBossSafeWeightMultiplier);
        _optionalCountWeights = optionalCountWeights?
            .Where(option => option.Weight > 0.0f)
            .ToArray() ?? Array.Empty<WeightedIntOption>();
        _tierWeights = tierWeights?
            .Where(option => option.Weight > 0.0f)
            .ToArray() ?? Array.Empty<WeightedIntOption>();
    }
}
