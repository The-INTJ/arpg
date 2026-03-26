using System.Linq;
using Xunit;

namespace ARPG.Tests;

public partial class PlayerStatsTests
{
    [Fact]
    public void JumpStatsApplyModifiersAndRespectMinimums()
    {
        var stats = new PlayerStats();
        stats.SetBaseStats(PlayerBaseStats.Default);

        Apply(stats, Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.JumpHeight, 0.6f));
        Apply(stats, Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.JumpCount, 1.0f));

        Assert.Equal(2.5f, stats.JumpHeight, 3);
        Assert.Equal(2, stats.JumpCount);

        Apply(stats, Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.JumpHeight, -20.0f));
        Apply(stats, Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.JumpCount, -20.0f));

        Assert.Equal(StatTargetInfo.GetMetadata(StatTarget.JumpHeight).MinimumValue, stats.JumpHeight, 3);
        Assert.Equal((int)StatTargetInfo.GetMetadata(StatTarget.JumpCount).MinimumValue, stats.JumpCount);
    }

    [Fact]
    public void PreviewIncludesJumpHeightAndJumpCount()
    {
        var stats = new PlayerStats();
        stats.SetBaseStats(PlayerBaseStats.Default);

        var previewEffects = new[]
        {
            Modifier.Fixed(ModifierOp.PercentAdd, StatTarget.JumpHeight, 25.0f).CreateAppliedEffectsFromFixedTargets().Single(),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.JumpCount, 2.0f).CreateAppliedEffectsFromFixedTargets().Single(),
        };

        Assert.Equal(2.375f, stats.PreviewStatWithEffects(StatTarget.JumpHeight, previewEffects), 3);
        Assert.Equal(3.0f, stats.PreviewStatWithEffects(StatTarget.JumpCount, previewEffects), 3);
    }

    [Fact]
    public void FlexiblePercentModifiersExcludeJumpCountButFlatAddsAllowIt()
    {
        var flatModifier = Modifier.Flexible(ModifierOp.FlatAdd, 1.0f);
        var percentModifier = Modifier.Flexible(ModifierOp.PercentAdd, 15.0f);

        Assert.Contains(StatTarget.JumpCount, flatModifier.Effects.Single().AllowedTargets);
        Assert.DoesNotContain(StatTarget.JumpCount, percentModifier.Effects.Single().AllowedTargets);
    }

    private static void Apply(PlayerStats stats, Modifier modifier)
    {
        foreach (var effect in modifier.CreateAppliedEffectsFromFixedTargets())
            stats.AddModifier(effect);
    }
}
