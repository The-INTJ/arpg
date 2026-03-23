namespace ARPG;

public static partial class MonsterEffectRoomProfiles
{
    private static readonly RoomMonsterEffectProfile[] Profiles =
    {
        new RoomMonsterEffectProfile(
            id: "room_1_quiet",
            displayName: "Quiet Den",
            description: "Most enemies are plain; a rare few roll a single basic monster effect.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.22f,
                additionalEffectDecay: 0.18f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 4,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.05f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.1f,
                nonBossSafeWeightMultiplier: 0.95f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.28f,
                additionalEffectDecay: 0.22f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 5,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.12f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.2f,
                nonBossSafeWeightMultiplier: 0.85f)),
        new RoomMonsterEffectProfile(
            id: "room_2_restless",
            displayName: "Restless Hall",
            description: "Monster effects are more common, but stacking is still intentionally rare.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.30f,
                additionalEffectDecay: 0.28f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 5,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.18f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.2f,
                nonBossSafeWeightMultiplier: 0.9f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.38f,
                additionalEffectDecay: 0.34f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 5,
                maxOptionalEffects: 2,
                maxTotalEffects: 3,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.30f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.25f,
                nonBossSafeWeightMultiplier: 0.85f)),
        new RoomMonsterEffectProfile(
            id: "room_3_pressure",
            displayName: "Pressure Chamber",
            description: "Effects show up more often, and bosses are weighted toward safer defensive patterns.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.36f,
                additionalEffectDecay: 0.32f,
                guaranteedEffectOptionalChanceMultiplier: 0.18f,
                threatBudget: 5,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.26f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.2f,
                nonBossSafeWeightMultiplier: 0.9f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.48f,
                additionalEffectDecay: 0.45f,
                guaranteedEffectOptionalChanceMultiplier: 0.18f,
                threatBudget: 6,
                maxOptionalEffects: 2,
                maxTotalEffects: 3,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.65f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.35f,
                nonBossSafeWeightMultiplier: 0.7f)),
    };

    public static RoomMonsterEffectProfile ForRoom(int roomNumber)
    {
        int index = System.Math.Clamp(roomNumber - 1, 0, Profiles.Length - 1);
        return Profiles[index];
    }
}
