using Godot;

namespace ARPG;

public partial class CaveChest : Area3D
{
    private InventoryItem _item;
    private PlayerController _nearbyPlayer;
    private Sprite3D _chestSprite;
    private Label3D _nameLabel;
    private Label3D _promptLabel;
    private bool _opened;

    [Signal]
    public delegate void OpenedEventHandler(string itemName);

    public void Init(InventoryItem item)
    {
        _item = item;
        _chestSprite = GetNode<Sprite3D>("VisualRoot/ChestSprite");
        _chestSprite.Texture = SpriteFactory.CreateChestTexture(opened: false);

        _nameLabel = GetNode<Label3D>("NameLabel");
        _nameLabel.Text = "Cave Cache";
        _nameLabel.Modulate = Palette.TextLight;
        _nameLabel.OutlineModulate = Palette.BgDark;
        _nameLabel.Visible = false;

        _promptLabel = GetNode<Label3D>("PromptLabel");
        _promptLabel.Text = item.Name;
        _promptLabel.Modulate = item.DisplayColor;
        _promptLabel.OutlineModulate = Palette.BgDark;
        _promptLabel.Visible = false;

        var tween = CreateTween().SetLoops();
        tween.TweenProperty(GetNode<Node3D>("VisualRoot"), "position:y", 0.42f, 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(GetNode<Node3D>("VisualRoot"), "position:y", 0.30f, 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        Monitoring = true;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (_opened || _nearbyPlayer == null || !_nearbyPlayer.IsGrounded)
            return;

        OpenChest();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_opened || body is not PlayerController player)
            return;

        _nearbyPlayer = player;
        _nameLabel.Visible = true;
        _promptLabel.Visible = true;
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not PlayerController)
            return;

        _nearbyPlayer = null;
        _nameLabel.Visible = false;
        _promptLabel.Visible = false;
    }

    private void OpenChest()
    {
        if (_opened || _item == null)
            return;

        _opened = true;
        Monitoring = false;
        _chestSprite.Texture = SpriteFactory.CreateChestTexture(opened: true);
        _nameLabel.Visible = false;
        _promptLabel.Visible = false;

        var pickup = GD.Load<PackedScene>(Scenes.ItemPickup).Instantiate<ItemPickup>();
        pickup.GlobalPosition = GlobalPosition + new Vector3(0.7f, 0.1f, 0.25f);
        pickup.Init(_item);
        GetParent<Node3D>().AddChild(pickup);

        EmitSignal(SignalName.Opened, _item.Name);
    }
}
