using Godot;

namespace ARPG;

/// <summary>
/// A glowing orb that grants a modifier when the player walks over it.
/// Created dynamically by GameManager when an enemy dies.
/// </summary>
public partial class LootPickup : Area3D
{
    private Modifier _modifier;
    private Label3D _label;

    [Signal]
    public delegate void PickedUpEventHandler(string description);

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
        _label = new Label3D();
        _label.Text = modifier.Description;
        _label.FontSize = 24;
        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.NoDepthTest = true;
        _label.FixedSize = true;
        _label.PixelSize = 0.008f;
        _label.Modulate = Palette.Accent;
        _label.OutlineSize = 6;
        _label.OutlineModulate = new Color(0, 0, 0);
        _label.Position = new Vector3(0, 0.8f, 0);
        AddChild(_label);

        // Collision trigger
        var shape = new CollisionShape3D();
        shape.Shape = new SphereShape3D { Radius = 1.0f };
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
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerController player)
        {
            player.Stats.AddModifier(_modifier);
            EmitSignal(SignalName.PickedUp, _modifier.Description);
            QueueFree();
        }
    }
}
