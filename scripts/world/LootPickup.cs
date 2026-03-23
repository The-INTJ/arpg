using Godot;

namespace ARPG;

/// <summary>
/// A glowing orb that offers a modifier when the player walks near it.
/// (E) opens the modify stats screen, (Q) stashes it in the backpack.
/// </summary>
public partial class LootPickup : Area3D
{
    private Modifier _modifier;
    private Label3D _nameLabel;
    private Label3D _promptLabel;
    private bool _playerInRange;
    private PlayerController _nearbyPlayer;

    [Signal]
    public delegate void EquipRequestedEventHandler();

    [Signal]
    public delegate void StashedEventHandler(string description);

    public Modifier Modifier => _modifier;

    public void Init(Modifier modifier)
    {
        _modifier = modifier;

        // Glowing orb mesh
        var mesh = new MeshInstance3D();
        var sphere = new SphereMesh { Radius = 0.2f, Height = 0.4f };
        sphere.Material = new StandardMaterial3D
        {
            AlbedoColor = Palette.Accent,
            EmissionEnabled = true,
            Emission = Palette.Accent,
            EmissionEnergyMultiplier = 2.0f,
        };
        mesh.Mesh = sphere;
        mesh.Position = Vector3.Up * 0.3f;
        AddChild(mesh);

        // Floating label showing what the modifier does
        _nameLabel = new Label3D();
        _nameLabel.Text = modifier.Description;
        _nameLabel.FontSize = 14;
        _nameLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _nameLabel.NoDepthTest = true;
        _nameLabel.FixedSize = true;
        _nameLabel.PixelSize = 0.004f;
        _nameLabel.Modulate = Palette.Accent;
        _nameLabel.OutlineSize = 4;
        _nameLabel.OutlineModulate = Palette.OutlineBlack;
        _nameLabel.Position = new Vector3(0, 0.7f, 0);
        AddChild(_nameLabel);

        // Prompt label (hidden until player is near)
        _promptLabel = new Label3D();
        _promptLabel.Text = $"({GameKeys.DisplayName(GameKeys.Attack)}) Equip  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Backpack";
        _promptLabel.FontSize = 12;
        _promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _promptLabel.NoDepthTest = true;
        _promptLabel.FixedSize = true;
        _promptLabel.PixelSize = 0.004f;
        _promptLabel.Modulate = Palette.TextLight;
        _promptLabel.OutlineSize = 3;
        _promptLabel.OutlineModulate = Palette.OutlineBlack;
        _promptLabel.Position = new Vector3(0, 1.0f, 0);
        _promptLabel.Visible = false;
        AddChild(_promptLabel);

        // Collision trigger
        var shape = new CollisionShape3D();
        shape.Shape = new SphereShape3D { Radius = 1.5f };
        AddChild(shape);

        // Bobbing animation
        var tween = CreateTween().SetLoops();
        tween.TweenProperty(mesh, "position:y", 0.5f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(mesh, "position:y", 0.3f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);

        Monitoring = true;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_playerInRange || _nearbyPlayer == null) return;
        // Don't interact during combat or when paused
        if (GetTree().Paused) return;

        if (@event.IsActionPressed(GameKeys.Attack))
        {
            // Stash to backpack first, then open equip screen
            _nearbyPlayer.Stats.AddToBackpack(_modifier);
            EmitSignal(SignalName.EquipRequested);
            GetViewport().SetInputAsHandled();
            QueueFree();
        }
        else if (@event.IsActionPressed(GameKeys.Ability))
        {
            // Put in backpack silently
            _nearbyPlayer.Stats.AddToBackpack(_modifier);
            EmitSignal(SignalName.Stashed, _modifier.Description);
            GetViewport().SetInputAsHandled();
            QueueFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player)
        {
            _playerInRange = true;
            _nearbyPlayer = player;
            _promptLabel.Visible = true;
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is PlayerController)
        {
            _playerInRange = false;
            _nearbyPlayer = null;
            _promptLabel.Visible = false;
        }
    }
}
