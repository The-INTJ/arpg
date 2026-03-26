using Godot;
using Xunit;

namespace ARPG.Tests;

public partial class PlayerTraversalFeelTests
{
    [Fact]
    public void StandingStartsAndReversalsUseMoreAggressiveGroundResponse()
    {
        float cruisingRate = PlayerTraversalFeel.GetHorizontalMoveRate(
            Vector3.Forward * 5.0f,
            Vector3.Forward * 5.0f,
            grounded: true);
        float standingStartRate = PlayerTraversalFeel.GetHorizontalMoveRate(
            Vector3.Zero,
            Vector3.Forward * 5.0f,
            grounded: true);
        float reversalRate = PlayerTraversalFeel.GetHorizontalMoveRate(
            Vector3.Forward * 5.0f,
            Vector3.Back * 5.0f,
            grounded: true);

        Assert.True(standingStartRate > cruisingRate);
        Assert.True(reversalRate > standingStartRate);
    }

    [Fact]
    public void AirControlUsesLowerResponseThanGroundedMovement()
    {
        float groundRate = PlayerTraversalFeel.GetHorizontalMoveRate(
            Vector3.Zero,
            Vector3.Right * 5.0f,
            grounded: true);
        float airRate = PlayerTraversalFeel.GetHorizontalMoveRate(
            Vector3.Zero,
            Vector3.Right * 5.0f,
            grounded: false);

        Assert.True(airRate < groundRate);
    }

    [Fact]
    public void GravitySoftensNearApexAndCutsHarderWhenJumpReleased()
    {
        float risingGravity = PlayerTraversalFeel.GetVerticalGravity(4.0f, jumpHeld: true);
        float apexGravity = PlayerTraversalFeel.GetVerticalGravity(0.5f, jumpHeld: true);
        float jumpCutGravity = PlayerTraversalFeel.GetVerticalGravity(4.0f, jumpHeld: false);
        float fallingGravity = PlayerTraversalFeel.GetVerticalGravity(-2.0f, jumpHeld: false);

        Assert.True(apexGravity < risingGravity);
        Assert.True(jumpCutGravity > risingGravity);
        Assert.True(fallingGravity > apexGravity);
    }
}
