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

    private Camera3D _camera;
    private float _yaw;
    private float _pitch = DefaultPitch;
    private float _distance = DefaultDistance;
    private bool _combatMode;

    /// <summary>Current yaw in radians, used by PlayerController for camera-relative movement.</summary>
    public float Yaw => _yaw;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
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

    private void UpdateCameraTransform()
    {
        // Yaw rotates the rig around Y
        Rotation = new Vector3(0, _yaw, 0);

        // Camera orbits at _distance with _pitch angle
        float y = -_distance * Mathf.Sin(_pitch);
        float z = _distance * Mathf.Cos(_pitch);
        _camera.Position = new Vector3(0, y, z);
        _camera.Rotation = new Vector3(_pitch, 0, 0);
    }
}
