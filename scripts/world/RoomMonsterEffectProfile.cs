using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public partial class RoomMonsterEffectProfile
{
    private readonly List<MonsterEffectGrantRule> _guaranteedEffects;
    private readonly Dictionary<string, float> _effectWeightMultipliers;
    private readonly Dictionary<MonsterEffectTag, float> _tagWeightMultipliers;

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public MonsterEffectSpawnRules NormalRules { get; }
    public MonsterEffectSpawnRules BossRules { get; }
    public IReadOnlyList<MonsterEffectGrantRule> GuaranteedEffects => _guaranteedEffects;

    public RoomMonsterEffectProfile(
        string id,
        string displayName,
        string description,
        MonsterEffectSpawnRules normalRules,
        MonsterEffectSpawnRules bossRules,
        IEnumerable<MonsterEffectGrantRule> guaranteedEffects = null,
        IDictionary<string, float> effectWeightMultipliers = null,
        IDictionary<MonsterEffectTag, float> tagWeightMultipliers = null)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        NormalRules = normalRules ?? new MonsterEffectSpawnRules(0.0f, 0.0f, 0.0f, 0, 0, 0, 0, null, null);
        BossRules = bossRules ?? NormalRules;
        _guaranteedEffects = guaranteedEffects?.ToList() ?? new List<MonsterEffectGrantRule>();
        _effectWeightMultipliers = effectWeightMultipliers != null
            ? new Dictionary<string, float>(effectWeightMultipliers)
            : new Dictionary<string, float>();
        _tagWeightMultipliers = tagWeightMultipliers != null
            ? new Dictionary<MonsterEffectTag, float>(tagWeightMultipliers)
            : new Dictionary<MonsterEffectTag, float>();
    }

    public MonsterEffectSpawnRules GetRules(bool isBoss)
    {
        return isBoss ? BossRules : NormalRules;
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
