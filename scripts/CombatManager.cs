using Godot;

namespace ARPG;

public partial class CombatManager : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private Camera3D _camera;
    private Node3D _cameraRig;
    private PackedScene _damageNumberScene;

    private Enemy _target;
    private Vector3 _cameraRestPosition;
    private float _shakeTimeLeft;
    private const float ShakeIntensity = 0.08f;
    private const float EnemyRetaliationDelay = 0.5f;

    /// <summary>Where the last killed enemy stood — used for loot spawning.</summary>
    public Vector3 LastKillPosition { get; private set; }

    [Signal]
    public delegate void CombatEndedEventHandler();

    public Enemy Target => _target;

    public void Init(PlayerController player, TurnManager turnManager, Camera3D camera)
    {
        _player = player;
        _turnManager = turnManager;
        _camera = camera;
        _cameraRig = camera.GetParent<Node3D>();
        _cameraRestPosition = _camera.Position;
        _damageNumberScene = GD.Load<PackedScene>("res://scenes/DamageNumber.tscn");
    }

    public void EnterCombat(Enemy enemy)
    {
        _target = enemy;
        _turnManager.SetState(TurnState.Busy);
        _player.SetPhysicsProcess(false);

        var tween = CreateTween();
        var zoomedPos = _cameraRestPosition * 0.75f;
        tween.TweenProperty(_camera, "position", zoomedPos, 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() =>
        {
            _shakeTimeLeft = 0.2f;
            _turnManager.SetState(TurnState.PlayerTurn);
        }));
    }

    public void PlayerAttack()
    {
        if (!_turnManager.IsPlayerTurn || _target == null) return;
        DealDamage(_player.AttackDamage);
    }

    public void PlayerAbility()
    {
        if (!_turnManager.IsPlayerTurn || _target == null) return;

        var ability = _player.Ability;
        if (ability == null || !ability.IsReady) return;

        int damage = (int)(_player.AttackDamage * ability.DamageMultiplier);
        ability.Use();
        DealDamage(damage);
    }

    private void DealDamage(int damage)
    {
        _turnManager.SetState(TurnState.Busy);

        _target.TakeDamage(damage);
        SpawnDamageNumber(_target.GlobalPosition + Vector3.Up * 1.2f, damage, false);
        _shakeTimeLeft = 0.15f;

        // Tick ability cooldown after each attack
        _player.Ability?.TickCooldown();

        if (_target.IsDead)
        {
            LastKillPosition = _target.GlobalPosition;
            _target.Die();
            _target = null;
            ExitCombat();
            EmitSignal(SignalName.CombatEnded);
            return;
        }

        var timer = GetTree().CreateTimer(EnemyRetaliationDelay);
        timer.Timeout += OnEnemyRetaliate;
    }

    private void OnEnemyRetaliate()
    {
        if (_target == null || !IsInstanceValid(_target)) return;

        _turnManager.SetState(TurnState.EnemyTurn);

        int damage = _target.AttackDamage;
        _target.AttackPlayer(_player);
        SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 1.5f, damage, true);
        _shakeTimeLeft = 0.12f;

        if (_player.Hp <= 0)
        {
            _turnManager.SetState(TurnState.Defeat);
            return;
        }

        _turnManager.SetState(TurnState.PlayerTurn);
    }

    private void ExitCombat()
    {
        var tween = CreateTween();
        var overshoot = _cameraRestPosition * 1.08f;
        tween.TweenProperty(_camera, "position", overshoot, 0.25f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_camera, "position", _cameraRestPosition, 0.2f)
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() =>
        {
            _turnManager.SetState(TurnState.Exploring);
            _player.SetPhysicsProcess(true);
        }));
    }

    public override void _Process(double delta)
    {
        if (_shakeTimeLeft > 0)
        {
            _shakeTimeLeft -= (float)delta;
            var offset = new Vector3(
                (float)GD.RandRange(-ShakeIntensity, ShakeIntensity),
                (float)GD.RandRange(-ShakeIntensity, ShakeIntensity),
                0
            );
            _cameraRig.Position = offset;
        }
        else
        {
            _cameraRig.Position = Vector3.Zero;
        }
    }

    private void SpawnDamageNumber(Vector3 worldPos, int amount, bool isPlayerDamage)
    {
        var instance = _damageNumberScene.Instantiate<Node3D>();
        instance.GlobalPosition = worldPos;
        GetTree().CurrentScene.AddChild(instance);

        if (instance is DamageNumber dn)
            dn.Setup(amount, isPlayerDamage);
    }
}
