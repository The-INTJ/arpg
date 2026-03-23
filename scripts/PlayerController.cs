using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D
{
    private const float MoveRampUpTime = 0.30f;
    private const float MoveRampDownTime = 0.15f;

    public PlayerStats Stats { get; private set; }
    public Ability Ability { get; private set; }

    public int Hp { get => Stats.CurrentHp; set => Stats.CurrentHp = value; }
    public int AttackDamage => Stats.AttackDamage;

    private float _regenAccumulator;

    public override void _Ready()
    {
        // Reuse persistent stats across rooms, or create fresh for room 1
        if (GameState.PersistentStats != null)
        {
            Stats = GameState.PersistentStats;
        }
        else
        {
            Stats = new PlayerStats();
            ArchetypeData.ApplyTo(GameState.SelectedArchetype, Stats);
            Stats.Weapon = Weapon.ForArchetype(GameState.SelectedArchetype);
            Stats.ResetHp();
            GameState.PersistentStats = Stats;
        }

        // Ability comes from the weapon
        Ability = Ability.ForWeapon(Stats.Weapon);

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

        Vector3 targetVelocity = input * speed;
        float rampTime = targetVelocity.LengthSquared() > 0 ? MoveRampUpTime : MoveRampDownTime;
        float maxDelta = speed / rampTime * (float)delta;
        Velocity = Velocity.MoveToward(targetVelocity, maxDelta);
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
