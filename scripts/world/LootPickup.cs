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

        // Apply runtime properties to scene nodes
        var mesh = GetNode<MeshInstance3D>("OrbMesh");
        var sphere = (SphereMesh)mesh.Mesh;
        sphere.Material = new StandardMaterial3D
        {
            AlbedoColor = Palette.Accent,
            EmissionEnabled = true,
            Emission = Palette.Accent,
            EmissionEnergyMultiplier = 2.0f,
        };

        _nameLabel = GetNode<Label3D>("NameLabel");
        _nameLabel.Text = modifier.Description;
        _nameLabel.Modulate = Palette.Accent;
        _nameLabel.OutlineModulate = Palette.OutlineBlack;

        _promptLabel = GetNode<Label3D>("PromptLabel");
        _promptLabel.Text = $"({GameKeys.DisplayName(GameKeys.Attack)}) Equip  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Backpack";
        _promptLabel.Modulate = Palette.TextLight;
        _promptLabel.OutlineModulate = Palette.OutlineBlack;

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
