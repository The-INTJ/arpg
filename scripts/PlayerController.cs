using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 5.0f;

    public int Hp = 15;
    public int AttackDamage = 5;

    public override void _Ready()
    {
        var mesh = GetNode<MeshInstance3D>("PlayerMesh");
        mesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = Palette.PlayerBody };
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Vector3.Zero;
        input.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        input.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");

        if (input.LengthSquared() > 0)
            input = input.Normalized();

        Velocity = input * Speed;
        MoveAndSlide();
    }
}
