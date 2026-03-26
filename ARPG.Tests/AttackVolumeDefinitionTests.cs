using Godot;
using Xunit;

namespace ARPG.Tests;

public partial class AttackVolumeDefinitionTests
{
    [Fact]
    public void ResolveSizeScalesReachAndAttackSizeIndependently()
    {
        var volume = AttackVolumeDefinition.CreateBox(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(0.0f, 0.0f, 0.0f));

        Vector3 resolved = volume.ResolveSize(attackReach: 1.5f, attackSize: 2.0f);

        Assert.Equal(2.0f, resolved.X, 3);
        Assert.Equal(4.0f, resolved.Y, 3);
        Assert.Equal(4.5f, resolved.Z, 3);
    }

    [Fact]
    public void BuildTransformRotatesForwardOffsetIntoAimDirection()
    {
        var volume = AttackVolumeDefinition.CreateBox(
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(0.0f, 0.0f, 0.75f));

        Transform3D transform = volume.BuildTransform(
            new Vector3(10.0f, 1.0f, 20.0f),
            Vector3.Right,
            attackReach: 2.0f,
            attackSize: 1.0f);

        Assert.Equal(11.5f, transform.Origin.X, 3);
        Assert.Equal(1.0f, transform.Origin.Y, 3);
        Assert.Equal(20.0f, transform.Origin.Z, 3);
    }
}
