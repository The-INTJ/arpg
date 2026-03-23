using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public partial class RoomMonsterEffectProfile
{
    private readonly List<MonsterEffectGrantRule> _guaranteedEffects;
    private readonly WeightedIntOption[] _normalOptionalEffectCountWeights;
    private readonly WeightedIntOption[] _bossOptionalEffectCountWeights;
    private readonly WeightedIntOption[] _normalTierWeights;
    private readonly WeightedIntOption[] _bossTierWeights;
    private readonly Dictionary<string, float> _effectWeightMultipliers;
    private readonly Dictionary<MonsterEffectTag, float> _tagWeightMultipliers;

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public float BaseEffectChance { get; }
    public int NormalThreatBudget { get; }
    public int BossThreatBudget { get; }
    public int NormalMaxOptionalEffects { get; }
    public int BossMaxOptionalEffects { get; }
    public int NormalMaxTotalEffects { get; }
    public int BossMaxTotalEffects { get; }
    public int NormalMaxTier { get; }
    public int BossMaxTier { get; }
    public IReadOnlyList<MonsterEffectGrantRule> GuaranteedEffects => _guaranteedEffects;

    public RoomMonsterEffectProfile(
        string id,
        string displayName,
        string description,
        float baseEffectChance,
        int normalThreatBudget,
        int bossThreatBudget,
        int normalMaxOptionalEffects,
        int bossMaxOptionalEffects,
        int normalMaxTotalEffects,
        int bossMaxTotalEffects,
        int normalMaxTier,
        int bossMaxTier,
        IEnumerable<WeightedIntOption> normalOptionalEffectCountWeights,
        IEnumerable<WeightedIntOption> bossOptionalEffectCountWeights,
        IEnumerable<WeightedIntOption> normalTierWeights,
        IEnumerable<WeightedIntOption> bossTierWeights,
        IEnumerable<MonsterEffectGrantRule> guaranteedEffects = null,
        IDictionary<string, float> effectWeightMultipliers = null,
        IDictionary<MonsterEffectTag, float> tagWeightMultipliers = null)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        BaseEffectChance = baseEffectChance;
        NormalThreatBudget = normalThreatBudget;
        BossThreatBudget = bossThreatBudget;
        NormalMaxOptionalEffects = normalMaxOptionalEffects;
        BossMaxOptionalEffects = bossMaxOptionalEffects;
        NormalMaxTotalEffects = normalMaxTotalEffects;
        BossMaxTotalEffects = bossMaxTotalEffects;
        NormalMaxTier = normalMaxTier;
        BossMaxTier = bossMaxTier;

        _normalOptionalEffectCountWeights = normalOptionalEffectCountWeights?.ToArray() ?? System.Array.Empty<WeightedIntOption>();
        _bossOptionalEffectCountWeights = bossOptionalEffectCountWeights?.ToArray() ?? System.Array.Empty<WeightedIntOption>();
        _normalTierWeights = normalTierWeights?.ToArray() ?? System.Array.Empty<WeightedIntOption>();
        _bossTierWeights = bossTierWeights?.ToArray() ?? System.Array.Empty<WeightedIntOption>();
        _guaranteedEffects = guaranteedEffects?.ToList() ?? new List<MonsterEffectGrantRule>();
        _effectWeightMultipliers = effectWeightMultipliers != null
            ? new Dictionary<string, float>(effectWeightMultipliers)
            : new Dictionary<string, float>();
        _tagWeightMultipliers = tagWeightMultipliers != null
            ? new Dictionary<MonsterEffectTag, float>(tagWeightMultipliers)
            : new Dictionary<MonsterEffectTag, float>();
    }

    public IReadOnlyList<WeightedIntOption> GetOptionalCountOptions(bool isBoss)
    {
        return isBoss ? _bossOptionalEffectCountWeights : _normalOptionalEffectCountWeights;
    }

    public IReadOnlyList<WeightedIntOption> GetTierOptions(bool isBoss, int maxTier)
    {
        var source = isBoss ? _bossTierWeights : _normalTierWeights;
        return source
            .Where(option => option.Value <= maxTier && option.Weight > 0.0f)
            .ToArray();
    }

    public float GetWeightMultiplier(MonsterEffectDefinition definition)
    {
        if (definition == null)
            return 0.0f;

        float multiplier = 1.0f;
        if (_effectWeightMultipliers.TryGetValue(definition.Id, out float effectMultiplier))
            multiplier *= effectMultiplier;

        foreach (var pair in _tagWeightMultipliers)
        {
            if ((definition.Tags & pair.Key) != 0)
                multiplier *= pair.Value;
        }

        return multiplier;
    }
}
