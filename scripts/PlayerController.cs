using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D
{
    public PlayerStats Stats { get; private set; } = new();
    public Ability Ability { get; private set; }

    public int Hp { get => Stats.CurrentHp; set => Stats.CurrentHp = value; }
    public int AttackDamage => Stats.AttackDamage;

    private float _regenAccumulator;

    public override void _Ready()
    {
        // Apply selected archetype
        ArchetypeData.ApplyTo(GameState.SelectedArchetype, Stats);
        Ability = Ability.ForArchetype(GameState.SelectedArchetype);

        // Replace the primitive mesh with a sprite billboard
        var mesh = GetNode<MeshInstance3D>("PlayerMesh");
        mesh.Visible = false;

        var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreatePlayerTexture());
        sprite.Position = new Vector3(0, 0.5f, 0);
        AddChild(sprite);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Vector3.Zero;
        input.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        input.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");

        if (input.LengthSquared() > 0)
            input = input.Normalized();

        float speed = Stats.MoveSpeed;
        if (Input.IsKeyPressed(Key.Shift))
            speed *= Stats.SprintMultiplier;

        Velocity = input * speed;
        MoveAndSlide();
    }

    public void TickRegen(float delta)
    {
        if (Stats.CurrentHp >= Stats.MaxHp) return;

        _regenAccumulator += Stats.HpRegenRate * delta;
        if (_regenAccumulator >= 1.0f)
        {
            int heal = (int)_regenAccumulator;
            Stats.CurrentHp = Godot.Mathf.Min(Stats.CurrentHp + heal, Stats.MaxHp);
            _regenAccumulator -= heal;
        }
    }
}
