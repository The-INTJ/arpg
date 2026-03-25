using Godot;

namespace ARPG;

public partial class RockWallSlice : StaticBody3D
{
    [Export]
    public bool UseCaveMaterial { get; set; }

    [Export]
    public Vector3 CollisionSize { get; set; } = new(4.0f, 3.0f, 0.8f);

    public override void _Ready()
    {
        AddToGroup(WorldGroups.CameraBlockers);
        ConfigureCollision();
        ApplyMaterialOverrides();
    }

    private void ConfigureCollision()
    {
        var collision = GetNode<CollisionShape3D>("CollisionShape3D");
        collision.Position = new Vector3(0, CollisionSize.Y * 0.5f, 0);

        if (collision.Shape is BoxShape3D box)
            box.Size = CollisionSize;
    }

    private void ApplyMaterialOverrides()
    {
        Material material = UseCaveMaterial
            ? WorldMaterials.GetCaveRockMaterial()
            : WorldMaterials.GetRockMaterial();

        ApplyMaterialRecursive(GetNode<Node3D>("VisualRoot"), material);
    }

    private static void ApplyMaterialRecursive(Node node, Material material)
    {
        if (node is MeshInstance3D mesh)
            mesh.MaterialOverride = material;

        foreach (Node child in node.GetChildren())
            ApplyMaterialRecursive(child, material);
    }
}
