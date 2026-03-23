using Godot;

namespace ARPG;

public partial class ItemPickup : Area3D
{
    private InventoryItem _item;
    private Label3D _nameLabel;
    private Label3D _promptLabel;
    private PlayerController _nearbyPlayer;
    private bool _inventoryFullShown;

    [Signal]
    public delegate void CollectedEventHandler(string itemName, int slotIndex);

    [Signal]
    public delegate void InventoryFullEventHandler(string itemName);

    public void Init(InventoryItem item)
    {
        _item = item;

        var mesh = new MeshInstance3D();
        var sphere = new SphereMesh { Radius = 0.18f, Height = 0.36f };
        sphere.Material = new StandardMaterial3D
        {
            AlbedoColor = item.DisplayColor,
            EmissionEnabled = true,
            Emission = item.DisplayColor,
            EmissionEnergyMultiplier = 2.0f,
        };
        mesh.Mesh = sphere;
        mesh.Position = Vector3.Up * 0.3f;
        AddChild(mesh);

        _nameLabel = new Label3D();
        _nameLabel.Text = item.Name;
        _nameLabel.FontSize = 14;
        _nameLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _nameLabel.NoDepthTest = true;
        _nameLabel.FixedSize = true;
        _nameLabel.PixelSize = 0.004f;
        _nameLabel.Modulate = item.DisplayColor;
        _nameLabel.OutlineSize = 4;
        _nameLabel.OutlineModulate = Palette.BgDark;
        _nameLabel.Position = new Vector3(0, 0.72f, 0);
        AddChild(_nameLabel);

        _promptLabel = new Label3D();
        _promptLabel.Text = item.Description;
        _promptLabel.FontSize = 12;
        _promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _promptLabel.NoDepthTest = true;
        _promptLabel.FixedSize = true;
        _promptLabel.PixelSize = 0.004f;
        _promptLabel.Modulate = Palette.TextLight;
        _promptLabel.OutlineSize = 3;
        _promptLabel.OutlineModulate = Palette.BgDark;
        _promptLabel.Position = new Vector3(0, 1.0f, 0);
        _promptLabel.Visible = false;
        AddChild(_promptLabel);

        var shape = new CollisionShape3D();
        shape.Shape = new SphereShape3D { Radius = 1.25f };
        AddChild(shape);

        var tween = CreateTween().SetLoops();
        tween.TweenProperty(mesh, "position:y", 0.48f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(mesh, "position:y", 0.3f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        Monitoring = true;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (_nearbyPlayer != null && _inventoryFullShown)
            TryCollect();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player)
            return;

        _nearbyPlayer = player;
        TryCollect();
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _nearbyPlayer = null;
            _inventoryFullShown = false;
            _promptLabel.Visible = false;
        }
    }

    private void TryCollect()
    {
        if (_nearbyPlayer == null)
            return;

        if (_nearbyPlayer.Stats.Inventory.TryAdd(_item, out int slotIndex))
        {
            EmitSignal(SignalName.Collected, _item.Name, slotIndex);
            QueueFree();
            return;
        }

        _promptLabel.Text = "Inventory Full";
        _promptLabel.Visible = true;
        if (!_inventoryFullShown)
            EmitSignal(SignalName.InventoryFull, _item.Name);

        _inventoryFullShown = true;
    }
}
