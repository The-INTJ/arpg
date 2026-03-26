using Xunit;

namespace ARPG.Tests;

public partial class AttackWindupStateTests
{
    [Fact]
    public void LeavesRangeBeforeCommitWindowCancelsTheAttack()
    {
        var windup = new AttackWindupState(durationSeconds: 0.45f, commitWindowSeconds: 0.2f);
        windup.Start();

        Assert.False(windup.Advance(0.10f, targetStillInRange: true, out bool cancelledEarly));
        Assert.False(cancelledEarly);
        Assert.False(windup.IsCommitted);

        Assert.False(windup.Advance(0.05f, targetStillInRange: false, out bool cancelled));
        Assert.True(cancelled);
        Assert.False(windup.IsActive);
    }

    [Fact]
    public void LeavesRangeInsideCommitWindowStillFinishesTheSwing()
    {
        var windup = new AttackWindupState(durationSeconds: 0.45f, commitWindowSeconds: 0.2f);
        windup.Start();

        Assert.False(windup.Advance(0.30f, targetStillInRange: true, out bool cancelledEarly));
        Assert.False(cancelledEarly);
        Assert.True(windup.IsCommitted);

        Assert.True(windup.Advance(0.20f, targetStillInRange: false, out bool cancelled));
        Assert.False(cancelled);
        Assert.False(windup.IsActive);
    }
}
