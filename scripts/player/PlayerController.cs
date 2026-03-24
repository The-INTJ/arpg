using System;
using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D
{
    private const float GroundAcceleration = 28.0f;
    private const float GroundDeceleration = 34.0f;
    private const float AirAcceleration = 11.0f;
    private const float AirDeceleration = 8.0f;
    private const float GravityStrength = 18.0f;
    private const float JumpVelocity = 7.0f;
    private const float CoyoteTime = 0.12f;
    private const float VoidFallGracePeriod = 1.0f;
    private static readonly Vector3 BobRootBasePosition = new(0, 0.25f, 0);

    public PlayerStats Stats { get; private set; }
    public Ability Ability { get; private set; }

    public int Hp { get => Stats.CurrentHp; set => Stats.CurrentHp = value; }
    public int AttackDamage => Stats.AttackDamage;
    public bool IsGrounded => IsOnFloor();

    [Signal]
    public delegate void EdgeFallEventHandler();

    private float _voidFallTimer;
    private float _regenAccumulator;
    private float _presentationTime;
    private float _coyoteTimer;
    private float _landingImpact;
    private float _presentationLean;
    private float _weaponLagTilt;
    private bool _movementLocked;
    private bool _wasGrounded;
    private CameraController _cameraController;
    private Node3D _visualRoot;
    private Node3D _bobRoot;
    private Sprite3D _sprite;
    private Node3D _weaponPivot;
    private Sprite3D _weaponSprite;
    private Tween _visualTween;
    private Aabb[] _zoneBounds = Array.Empty<Aabb>();
    private Vector3 _lastGroundedPosition;
    private bool _hasLastGroundedPosition;

    public override void _Ready()
    {
        FloorSnapLength = 0.45f;
        _wasGrounded = true;
        _lastGroundedPosition = GlobalPosition;
        _hasLastGroundedPosition = true;

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
        float dt = (float)delta;

        // Gather raw input
        var raw = Vector3.Zero;
        if (!_movementLocked)
        {
            raw.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
            raw.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
        }

        if (raw.LengthSquared() > 0)
            raw = raw.Normalized();

        // Transform input direction by camera yaw so WASD is camera-relative
        float yaw = _cameraController.Yaw;
        var input = new Basis(Vector3.Up, yaw) * raw;

        float speed = Stats.MoveSpeed;
        if (!_movementLocked && Input.IsKeyPressed(Key.Shift))
            speed *= Stats.SprintMultiplier;

        bool grounded = IsOnFloor();
        _coyoteTimer = grounded ? CoyoteTime : Mathf.Max(0.0f, _coyoteTimer - dt);

        if (!_movementLocked && Input.IsActionJustPressed(GameKeys.Jump) && _coyoteTimer > 0.0f)
        {
            Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
            _coyoteTimer = 0.0f;
            grounded = false;
        }

        var horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        var targetHorizontalVelocity = input * speed * (grounded ? 1.0f : 0.58f);
        float moveRate = targetHorizontalVelocity.LengthSquared() > 0.0f
            ? (grounded ? GroundAcceleration : AirAcceleration)
            : (grounded ? GroundDeceleration : AirDeceleration);
        horizontalVelocity = horizontalVelocity.MoveToward(targetHorizontalVelocity, moveRate * dt);

        float verticalVelocityBeforeMove = Velocity.Y;

        if (!grounded)
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y - GravityStrength * dt, horizontalVelocity.Z);
        else if (Velocity.Y < 0.0f)
            Velocity = new Vector3(horizontalVelocity.X, -0.01f, horizontalVelocity.Z);
        else
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);

        MoveAndSlide();

        // Outside a zone is only dangerous if the player actually spends time falling.
        bool groundedNow = IsOnFloor();
        if (UpdateVoidFallRecovery(dt, groundedNow))
            groundedNow = true;

        // Flip sprite based on camera-relative horizontal movement
        var facingVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        if (facingVelocity.LengthSquared() > 0.01f)
        {
            var cameraRight = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
            _sprite.FlipH = facingVelocity.Dot(cameraRight) < 0;
        }

        if (!_wasGrounded && groundedNow)
            _landingImpact = Mathf.Clamp(Mathf.Abs(verticalVelocityBeforeMove) * 0.08f + 0.2f, 0.2f, 1.0f);

        _wasGrounded = groundedNow;
        UpdatePresentation(dt, facingVelocity, input, speed, groundedNow);
    }

    public void SetZoneBounds(Aabb[] zoneBounds)
    {
        _zoneBounds = zoneBounds ?? Array.Empty<Aabb>();
    }

    private bool UpdateVoidFallRecovery(float delta, bool grounded)
    {
        if (grounded)
        {
            _lastGroundedPosition = GlobalPosition;
            _hasLastGroundedPosition = true;
            _voidFallTimer = 0.0f;
            return false;
        }

        if (IsInsideZoneBounds(GlobalPosition))
        {
            _voidFallTimer = 0.0f;
            return false;
        }

        _voidFallTimer += delta;
        if (_voidFallTimer < VoidFallGracePeriod)
            return false;

        RecoverFromVoidFall();
        return true;
    }

    private bool IsInsideZoneBounds(Vector3 worldPosition)
    {
        for (int i = 0; i < _zoneBounds.Length; i++)
        {
            if (_zoneBounds[i].HasPoint(worldPosition))
                return true;
        }

        return false;
    }

    private void RecoverFromVoidFall()
    {
        Hp = Mathf.Max(0, Hp - 5);
        Velocity = Vector3.Zero;
        _voidFallTimer = 0.0f;
        _coyoteTimer = 0.0f;

        if (_hasLastGroundedPosition)
            GlobalPosition = _lastGroundedPosition + new Vector3(0, 0.1f, 0);

        EmitSignal(SignalName.EdgeFall);
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
        Vector3 attackDirection = targetPosition - GlobalPosition;
        attackDirection.Y = 0;
        attackDirection = attackDirection.LengthSquared() > 0.001f ? attackDirection.Normalized() : Vector3.Forward;
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

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked)
            Velocity = new Vector3(0, Velocity.Y, 0);
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

    private void UpdatePresentation(float delta, Vector3 horizontalVelocity, Vector3 inputDirection, float targetSpeed, bool grounded)
    {
        float horizontalSpeed = horizontalVelocity.Length();
        float speedRatio = targetSpeed > 0.001f
            ? Mathf.Clamp(horizontalSpeed / targetSpeed, 0.0f, 1.35f)
            : 0.0f;
        _presentationTime += delta * Mathf.Lerp(2.8f, 10.0f, speedRatio);
        _landingImpact = Mathf.MoveToward(_landingImpact, 0.0f, delta * 2.8f);

        float bobAmount = grounded
            ? Mathf.Lerp(0.012f, 0.048f, speedRatio)
            : 0.008f;
        float bobOffset = Mathf.Sin(_presentationTime) * bobAmount;
        float swayOffset = Mathf.Cos(_presentationTime * 0.5f) * Mathf.Lerp(0.004f, 0.015f, speedRatio);
        float airOffset = grounded ? 0.0f : Mathf.Clamp(Velocity.Y * 0.035f, -0.10f, 0.12f);
        float side = _sprite.FlipH ? -1.0f : 1.0f;
        Vector3 desiredBobPosition = BobRootBasePosition + new Vector3(swayOffset * side, bobOffset + airOffset, 0);
        _bobRoot.Position = _bobRoot.Position.Lerp(desiredBobPosition, grounded ? 0.22f : 0.16f);

        float lateralIntent = inputDirection.LengthSquared() > 0.0f
            ? new Vector3(Mathf.Cos(_cameraController.Yaw), 0, -Mathf.Sin(_cameraController.Yaw)).Dot(inputDirection)
            : 0.0f;
        float targetLean = -lateralIntent * Mathf.Lerp(1.5f, 8.0f, speedRatio);
        _presentationLean = Mathf.Lerp(_presentationLean, targetLean, 0.18f);
        _visualRoot.RotationDegrees = new Vector3(0, 0, _presentationLean);

        float squat = _landingImpact * 0.18f;
        float stretch = grounded
            ? -squat
            : Mathf.Clamp(Velocity.Y * 0.015f, -0.04f, 0.06f);
        _bobRoot.Scale = new Vector3(1.0f + squat * 0.6f, 1.0f + stretch, 1.0f);

        _weaponLagTilt = Mathf.Lerp(_weaponLagTilt, -_presentationLean * 0.65f, 0.16f);
        Vector3 desiredWeaponPivot = new Vector3(
            0.16f * side + Mathf.Clamp(horizontalVelocity.Length() * 0.003f * side, -0.02f, 0.02f),
            -0.03f + bobOffset * 0.28f - (grounded ? 0.0f : 0.03f),
            0.02f);
        _weaponPivot.Position = _weaponPivot.Position.Lerp(desiredWeaponPivot, 0.18f);
        _weaponPivot.RotationDegrees = _weaponPivot.RotationDegrees.Lerp(new Vector3(0, 0, _weaponLagTilt), 0.18f);
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
