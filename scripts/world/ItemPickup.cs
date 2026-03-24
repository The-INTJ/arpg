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

        // Apply runtime properties to scene nodes
        var mesh = GetNode<MeshInstance3D>("OrbMesh");
        var sphere = (SphereMesh)mesh.Mesh;
        sphere.Material = new StandardMaterial3D
        {
            AlbedoColor = item.DisplayColor,
            EmissionEnabled = true,
            Emission = item.DisplayColor,
            EmissionEnergyMultiplier = 2.0f,
        };

        _nameLabel = GetNode<Label3D>("NameLabel");
        _nameLabel.Text = item.Name;
        _nameLabel.Modulate = item.DisplayColor;
        _nameLabel.OutlineModulate = Palette.BgDark;
        _nameLabel.Visible = false;

        _promptLabel = GetNode<Label3D>("PromptLabel");
        _promptLabel.Text = item.Description;
        _promptLabel.Modulate = Palette.TextLight;
        _promptLabel.OutlineModulate = Palette.BgDark;
        _promptLabel.Visible = false;

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
        _nameLabel.Visible = true;
        TryCollect();
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _nearbyPlayer = null;
            _inventoryFullShown = false;
            _nameLabel.Visible = false;
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
        _nameLabel.Visible = true;
        _promptLabel.Visible = true;
        if (!_inventoryFullShown)
            EmitSignal(SignalName.InventoryFull, _item.Name);

        _inventoryFullShown = true;
    }
}
