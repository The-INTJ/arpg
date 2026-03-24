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
    private uint _seed;

    public override void _Ready()
    {
        _seed = GD.Randi();
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
        AddPlatformEdgeRocks();
        AddRampEdgeBoulders();
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

    private void AddPlatformEdgeRocks()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = _seed ^ 0xED6E;
        var rockMat = WorldMaterials.GetRockMaterial();

        float halfW = _platformSize.X * 0.5f;
        float halfD = _platformSize.Y * 0.5f;
        float topY = _platformTopHeight;

        // Rocks along the platform edges (not the ramp-facing side)
        // Back edge (positive X away from ramp)
        for (int i = 0; i < 4; i++)
        {
            float z = rng.RandfRange(-halfD + 1.0f, halfD - 1.0f);
            float radius = rng.RandfRange(0.3f, 0.7f);
            AddRock(rockMat, rng,
                new Vector3(_platformOffsetX + halfW - rng.RandfRange(0, 0.5f), topY, z),
                radius);
        }

        // Side edges
        foreach (float edgeZ in new[] { -halfD, halfD })
        {
            float sign = edgeZ > 0 ? 1.0f : -1.0f;
            for (int i = 0; i < 3; i++)
            {
                float x = rng.RandfRange(_platformOffsetX - halfW + 2.0f, _platformOffsetX + halfW - 1.0f);
                float radius = rng.RandfRange(0.25f, 0.55f);
                AddRock(rockMat, rng,
                    new Vector3(x, topY, edgeZ * sign + sign * rng.RandfRange(-0.3f, 0.3f)),
                    radius);
            }
        }
    }

    private void AddRampEdgeBoulders()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = _seed ^ 0xB01D;
        var rockMat = WorldMaterials.GetRockMaterial();

        float heightDelta = Mathf.Abs(_rampHighTop - _rampLowTop);
        float halfRampZ = _rampWidth * 0.5f;

        // Place boulders along both sides of the ramp
        for (int side = -1; side <= 1; side += 2)
        {
            float edgeZ = side * halfRampZ;
            for (int i = 0; i < 3; i++)
            {
                float t = rng.RandfRange(0.15f, 0.85f);
                float x = t * _rampRun;
                float y = Mathf.Lerp(_rampLowTop, _rampHighTop, t);
                float radius = rng.RandfRange(0.2f, 0.5f);
                AddRock(rockMat, rng,
                    new Vector3(x - _rampRun * 0.5f, y, edgeZ + side * rng.RandfRange(0, 0.4f)),
                    radius);
            }
        }

        // A couple rocks at the base of the ramp
        for (int i = 0; i < 2; i++)
        {
            float z = rng.RandfRange(-halfRampZ + 1.0f, halfRampZ - 1.0f);
            float radius = rng.RandfRange(0.2f, 0.4f);
            AddRock(rockMat, rng,
                new Vector3(-_rampRun * 0.5f - rng.RandfRange(0, 1.0f), _rampLowTop, z),
                radius);
        }
    }

    private void AddRock(Material mat, RandomNumberGenerator rng, Vector3 position, float radius)
    {
        var mesh = new MeshInstance3D();
        mesh.Mesh = new SphereMesh
        {
            Radius = radius,
            Height = radius * rng.RandfRange(0.8f, 1.4f),
        };
        mesh.MaterialOverride = mat;
        mesh.Position = position;
        mesh.Scale = new Vector3(
            rng.RandfRange(0.7f, 1.3f),
            rng.RandfRange(0.5f, 0.9f),
            rng.RandfRange(0.7f, 1.3f));
        mesh.Rotation = new Vector3(
            rng.RandfRange(-0.3f, 0.3f),
            rng.RandfRange(0, Mathf.Tau),
            rng.RandfRange(-0.3f, 0.3f));
        mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(mesh);
    }
}
