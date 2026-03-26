using System;
using Godot;

namespace ARPG;

public partial class PlayerController : CharacterBody3D, IDeveloperEffectProvider
{
    private const float GodFlightSpeedMultiplier = 2.4f;
    private const float GodFlightVeryFastMultiplier = 8.0f;
    private static readonly Vector3 BobRootBasePosition = new(0, 0.25f, 0);

    public PlayerStats Stats { get; private set; }
    public Ability Ability { get; private set; }

    public int Hp { get => Stats.CurrentHp; set => Stats.CurrentHp = value; }
    public int AttackDamage => Stats.AttackDamage;
    public bool IsGrounded => IsOnFloor();
    public Vector3 CombatAimDirection => _combatAimDirection;

    private float _regenAccumulator;
    private float _presentationTime;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private float _landingImpact;
    private float _presentationLean;
    private float _weaponLagTilt;
    private double _godModeElapsed;
    private float _remainingJumpBudget;
    private bool _movementLocked;
    private bool _wasGrounded;
    private bool _lastGodModeEnabled;
    private bool _lastPassThroughEnabled;
    private Vector3 _combatAimDirection = Vector3.Forward;
    private CameraController _cameraController;
    private CollisionShape3D _playerCollision;
    private DeveloperToolsManager _developerTools;
    private Node3D _visualRoot;
    private Node3D _bobRoot;
    private Sprite3D _sprite;
    private Node3D _weaponPivot;
    private Sprite3D _weaponSprite;
    private Tween _visualTween;
    private readonly ForwardDoubleTapDetector _forwardBurstDetector = new();

    public override void _Ready()
    {
        FloorSnapLength = PlayerTraversalFeel.DefaultFloorSnapLength;
        _wasGrounded = true;

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
        _playerCollision = GetNode<CollisionShape3D>("PlayerCollision");
        ResetAvailableJumps();
    }

    public override void _PhysicsProcess(double delta)
    {
        SyncDeveloperTraversalState();
        if (CurrentTraversalMode == PlayerTraversalMode.GodFlight)
        {
            RunGodFlightPhysics(delta);
            return;
        }

        _developerTools?.GodMode.SetVeryFastBurstActive(false);
        _forwardBurstDetector.Reset();
        RunGroundedPhysics(delta);
    }

    public void RegisterDeveloperEffects(DeveloperToolsManager developerTools)
    {
        if (_developerTools != null)
            return;

        _developerTools = developerTools;
    }

    private PlayerTraversalMode CurrentTraversalMode => _developerTools?.GodMode.Enabled == true && _developerTools.GodMode.FlightEnabled
        ? PlayerTraversalMode.GodFlight
        : PlayerTraversalMode.Grounded;

