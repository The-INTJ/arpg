using Godot;

namespace ARPG;

[Tool]
public partial class RockWallSlice : StaticBody3D
{
    private bool _useCaveMaterial = true;
    private Vector3 _collisionSize = new(4.0f, 3.0f, 0.8f);

    [Export]
    public bool UseCaveMaterial
    {
        get => _useCaveMaterial;
        set
        {
            _useCaveMaterial = value;
            if (IsInsideTree())
                ApplyConfiguredState();
        }
    }

    [Export]
    public Vector3 CollisionSize
    {
        get => _collisionSize;
        set
        {
            _collisionSize = value;
            if (IsInsideTree())
                ApplyConfiguredState();
        }
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint() && !IsInGroup(WorldGroups.CameraBlockers))
            AddToGroup(WorldGroups.CameraBlockers);

        ApplyConfiguredState();
    }

    private void ApplyConfiguredState()
    {
        ConfigureCollision();
        ApplyMaterialOverrides();
    }

    private void ConfigureCollision()
    {
        var collision = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        if (collision == null)
            return;

        collision.Position = new Vector3(0, _collisionSize.Y * 0.5f, 0);

        if (collision.Shape is BoxShape3D box)
            box.Size = _collisionSize;
    }

    private void ApplyMaterialOverrides()
    {
        var visualRoot = GetNodeOrNull<Node3D>("VisualRoot");
        if (visualRoot == null)
            return;

        Material material = _useCaveMaterial
            ? WorldMaterials.GetCaveRockMaterial()
            : WorldMaterials.GetRockMaterial();

        ApplyMaterialRecursive(visualRoot, material);
    }

    private static void ApplyMaterialRecursive(Node node, Material material)
    {
        if (node is MeshInstance3D mesh)
            mesh.MaterialOverride = material;

        foreach (Node child in node.GetChildren())
            ApplyMaterialRecursive(child, material);
    }
}
