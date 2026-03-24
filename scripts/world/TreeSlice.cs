using Godot;

namespace ARPG;

public partial class TreeSlice : StaticBody3D
{
    private static StandardMaterial3D _trunkMaterial;
    private static StandardMaterial3D _pineMaterial;
    private static StandardMaterial3D _roundCanopyMaterial;
    private static StandardMaterial3D _rootMaterial;

    public override void _Ready()
    {
        bool isPine = Name.ToString().Contains("Pine");
        AddToGroup(WorldGroups.CameraBlockers);

        GetNode<MeshInstance3D>("Trunk").MaterialOverride = GetTrunkMaterial();
        GetNode<MeshInstance3D>("Canopy").MaterialOverride = isPine
            ? GetPineMaterial()
            : GetRoundCanopyMaterial();

        AddRoots(isPine);
        if (isPine)
            AddPineBranches();
        else
            AddCanopyLayers();
    }

    private void AddRoots(bool isPine)
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = GD.Randi();
        var mat = GetRootMaterial();
        int rootCount = isPine ? 3 : 4;

        for (int i = 0; i < rootCount; i++)
        {
            float angle = (Mathf.Tau / rootCount) * i + rng.RandfRange(-0.3f, 0.3f);
            float dist = rng.RandfRange(0.18f, 0.35f);
            float length = rng.RandfRange(0.5f, 0.9f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh
            {
                TopRadius = 0.04f,
                BottomRadius = rng.RandfRange(0.08f, 0.14f),
                Height = length,
            };
            mesh.MaterialOverride = mat;
            float x = Mathf.Cos(angle) * dist;
            float z = Mathf.Sin(angle) * dist;
            mesh.Position = new Vector3(x, length * 0.25f, z);
            // Tilt root outward
            mesh.Rotation = new Vector3(
                Mathf.Sin(angle) * 0.6f,
                angle,
                -Mathf.Cos(angle) * 0.6f);
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }

        // Root base bulge
        var bulge = new MeshInstance3D();
        bulge.Mesh = new SphereMesh
        {
            Radius = isPine ? 0.32f : 0.38f,
            Height = 0.35f,
        };
        bulge.MaterialOverride = mat;
        bulge.Position = new Vector3(0, 0.1f, 0);
        bulge.Scale = new Vector3(1.0f, 0.5f, 1.0f);
        bulge.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(bulge);
    }

    private void AddPineBranches()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = GD.Randi();
        var mat = GetPineMaterial();

        // Add smaller secondary cone layers at varying heights
        float[] layerHeights = { 1.8f, 2.4f };
        float[] layerRadii = { 0.7f, 0.45f };

        for (int i = 0; i < layerHeights.Length; i++)
        {
            float offsetAngle = rng.RandfRange(0, Mathf.Tau);
            float offsetDist = rng.RandfRange(0.05f, 0.15f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new CylinderMesh
            {
                TopRadius = 0.0f,
                BottomRadius = layerRadii[i],
                Height = 1.0f + rng.RandfRange(-0.1f, 0.2f),
            };
            mesh.MaterialOverride = mat;
            mesh.Position = new Vector3(
                Mathf.Cos(offsetAngle) * offsetDist,
                layerHeights[i],
                Mathf.Sin(offsetAngle) * offsetDist);
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }
    }

    private void AddCanopyLayers()
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = GD.Randi();
        var mat = GetRoundCanopyMaterial();

        // Add 2-3 smaller sphere clusters around the main canopy
        int clusterCount = rng.RandiRange(2, 3);
        for (int i = 0; i < clusterCount; i++)
        {
            float angle = (Mathf.Tau / clusterCount) * i + rng.RandfRange(-0.4f, 0.4f);
            float dist = rng.RandfRange(0.4f, 0.7f);
            float radius = rng.RandfRange(0.45f, 0.7f);

            var mesh = new MeshInstance3D();
            mesh.Mesh = new SphereMesh
            {
                Radius = radius,
                Height = radius * rng.RandfRange(1.6f, 2.0f),
            };
            mesh.MaterialOverride = mat;
            mesh.Position = new Vector3(
                Mathf.Cos(angle) * dist,
                2.55f + rng.RandfRange(-0.3f, 0.3f),
                Mathf.Sin(angle) * dist);
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            AddChild(mesh);
        }
    }

    private static StandardMaterial3D GetTrunkMaterial()
    {
        return _trunkMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.TreeTrunk,
            Roughness = 0.92f,
        };
    }

    private static StandardMaterial3D GetPineMaterial()
    {
        return _pineMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.TreePine,
            Roughness = 0.86f,
        };
    }

    private static StandardMaterial3D GetRoundCanopyMaterial()
    {
        return _roundCanopyMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.TreeCanopy,
            Roughness = 0.84f,
        };
    }

    private static StandardMaterial3D GetRootMaterial()
    {
        return _rootMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.TreeTrunk.Darkened(0.15f),
            Roughness = 0.95f,
        };
    }
}
