using Xunit;

namespace ARPG.Tests;

public partial class ForwardDoubleTapDetectorTests
{
    [Fact]
    public void DoubleTapActivatesBurstOnlyWhileForwardHeld()
    {
        var detector = new ForwardDoubleTapDetector(0.30);

        detector.Update(forwardJustPressed: true, forwardHeld: true, elapsedSeconds: 0.00);
        Assert.False(detector.IsBurstActive(true));

        detector.Update(forwardJustPressed: false, forwardHeld: false, elapsedSeconds: 0.08);
        detector.Update(forwardJustPressed: true, forwardHeld: true, elapsedSeconds: 0.22);
        Assert.True(detector.IsBurstActive(true));

        detector.Update(forwardJustPressed: false, forwardHeld: false, elapsedSeconds: 0.30);
        Assert.False(detector.IsBurstActive(false));
    }

    [Fact]
    public void TapOutsideWindowDoesNotActivateBurst()
    {
        var detector = new ForwardDoubleTapDetector(0.20);

        detector.Update(forwardJustPressed: true, forwardHeld: true, elapsedSeconds: 0.00);
        detector.Update(forwardJustPressed: false, forwardHeld: false, elapsedSeconds: 0.10);
        detector.Update(forwardJustPressed: true, forwardHeld: true, elapsedSeconds: 0.35);

        Assert.False(detector.IsBurstActive(true));
    }
}
