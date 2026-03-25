using Godot;

namespace ARPG;

public partial class CavePocketSlice : Node3D
{
    private uint _seed;

    public override void _Ready()
    {
        _seed = GD.Randi();

        ApplyMaterial("CaveFloor/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Cave));
        ApplyMaterial("Shelf/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Mid));
        ApplyMaterial("Ramp/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Ramp));
        ApplyMaterial("Ceiling/Mesh", WorldMaterials.GetCaveRoofMaterial());
        RegisterCameraBlockers();

        var lamp = GetNode<OmniLight3D>("Lamp");
        lamp.LightColor = Palette.CaveLampGlow;
        lamp.OmniRange = 7.5f;
        lamp.LightEnergy = 1.7f;
        lamp.ShadowEnabled = false;

        AddStalactites();
        AddStalagmites();
        AddFloorRocks();
    }

    private void AddStalactites()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = _seed ^ 0xCAFE;
        var rockMat = WorldMaterials.GetCaveRockMaterial();
        float ceilingY = 2.55f;

        for (int i = 0; i < 12; i++)
        {
            float x = rng.RandfRange(-2.0f, 14.0f);
            float z = rng.RandfRange(-10.0f, 10.0f);
            float height = rng.RandfRange(0.4f, 1.4f);
            float radius = rng.RandfRange(0.15f, 0.4f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh
            {
                TopRadius = 0.0f,
                BottomRadius = radius,
                Height = height,
            };
            mesh.MaterialOverride = rockMat;
            mesh.Position = new Vector3(x, ceilingY - height * 0.5f, z);
            mesh.Rotation = new Vector3(Mathf.Pi, 0, 0);
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }
    }

    private void AddStalagmites()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = _seed ^ 0xBEEF;
        var rockMat = WorldMaterials.GetCaveRockMaterial();
        float floorY = -0.47f + 0.5f;

        for (int i = 0; i < 8; i++)
        {
            float x = rng.RandfRange(-6.0f, 3.0f);
            float z = rng.RandfRange(-9.0f, 9.0f);
            float height = rng.RandfRange(0.3f, 0.9f);
            float radius = rng.RandfRange(0.12f, 0.35f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh
            {
                TopRadius = 0.0f,
                BottomRadius = radius,
                Height = height,
            };
            mesh.MaterialOverride = rockMat;
            mesh.Position = new Vector3(x, floorY + height * 0.5f, z);
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }
    }

    private void AddFloorRocks()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = _seed ^ 0xF00D;
        var rockMat = WorldMaterials.GetCaveRockMaterial();
        float floorY = 0.03f;

        for (int i = 0; i < 6; i++)
        {
            float x = rng.RandfRange(-5.0f, 6.0f);
            float z = rng.RandfRange(-8.0f, 8.0f);
            float radius = rng.RandfRange(0.2f, 0.5f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new SphereMesh
            {
                Radius = radius,
                Height = radius * rng.RandfRange(0.6f, 1.2f),
            };
            mesh.MaterialOverride = rockMat;
            mesh.Position = new Vector3(x, floorY + radius * 0.3f, z);
            mesh.Scale = new Vector3(
                rng.RandfRange(0.8f, 1.4f),
                rng.RandfRange(0.4f, 0.8f),
                rng.RandfRange(0.8f, 1.3f));
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }
    }

    private void ApplyMaterial(string meshPath, Material material)
    {
        GetNode<MeshInstance3D>(meshPath).MaterialOverride = material;
    }

    private void RegisterCameraBlockers()
    {
        foreach (string bodyPath in new[] { "CaveFloor", "Shelf", "Ramp", "Ceiling" })
            GetNode<StaticBody3D>(bodyPath).AddToGroup(WorldGroups.CameraBlockers);
    }
}
