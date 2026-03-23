namespace ARPG;

public static partial class MonsterEffectRoomProfiles
{
    private static readonly RoomMonsterEffectProfile DefaultProfile = new(
        id: "mvp_default",
        displayName: "Unsettled Den",
        description: "Enemies have a 30% chance to roll one tier-0 monster effect.",
        baseEffectChance: 0.30f,
        normalThreatBudget: 4,
        bossThreatBudget: 5,
        normalMaxOptionalEffects: 1,
        bossMaxOptionalEffects: 1,
        normalMaxTotalEffects: 2,
        bossMaxTotalEffects: 2,
        normalMaxTier: 0,
        bossMaxTier: 0,
        normalOptionalEffectCountWeights: new[] { new WeightedIntOption(1, 1.0f) },
        bossOptionalEffectCountWeights: new[] { new WeightedIntOption(1, 1.0f) },
        normalTierWeights: new[] { new WeightedIntOption(0, 1.0f) },
        bossTierWeights: new[] { new WeightedIntOption(0, 1.0f) });

    public static RoomMonsterEffectProfile ForRoom(int roomNumber)
    {
        _ = roomNumber;
        return DefaultProfile;
    }
}
