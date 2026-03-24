using Godot;

namespace ARPG;

public partial class CavePocketSlice : Node3D
{
    public override void _Ready()
    {
        ApplyMaterial("CaveFloor/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Cave));
        ApplyMaterial("Shelf/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Mid));
        ApplyMaterial("Ramp/Mesh", WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Ramp));
        ApplyMaterial("BackWall/Mesh", WorldMaterials.GetCaveRockMaterial());
        ApplyMaterial("NorthWall/Mesh", WorldMaterials.GetCaveRockMaterial());
        ApplyMaterial("SouthWall/Mesh", WorldMaterials.GetCaveRockMaterial());
        ApplyMaterial("Ceiling", WorldMaterials.GetCaveRoofMaterial());

        var lamp = GetNode<OmniLight3D>("Lamp");
        lamp.LightColor = Palette.CaveLampGlow;
        lamp.OmniRange = 7.5f;
        lamp.LightEnergy = 1.7f;
        lamp.ShadowEnabled = false;
    }

    private void ApplyMaterial(string meshPath, Material material)
    {
        GetNode<MeshInstance3D>(meshPath).MaterialOverride = material;
    }
}
