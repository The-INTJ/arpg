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
    private CameraController _cameraController;
    private Sprite3D _sprite;

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

        // Replace the primitive mesh with a sprite billboard (smaller for zoomed-in camera)
        var mesh = GetNode<MeshInstance3D>("PlayerMesh");
        mesh.Visible = false;

        _sprite = SpriteFactory.CreateSprite(SpriteFactory.CreatePlayerTexture(), 0.05f);
        _sprite.Position = new Vector3(0, 0.25f, 0);
        AddChild(_sprite);

        _cameraController = GetNode<CameraController>("CameraRig");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Gather raw input
        var raw = Vector3.Zero;
        raw.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        raw.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");

        if (raw.LengthSquared() > 0)
            raw = raw.Normalized();

        // Transform input direction by camera yaw so WASD is camera-relative
        float yaw = _cameraController.Yaw;
        var input = new Basis(Vector3.Up, yaw) * raw;

        float speed = Stats.MoveSpeed;
        if (Input.IsKeyPressed(Key.Shift))
            speed *= Stats.SprintMultiplier;

        Vector3 targetVelocity = input * speed;
        float rampTime = targetVelocity.LengthSquared() > 0 ? MoveRampUpTime : MoveRampDownTime;
        float maxDelta = speed / rampTime * (float)delta;
        Velocity = Velocity.MoveToward(targetVelocity, maxDelta);
        MoveAndSlide();

        // Flip sprite based on camera-relative horizontal movement
        if (Velocity.LengthSquared() > 0.01f)
        {
            var cameraRight = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
            _sprite.FlipH = Velocity.Dot(cameraRight) < 0;
        }
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
