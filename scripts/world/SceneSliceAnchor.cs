using Godot;

namespace ARPG;

public partial class SceneSliceAnchor : Marker3D
{
    [Export]
    public SceneSliceAnchorKind Kind = SceneSliceAnchorKind.EnemySpawn;

    public override void _EnterTree()
    {
        if (Kind != SceneSliceAnchorKind.EnemySpawn)
            return;

        string nodeName = Name.ToString();
        if (nodeName.Contains("Chest"))
            Kind = SceneSliceAnchorKind.CaveChest;
        else if (nodeName.Contains("Fallback"))
            Kind = SceneSliceAnchorKind.FallbackItem;
    }
}