    private void RunGroundedPhysics(double delta)
    {
        float dt = (float)delta;
        bool jumpPressed = !_movementLocked && Input.IsActionJustPressed(GameKeys.Jump);
        bool jumpHeld = !_movementLocked && Input.IsActionPressed(GameKeys.Jump);

        // Gather raw input
        var raw = Vector3.Zero;
        if (!_movementLocked)
        {
            raw.X = Input.GetActionStrength(GameKeys.MoveRight) - Input.GetActionStrength(GameKeys.MoveLeft);
            raw.Z = Input.GetActionStrength(GameKeys.MoveBack) - Input.GetActionStrength(GameKeys.MoveForward);
        }

        if (raw.LengthSquared() > 0)
            raw = raw.Normalized();

        // Transform input direction by camera yaw so WASD is camera-relative
        float yaw = _cameraController.Yaw;
        var input = new Basis(Vector3.Up, yaw) * raw;

        float speed = Stats.MoveSpeed;
        if (!_movementLocked && Input.IsActionPressed(GameKeys.Sprint))
            speed *= Stats.SprintMultiplier;

        bool grounded = IsOnFloor();
        _coyoteTimer = grounded ? PlayerTraversalFeel.CoyoteTime : Mathf.Max(0.0f, _coyoteTimer - dt);
        _jumpBufferTimer = jumpPressed ? PlayerTraversalFeel.JumpBufferTime : Mathf.Max(0.0f, _jumpBufferTimer - dt);

        if (grounded)
            ResetAvailableJumps();

        var horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        var targetHorizontalVelocity = input * speed * (grounded ? 1.0f : PlayerTraversalFeel.AirControlSpeedFactor);
        float moveRate = PlayerTraversalFeel.GetHorizontalMoveRate(horizontalVelocity, targetHorizontalVelocity, grounded);
        horizontalVelocity = horizontalVelocity.MoveToward(targetHorizontalVelocity, moveRate * dt);

        float verticalVelocityBeforeMove = Velocity.Y;

        if (TryConsumeBufferedJump(grounded))
        {
            grounded = false;
            verticalVelocityBeforeMove = Velocity.Y;
        }

        if (!grounded)
        {
            float gravity = PlayerTraversalFeel.GetVerticalGravity(Velocity.Y, jumpHeld);
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y - gravity * dt, horizontalVelocity.Z);
        }
        else if (Velocity.Y < 0.0f)
            Velocity = new Vector3(horizontalVelocity.X, -0.01f, horizontalVelocity.Z);
        else
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);

        MoveAndSlide();

        bool groundedNow = IsOnFloor();

        // Flip sprite based on camera-relative horizontal movement
        var facingVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        UpdateCombatAimDirection(input.LengthSquared() > 0.001f ? input : facingVelocity);
        if (facingVelocity.LengthSquared() > 0.01f)
        {
            var cameraRight = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
            _sprite.FlipH = facingVelocity.Dot(cameraRight) < 0;
        }

        if (!_wasGrounded && groundedNow)
            _landingImpact = Mathf.Clamp(Mathf.Abs(verticalVelocityBeforeMove) * 0.08f + 0.2f, 0.2f, 1.0f);

        if (groundedNow)
            ResetAvailableJumps();

        _wasGrounded = groundedNow;
        UpdatePresentation(dt, facingVelocity, input, speed, groundedNow);
    }

    private void RunGodFlightPhysics(double delta)
    {
        float dt = (float)delta;
        _godModeElapsed += delta;

        bool forwardHeld = !_movementLocked && Input.IsActionPressed(GameKeys.MoveForward);
        bool forwardJustPressed = !_movementLocked && Input.IsActionJustPressed(GameKeys.MoveForward);
        _forwardBurstDetector.Update(forwardJustPressed, forwardHeld, _godModeElapsed);
        bool veryFastBurst = _forwardBurstDetector.IsBurstActive(forwardHeld);
        _developerTools?.GodMode.SetVeryFastBurstActive(veryFastBurst);

        Vector3 movementDirection = Vector3.Zero;
        if (!_movementLocked)
        {
            Basis viewBasis = _cameraController.CameraBasis;
            Vector3 forward = -viewBasis.Z.Normalized();
            Vector3 right = viewBasis.X.Normalized();
            float horizontalX = Input.GetActionStrength(GameKeys.MoveRight) - Input.GetActionStrength(GameKeys.MoveLeft);
            float horizontalZ = Input.GetActionStrength(GameKeys.MoveForward) - Input.GetActionStrength(GameKeys.MoveBack);
            float vertical = Input.GetActionStrength(GameKeys.DevAscend) - Input.GetActionStrength(GameKeys.DevDescend);

            movementDirection = right * horizontalX + forward * horizontalZ + Vector3.Up * vertical;
            if (movementDirection.LengthSquared() > 0.001f)
                movementDirection = movementDirection.Normalized();
        }

        float speedMultiplier = veryFastBurst ? GodFlightVeryFastMultiplier : GodFlightSpeedMultiplier;
        float speed = Stats.MoveSpeed * speedMultiplier;
        Velocity = movementDirection * speed;

        if (_developerTools?.GodMode.PassThroughEnabled == true)
            GlobalPosition += Velocity * dt;
        else
            MoveAndSlide();

        bool groundedNow = IsOnFloor();

        var facingVelocity = new Vector3(Velocity.X, 0, Velocity.Z);
        UpdateCombatAimDirection(movementDirection.LengthSquared() > 0.001f
            ? new Vector3(movementDirection.X, 0, movementDirection.Z)
            : facingVelocity);
        if (facingVelocity.LengthSquared() > 0.01f)
        {
            var cameraRight = new Vector3(Mathf.Cos(_cameraController.Yaw), 0, -Mathf.Sin(_cameraController.Yaw));
            _sprite.FlipH = facingVelocity.Dot(cameraRight) < 0;
        }

        var presentationInput = new Vector3(movementDirection.X, 0, movementDirection.Z);
        UpdatePresentation(dt, facingVelocity, presentationInput, speed, groundedNow);
        _wasGrounded = groundedNow;
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

    public void ShowAttackTelegraph(float range, float arcDegrees, bool isHeavy)
    {
        var telegraph = new MeleeAttackTelegraph();
        telegraph.GlobalPosition = GlobalPosition + new Vector3(0, -0.20f, 0);
        GetTree().CurrentScene.AddChild(telegraph);
        telegraph.Play(
            _combatAimDirection,
            range,
            arcDegrees,
            isHeavy ? Palette.ItemArcane : Palette.ItemPower,
            isHeavy ? 0.18f : 0.14f);
    }

    public Vector3 GetAttackAimPoint(float distance)
    {
        return GlobalPosition + _combatAimDirection * distance;
    }

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked)
        {
            Velocity = Vector3.Zero;
            _coyoteTimer = 0.0f;
            _jumpBufferTimer = 0.0f;
            ResetAvailableJumps();
            _forwardBurstDetector.Reset();
            _developerTools?.GodMode.SetVeryFastBurstActive(false);
        }
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

    private void UpdateCombatAimDirection(Vector3 preferredDirection)
    {
        Vector3 direction = preferredDirection;
        direction.Y = 0.0f;

        if (direction.LengthSquared() <= 0.001f)
        {
            Vector3 fallback = -_cameraController.CameraBasis.Z;
            fallback.Y = 0.0f;
            direction = fallback;
        }

        if (direction.LengthSquared() > 0.001f)
            _combatAimDirection = direction.Normalized();
    }

    private void SyncDeveloperTraversalState()
    {
        bool godModeEnabled = _developerTools?.GodMode.Enabled == true && _developerTools.GodMode.FlightEnabled;
        bool passThroughEnabled = godModeEnabled && _developerTools.GodMode.PassThroughEnabled;
        if (godModeEnabled == _lastGodModeEnabled && passThroughEnabled == _lastPassThroughEnabled)
            return;

        _lastGodModeEnabled = godModeEnabled;
        _lastPassThroughEnabled = passThroughEnabled;
        FloorSnapLength = godModeEnabled ? 0.0f : PlayerTraversalFeel.DefaultFloorSnapLength;
        if (_playerCollision != null)
            _playerCollision.Disabled = passThroughEnabled;

        if (!godModeEnabled)
        {
            Velocity = Vector3.Zero;
            _forwardBurstDetector.Reset();
            _developerTools?.GodMode.SetVeryFastBurstActive(false);
        }
    }

    private void ResetAvailableJumps()
    {
        _remainingJumpBudget = MathF.Max(1.0f, Stats?.JumpCount ?? 1.0f);
    }

    private bool TryConsumeBufferedJump(bool grounded)
    {
        if (_jumpBufferTimer <= 0.0f)
            return false;

        bool canUseGroundJump = grounded || _coyoteTimer > 0.0f;
        if (!canUseGroundJump && _remainingJumpBudget <= 0.001f)
            return false;

        float jumpScale = canUseGroundJump ? 1.0f : MathF.Min(_remainingJumpBudget, 1.0f);
        Velocity = new Vector3(Velocity.X, PlayerTraversalFeel.ComputeJumpVelocity(Stats.JumpHeight * jumpScale), Velocity.Z);
        _jumpBufferTimer = 0.0f;
        _coyoteTimer = 0.0f;
        _remainingJumpBudget = MathF.Max(0.0f, _remainingJumpBudget - 1.0f);
        return true;
    }
}
