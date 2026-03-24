using Godot;

namespace ARPG;

public partial class CameraController : Node3D
{
    private const float MouseSensitivity = 0.003f;
    private const float MinPitch = -1.2f;   // ~-69 degrees (looking down)
    private const float MaxPitch = -0.25f;  // ~-14 degrees (nearly level)
    private const float DefaultPitch = -0.55f; // ~-31 degrees
    private const float DefaultDistance = 6.0f;
    private const float MinDistance = 3.0f;
    private const float MaxDistance = 12.0f;
    private const float ZoomStep = 0.5f;
    private const float CollisionPadding = 0.15f;
    private const float SnapDistanceMultiplier = 2.0f;
    private const int MaxRaycastPasses = 8;

    private Camera3D _camera;
    private CollisionObject3D _playerBody;
    private float _yaw;
    private float _pitch = DefaultPitch;
    private float _distance = DefaultDistance;
    private bool _combatMode;
    private bool _windowFocused = true;

    /// <summary>Current yaw in radians, used by PlayerController for camera-relative movement.</summary>
    public float Yaw => _yaw;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _playerBody = GetParentOrNull<CollisionObject3D>();
        ProcessMode = ProcessModeEnum.Always;
        UpdateCameraTransform();
        UpdateMouseMode();
    }

    public override void _Process(double delta)
    {
        UpdateMouseMode();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_combatMode)
            return;

        UpdateCameraTransform();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured && !_combatMode)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
            UpdateCameraTransform();
        }

        if (@event is InputEventMouseButton button && button.Pressed)
        {
            if (button.ButtonIndex == MouseButton.WheelUp)
            {
                _distance = Mathf.Max(MinDistance, _distance - ZoomStep);
                UpdateCameraTransform();
            }
            else if (button.ButtonIndex == MouseButton.WheelDown)
            {
                _distance = Mathf.Min(MaxDistance, _distance + ZoomStep);
                UpdateCameraTransform();
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusIn)
        {
            _windowFocused = true;
        }
        else if (what == NotificationWMWindowFocusOut)
        {
            _windowFocused = false;
            // Immediately release mouse when window loses focus to prevent
            // flicker/reload when moving cursor between monitors
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    /// <summary>
    /// Toggle mouse-look during combat so it doesn't fight with CombatManager camera tweens.
    /// </summary>
    public void SetCombatMode(bool inCombat)
    {
        _combatMode = inCombat;
    }

    /// <summary>
    /// Snapshot the current camera local position (used by CombatManager before zoom tween).
    /// </summary>
    public Vector3 GetCameraRestPosition() => _camera.Position;

    /// <summary>
    /// Restore the camera transform after combat ends (CombatManager may have tweened it).
    /// </summary>
    public void RestoreCameraTransform()
    {
        UpdateCameraTransform();
    }

    private bool ShouldCaptureMouse()
    {
        if (!_windowFocused) return false;
        if (GetTree().Paused) return false;
        return true;
    }

    private void UpdateMouseMode()
    {
        var desired = ShouldCaptureMouse()
            ? Input.MouseModeEnum.Captured
            : Input.MouseModeEnum.Visible;

        if (Input.MouseMode != desired)
            Input.MouseMode = desired;
    }

    private void UpdateCameraTransform()
    {
        // Yaw rotates the rig around Y
        Rotation = new Vector3(0, _yaw, 0);
        _camera.Rotation = new Vector3(_pitch, 0, 0);

        if (ShouldSnapToPlayer())
        {
            _camera.Position = Vector3.Zero;
            return;
        }

        _camera.Position = ResolveCameraPosition(CalculateDesiredLocalPosition());
    }

    private Vector3 CalculateDesiredLocalPosition()
    {
        // Camera orbits at _distance with _pitch angle.
        float y = -_distance * Mathf.Sin(_pitch);
        float z = _distance * Mathf.Cos(_pitch);
        return new Vector3(0, y, z);
    }

    private Vector3 ResolveCameraPosition(Vector3 desiredLocalPosition)
    {
        float desiredDistance = desiredLocalPosition.Length();
        if (desiredDistance <= Mathf.Epsilon)
            return Vector3.Zero;

        Vector3 pivotGlobalPosition = GlobalPosition;
        Vector3 desiredGlobalPosition = ToGlobal(desiredLocalPosition);
        if (!TryGetBlockingHit(pivotGlobalPosition, desiredGlobalPosition, out Vector3 hitPosition))
            return desiredLocalPosition;

        float resolvedDistance = Mathf.Max(pivotGlobalPosition.DistanceTo(hitPosition) - CollisionPadding, 0.0f);
        return desiredLocalPosition.Normalized() * resolvedDistance;
    }

    private bool TryGetBlockingHit(Vector3 from, Vector3 to, out Vector3 hitPosition)
    {
        hitPosition = Vector3.Zero;

        var spaceState = GetWorld3D()?.DirectSpaceState;
        if (spaceState == null)
            return false;

        var excludedBodies = new Godot.Collections.Array<Rid>();
        if (_playerBody != null)
            excludedBodies.Add(_playerBody.GetRid());

        for (int pass = 0; pass < MaxRaycastPasses; pass++)
        {
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollideWithBodies = true;
            query.CollideWithAreas = false;
            query.Exclude = excludedBodies;

            var result = spaceState.IntersectRay(query);
            if (result.Count == 0)
                return false;

            if (result["collider"].AsGodotObject() is Node collider && collider.IsInGroup(WorldGroups.CameraBlockers))
            {
                hitPosition = result["position"].AsVector3();
                return true;
            }

            if (result["collider"].AsGodotObject() is CollisionObject3D body)
            {
                excludedBodies.Add(body.GetRid());
                continue;
            }

            return false;
        }

        return false;
    }

    private bool ShouldSnapToPlayer()
    {
        return _camera.GlobalPosition.DistanceTo(GlobalPosition) >= _distance * SnapDistanceMultiplier;
    }
}
