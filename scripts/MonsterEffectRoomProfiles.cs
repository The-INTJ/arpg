using System.Collections.Generic;

namespace ARPG;

public static partial class MonsterEffectRoomProfiles
{
    private static readonly RoomMonsterEffectProfile[] Profiles =
    {
        new RoomMonsterEffectProfile(
            id: "room_1_quiet",
            displayName: "Wild Den",
            description: "Monster effects appear often now, but only at tier 0. Extra effects are still rare.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.50f,
                additionalEffectDecay: 0.18f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 7,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 0,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.12f),
                },
                tierWeights: new[] { new WeightedIntOption(0, 1.0f) },
                bossSafeWeightMultiplier: 1.1f,
                nonBossSafeWeightMultiplier: 0.95f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.55f,
                additionalEffectDecay: 0.22f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 9,
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
                nonBossSafeWeightMultiplier: 0.85f)),
        new RoomMonsterEffectProfile(
            id: "room_2_guarded",
            displayName: "Guarded Hall",
            description: "Defense effects are favored, and some enemies enter combat already braced at tier 1.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.60f,
                additionalEffectDecay: 0.28f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 12,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 1,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.28f),
                },
                tierWeights: new[]
                {
                    new WeightedIntOption(0, 0.35f),
                    new WeightedIntOption(1, 1.0f),
                },
                bossSafeWeightMultiplier: 1.35f,
                nonBossSafeWeightMultiplier: 0.9f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.68f,
                additionalEffectDecay: 0.34f,
                guaranteedEffectOptionalChanceMultiplier: 0.20f,
                threatBudget: 14,
                maxOptionalEffects: 2,
                maxTotalEffects: 3,
                maxTier: 1,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.45f),
                },
                tierWeights: new[]
                {
                    new WeightedIntOption(0, 0.20f),
                    new WeightedIntOption(1, 1.0f),
                },
                bossSafeWeightMultiplier: 1.4f,
                nonBossSafeWeightMultiplier: 0.8f),
            guaranteedEffects: new[]
            {
                new MonsterEffectGrantRule("bulwark", MonsterEffectGrantScope.RandomSubset, 1, subsetChance: 0.35f),
            },
            tagWeightMultipliers: new Dictionary<MonsterEffectTag, float>
            {
                [MonsterEffectTag.Defense] = 1.8f,
            }),
        new RoomMonsterEffectProfile(
            id: "room_3_pressure",
            displayName: "Pressure Chamber",
            description: "Tier 1-2 effects dominate, and the boss is always fortified before optional rolls.",
            normalRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.72f,
                additionalEffectDecay: 0.38f,
                guaranteedEffectOptionalChanceMultiplier: 0.22f,
                threatBudget: 15,
                maxOptionalEffects: 2,
                maxTotalEffects: 2,
                maxTier: 2,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.40f),
                },
                tierWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.60f),
                },
                bossSafeWeightMultiplier: 1.25f,
                nonBossSafeWeightMultiplier: 0.95f),
            bossRules: new MonsterEffectSpawnRules(
                optionalEffectChance: 0.82f,
                additionalEffectDecay: 0.50f,
                guaranteedEffectOptionalChanceMultiplier: 0.18f,
                threatBudget: 20,
                maxOptionalEffects: 2,
                maxTotalEffects: 3,
                maxTier: 2,
                optionalCountWeights: new[]
                {
                    new WeightedIntOption(1, 1.0f),
                    new WeightedIntOption(2, 0.85f),
                },
                tierWeights: new[]
                {
                    new WeightedIntOption(1, 0.8f),
                    new WeightedIntOption(2, 1.4f),
                },
                bossSafeWeightMultiplier: 1.45f,
                nonBossSafeWeightMultiplier: 0.7f),
            guaranteedEffects: new[]
            {
                new MonsterEffectGrantRule("bulwark", MonsterEffectGrantScope.BossOnly, 2),
            }),
    };

    public static RoomMonsterEffectProfile ForRoom(int roomNumber)
    {
        int index = System.Math.Clamp(roomNumber - 1, 0, Profiles.Length - 1);
        return Profiles[index];
    }
}
