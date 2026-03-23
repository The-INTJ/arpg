using System;
using Godot;

namespace ARPG;

public enum MonsterEffectGrantScope
{
    AllMonsters,
    BossOnly,
    NormalEnemies,
    RandomSubset,
}

public partial class MonsterEffectGrantRule
{
    public string EffectId { get; }
    public MonsterEffectGrantScope Scope { get; }
    public int Tier { get; }
    public float SubsetChance { get; }
    public bool ConsumesThreatBudget { get; }
    public bool AllowOptionalRollsAfterGrant { get; }

    public MonsterEffectGrantRule(
        string effectId,
        MonsterEffectGrantScope scope,
        int tier,
        float subsetChance = 1.0f,
        bool consumesThreatBudget = true,
        bool allowOptionalRollsAfterGrant = true)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            throw new ArgumentException("Grant rules need an effect id.", nameof(effectId));

        EffectId = effectId;
        Scope = scope;
        Tier = Math.Max(0, tier);
        SubsetChance = Mathf.Clamp(subsetChance, 0.0f, 1.0f);
        ConsumesThreatBudget = consumesThreatBudget;
        AllowOptionalRollsAfterGrant = allowOptionalRollsAfterGrant;
    }

    public bool AppliesTo(bool isBoss)
    {
        return Scope switch
        {
            MonsterEffectGrantScope.AllMonsters => true,
            MonsterEffectGrantScope.BossOnly => isBoss,
            MonsterEffectGrantScope.NormalEnemies => !isBoss,
            MonsterEffectGrantScope.RandomSubset => GD.Randf() <= SubsetChance,
            _ => false,
        };
    }
}
