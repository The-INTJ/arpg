using Godot;

namespace ARPG;

public partial class TreeSlice : StaticBody3D
{
    private static StandardMaterial3D _trunkMaterial;
    private static StandardMaterial3D _pineMaterial;
    private static StandardMaterial3D _roundCanopyMaterial;

    public override void _Ready()
    {
        GetNode<MeshInstance3D>("Trunk").MaterialOverride = GetTrunkMaterial();
        GetNode<MeshInstance3D>("Canopy").MaterialOverride = Name.ToString().Contains("Pine")
            ? GetPineMaterial()
            : GetRoundCanopyMaterial();
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
}
