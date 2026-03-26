using Godot;

namespace ARPG;

/// <summary>
/// Temporary combat presentation adapter.
/// Keeps camera shake and floating-number spawning while CombatSystem owns
/// targeting, hit validation, and damage resolution.
/// </summary>
public partial class CombatManager : Node
{
    private const float ShakeIntensity = 0.08f;

    private Node3D _cameraRig;
    private PackedScene _damageNumberScene;
    private float _shakeTimeLeft;

    public void Init(PlayerController player, TurnManager turnManager, Camera3D camera)
    {
        _cameraRig = camera?.GetParent<Node3D>();
        _damageNumberScene = GD.Load<PackedScene>(Scenes.DamageNumber);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_shakeTimeLeft > 0)
        {
            _shakeTimeLeft -= dt;
            if (_cameraRig != null)
            {
                _cameraRig.Position = new Vector3(
                    (float)GD.RandRange(-ShakeIntensity, ShakeIntensity),
                    (float)GD.RandRange(-ShakeIntensity, ShakeIntensity),
                    0);
            }
        }
        else if (_cameraRig != null)
        {
            _cameraRig.Position = Vector3.Zero;
        }
    }

    public void TriggerShake(float seconds)
    {
        _shakeTimeLeft = Mathf.Max(_shakeTimeLeft, Mathf.Max(0.0f, seconds));
    }

    public void ShowDamageNumber(Vector3 worldPos, int amount, bool isPlayerDamage)
    {
        var instance = _damageNumberScene.Instantiate<Node3D>();
        instance.GlobalPosition = worldPos;
        GetTree().CurrentScene.AddChild(instance);

        if (instance is DamageNumber damageNumber)
            damageNumber.Setup(amount, isPlayerDamage);
    }

    public void ShowFloatingText(Vector3 worldPos, string text, Color color)
    {
        var instance = _damageNumberScene.Instantiate<Node3D>();
        instance.GlobalPosition = worldPos;
        GetTree().CurrentScene.AddChild(instance);

        if (instance is DamageNumber damageNumber)
            damageNumber.SetupText(text, color);
    }
}
