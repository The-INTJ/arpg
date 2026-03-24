using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D
{
    private const float MoveRampUpTime = 0.30f;
    private const float MoveRampDownTime = 0.15f;
    private static readonly Vector3 BobRootBasePosition = new(0, 0.25f, 0);

    public PlayerStats Stats { get; private set; }
    public Ability Ability { get; private set; }

    public int Hp { get => Stats.CurrentHp; set => Stats.CurrentHp = value; }
    public int AttackDamage => Stats.AttackDamage;

    private float _regenAccumulator;
    private float _presentationTime;
    private CameraController _cameraController;
    private Node3D _visualRoot;
    private Node3D _bobRoot;
    private Sprite3D _sprite;
    private Node3D _weaponPivot;
    private Sprite3D _weaponSprite;
    private Tween _visualTween;

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

        _visualRoot = new Node3D();
        _visualRoot.Name = "VisualRoot";
        AddChild(_visualRoot);

        _bobRoot = new Node3D();
        _bobRoot.Name = "BobRoot";
        _bobRoot.Position = BobRootBasePosition;
        _visualRoot.AddChild(_bobRoot);

        _sprite = SpriteFactory.CreateSprite(SpriteFactory.CreatePlayerTexture(GameState.SelectedArchetype), 0.05f);
        _bobRoot.AddChild(_sprite);

        _weaponPivot = new Node3D();
        _weaponPivot.Name = "WeaponPivot";
        _weaponPivot.Position = new Vector3(0.16f, -0.03f, 0.02f);
        _bobRoot.AddChild(_weaponPivot);

        _weaponSprite = SpriteFactory.CreateSprite(SpriteFactory.CreateWeaponTexture(GameState.SelectedArchetype), 0.03f);
        _weaponSprite.Modulate = new Color(1, 1, 1, 0.95f);
        _weaponPivot.AddChild(_weaponSprite);

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

        UpdatePresentation((float)delta);
    }

    public void TickRegen(float delta)
    {
        if (Stats.HpRegenRate <= 0.0f || Stats.CurrentHp >= Stats.MaxHp) return;

        _regenAccumulator += Stats.HpRegenRate * delta;
        if (_regenAccumulator >= 1.0f)
        {
            int heal = (int)_regenAccumulator;
            Stats.CurrentHp = Godot.Mathf.Min(Stats.CurrentHp + heal, Stats.MaxHp);
            _regenAccumulator -= heal;
        }
    }

    public void PlayAttackAnimation(Vector3 targetPosition, bool isHeavy)
    {
        float lungeDistance = isHeavy ? 0.24f : 0.16f;
        Vector3 attackDirection = (targetPosition - GlobalPosition).Normalized();
        Vector3 localLunge = ToLocal(GlobalPosition + attackDirection * lungeDistance);
        float swingDirection = _sprite.FlipH ? -1.0f : 1.0f;

        ResetVisualTweenState();
        _visualTween = CreateTween();
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(_visualRoot, "position", localLunge, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(_sprite, "scale", new Vector3(isHeavy ? 1.12f : 1.08f, isHeavy ? 1.12f : 1.08f, 1), 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(_weaponSprite, "rotation_degrees", new Vector3(0, 0, 75.0f * swingDirection), 0.08f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(_weaponSprite, "scale", new Vector3(isHeavy ? 1.22f : 1.16f, isHeavy ? 1.22f : 1.16f, 1), 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(false);
        _visualTween.TweenProperty(_visualRoot, "position", Vector3.Zero, 0.14f)
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(_sprite, "scale", Vector3.One, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        _visualTween.TweenProperty(_weaponSprite, "rotation_degrees", Vector3.Zero, 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);
        _visualTween.TweenProperty(_weaponSprite, "scale", Vector3.One, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    public void PlayHitReaction()
    {
        ResetVisualTweenState();
        _visualTween = CreateTween();
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(_visualRoot, "position", new Vector3(0, 0, 0.08f), 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(_sprite, "modulate", new Color(1.0f, 0.72f, 0.72f), 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(false);
        _visualTween.TweenProperty(_visualRoot, "position", Vector3.Zero, 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(_sprite, "modulate", Colors.White, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    private void UpdatePresentation(float delta)
    {
        _presentationTime += delta * (Velocity.LengthSquared() > 0.05f ? 8.0f : 3.2f);
        float bobAmount = Velocity.LengthSquared() > 0.05f ? 0.03f : 0.012f;
        float bobOffset = Mathf.Sin(_presentationTime) * bobAmount;
        _bobRoot.Position = _bobRoot.Position.Lerp(BobRootBasePosition + new Vector3(0, bobOffset, 0), 0.2f);

        float side = _sprite.FlipH ? -1.0f : 1.0f;
        Vector3 desiredWeaponPivot = new Vector3(0.16f * side, -0.03f + bobOffset * 0.25f, 0.02f);
        _weaponPivot.Position = _weaponPivot.Position.Lerp(desiredWeaponPivot, 0.18f);
        _weaponSprite.FlipH = _sprite.FlipH;
    }

    private void ResetVisualTweenState()
    {
        _visualTween?.Kill();
        _visualRoot.Position = Vector3.Zero;
        _sprite.Scale = Vector3.One;
        _sprite.Modulate = Colors.White;
        _weaponSprite.Scale = Vector3.One;
        _weaponSprite.RotationDegrees = Vector3.Zero;
    }
}
