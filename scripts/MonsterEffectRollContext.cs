using System;
using System.Collections.Generic;

namespace ARPG;

public partial class MonsterEffectRollContext
{
    private readonly HashSet<string> _blockedEffectIds = new();

    public int RoomNumber { get; }
    public RoomMonsterEffectProfile Profile { get; }
    public bool IsBoss { get; }
    public MonsterEffectTag PreferredTags { get; }
    public MonsterEffectTag BlockedTags { get; }

    public int ThreatBudget => IsBoss ? Profile.BossThreatBudget : Profile.NormalThreatBudget;
    public int MaxOptionalEffects => IsBoss ? Profile.BossMaxOptionalEffects : Profile.NormalMaxOptionalEffects;
    public int MaxTotalEffects => IsBoss ? Profile.BossMaxTotalEffects : Profile.NormalMaxTotalEffects;
    public int MaxTier => IsBoss ? Profile.BossMaxTier : Profile.NormalMaxTier;

    public MonsterEffectRollContext(
        int roomNumber,
        RoomMonsterEffectProfile profile,
        bool isBoss,
        MonsterEffectTag preferredTags = MonsterEffectTag.None,
        MonsterEffectTag blockedTags = MonsterEffectTag.None,
        IEnumerable<string> blockedEffectIds = null)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        RoomNumber = roomNumber;
        IsBoss = isBoss;
        PreferredTags = preferredTags;
        BlockedTags = blockedTags;

        if (blockedEffectIds == null)
            return;

        foreach (string effectId in blockedEffectIds)
        {
            if (!string.IsNullOrWhiteSpace(effectId))
                _blockedEffectIds.Add(effectId);
        }
    }

    public bool IsBlocked(MonsterEffectDefinition definition)
    {
        if (definition == null)
            return true;

        if (_blockedEffectIds.Contains(definition.Id))
            return true;

        return BlockedTags != MonsterEffectTag.None && (definition.Tags & BlockedTags) != 0;
    }

    public float GetContextWeightMultiplier(MonsterEffectDefinition definition)
    {
        if (definition == null)
            return 0.0f;

        if (BlockedTags != MonsterEffectTag.None && (definition.Tags & BlockedTags) != 0)
            return 0.0f;

        float multiplier = 1.0f;
        if (PreferredTags != MonsterEffectTag.None && (definition.Tags & PreferredTags) != 0)
            multiplier *= 1.5f;

        return multiplier;
    }
}
