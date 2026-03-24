using Godot;

namespace ARPG;

public partial class PlatformRampSlice : Node3D
{
    private const float FloorThickness = 1.0f;
    private const float RampThickness = 0.6f;

    private Vector2 _platformSize = new(14.0f, 20.0f);
    private float _platformTopHeight = 1.15f;
    private float _platformOffsetX = 6.0f;
    private float _rampWidth = 10.0f;
    private float _rampRun = 12.0f;
    private float _rampLowTop;
    private float _rampHighTop = 1.15f;
    private WorldSurfaceKind _platformSurfaceKind = WorldSurfaceKind.Mid;

    public override void _Ready()
    {
        ApplyGeometry();
    }

    public void Configure(
        Vector2 platformSize,
        float platformTopHeight,
        float platformOffsetX,
        float rampWidth,
        float rampRun,
        float rampLowTop,
        float rampHighTop,
        WorldSurfaceKind platformSurfaceKind)
    {
        _platformSize = platformSize;
        _platformTopHeight = platformTopHeight;
        _platformOffsetX = platformOffsetX;
        _rampWidth = rampWidth;
        _rampRun = rampRun;
        _rampLowTop = rampLowTop;
        _rampHighTop = rampHighTop;
        _platformSurfaceKind = platformSurfaceKind;

        if (IsInsideTree())
            ApplyGeometry();
    }

    private void ApplyGeometry()
    {
        ConfigurePlatform();
        ConfigureRamp();
    }

    private void ConfigurePlatform()
    {
        var platform = GetNode<StaticBody3D>("Platform");
        platform.Position = new Vector3(_platformOffsetX, _platformTopHeight - FloorThickness / 2.0f, 0);

        var mesh = GetNode<MeshInstance3D>("Platform/Mesh");
        mesh.MaterialOverride = WorldMaterials.GetSurfaceMaterial(_platformSurfaceKind);
        ((BoxMesh)mesh.Mesh).Size = new Vector3(_platformSize.X, FloorThickness, _platformSize.Y);

        var collision = GetNode<CollisionShape3D>("Platform/Collision");
        ((BoxShape3D)collision.Shape).Size = new Vector3(_platformSize.X, FloorThickness, _platformSize.Y);
    }

    private void ConfigureRamp()
    {
        float heightDelta = Mathf.Abs(_rampHighTop - _rampLowTop);
        float angle = Mathf.Atan2(heightDelta, _rampRun);
        float slopedLength = Mathf.Sqrt(_rampRun * _rampRun + heightDelta * heightDelta);
        float averageTop = (_rampLowTop + _rampHighTop) * 0.5f;
        float centerY = averageTop - RampThickness * 0.5f * Mathf.Cos(angle);
        var rampSize = new Vector3(slopedLength, RampThickness, _rampWidth);

        var ramp = GetNode<StaticBody3D>("Ramp");
        ramp.Position = new Vector3(0, centerY, 0);
        ramp.Rotation = new Vector3(0, 0, angle);

        var mesh = GetNode<MeshInstance3D>("Ramp/Mesh");
        mesh.MaterialOverride = WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Ramp);
        ((BoxMesh)mesh.Mesh).Size = rampSize;

        var collision = GetNode<CollisionShape3D>("Ramp/Collision");
        ((BoxShape3D)collision.Shape).Size = rampSize;
    }
}
