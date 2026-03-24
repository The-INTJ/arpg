using System.Linq;
using Godot;

namespace ARPG;

public partial class CombatManager : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private Camera3D _camera;
    private Node3D _cameraRig;
    private CameraController _cameraController;
    private PackedScene _damageNumberScene;

    private Enemy _target;
    private Vector3 _cameraRestPosition;
    private float _shakeTimeLeft;
    private const float ShakeIntensity = 0.08f;
    private const float EnemyRetaliationDelay = 0.5f;

    /// <summary>Where the last killed enemy stood — used for loot spawning.</summary>
    public Vector3 LastKillPosition { get; private set; }
    public bool LastKillWasBoss { get; private set; }
    public bool LastKillWasElite { get; private set; }
    public InventoryItem LastKillItemDrop { get; private set; }

    [Signal]
    public delegate void CombatEndedEventHandler();

    [Signal]
    public delegate void CombatFeedbackEventHandler(string text);

    public Enemy Target => _target;

    public void Init(PlayerController player, TurnManager turnManager, Camera3D camera)
    {
        _player = player;
        _turnManager = turnManager;
        _camera = camera;
        _cameraRig = camera.GetParent<Node3D>();
        _cameraController = _cameraRig as CameraController;
        _damageNumberScene = GD.Load<PackedScene>(Scenes.DamageNumber);
    }

    public void EnterCombat(Enemy enemy)
    {
        _target = enemy;
        LastKillItemDrop = null;
        _target.OnCombatStarted();
        _turnManager.SetState(TurnState.Busy);
        _player.SetPhysicsProcess(false);
        AudioManager.Instance?.StartCombatMusic();

        // Snapshot current camera position before zoom (dynamic with orbit camera)
        _cameraRestPosition = _camera.Position;
        _cameraController?.SetCombatMode(true);

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
        int damage = _player.Stats.ConsumePreparedAttackDamage(_player.AttackDamage, out string feedback);
        _player.PlayAttackAnimation(_target.GlobalPosition, isHeavy: false);
        ResolveDamageAction(damage, endTurnAfterAction: true, tickAbilityCooldown: true, extraFeedback: feedback);
    }

    public void PlayerAbility()
    {
        if (!_turnManager.IsPlayerTurn || _target == null) return;

        var ability = _player.Ability;
        if (ability == null || !ability.IsReady) return;

        int damage = (int)(_player.AttackDamage * ability.DamageMultiplier);
        ability.Use();
        damage = _player.Stats.ConsumePreparedAttackDamage(damage, out string feedback);
        _player.PlayAttackAnimation(_target.GlobalPosition, isHeavy: true);
        ResolveDamageAction(damage, endTurnAfterAction: true, tickAbilityCooldown: true, extraFeedback: feedback);
    }

    public void PlayerUseDamageItem(int damage, bool endTurnAfterUse)
    {
        if (!_turnManager.IsPlayerTurn || _target == null) return;
        ResolveDamageAction(damage, endTurnAfterAction: endTurnAfterUse, tickAbilityCooldown: endTurnAfterUse);
    }

    public void PlayerUseUtilityItem(bool endTurnAfterUse)
    {
        if (!_turnManager.IsPlayerTurn || _target == null || !IsInstanceValid(_target)) return;

        if (!endTurnAfterUse)
            return;

        _player.Ability?.TickCooldown();
        _turnManager.SetState(TurnState.Busy);

        var timer = GetTree().CreateTimer(EnemyRetaliationDelay);
        timer.Timeout += OnEnemyRetaliate;
    }

    private void ResolveDamageAction(int damage, bool endTurnAfterAction, bool tickAbilityCooldown, string extraFeedback = null)
    {
        _turnManager.SetState(TurnState.Busy);
        LastKillWasBoss = false;
        LastKillWasElite = false;
        LastKillItemDrop = null;

        var result = _target.ResolveIncomingDamage(damage, _player);
        GameState.RecordDamageDone(result.Damage);
        SpawnDamageNumber(_target.GlobalPosition + Vector3.Up * 0.6f, result.Damage, false);
        if (result.Damage > 0)
        {
            _target.PlayHitAnimation();
            AudioManager.Instance?.PlayHit();
        }

        if (result.RetaliationDamage > 0)
        {
            _player.PlayHitReaction();
            SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.RetaliationDamage, true);
        }

        EmitCombatFeedback(CombineFeedback(extraFeedback, result.BuildFeedbackText()));
        _shakeTimeLeft = result.Damage > 0 || result.RetaliationDamage > 0 ? 0.15f : 0.08f;

        if (tickAbilityCooldown)
            _player.Ability?.TickCooldown();

        bool targetDied = _target.IsDead;
        bool playerDied = _player.Hp <= 0;

        if (targetDied)
        {
            if (!tickAbilityCooldown)
                _player.Ability?.TickCooldown();

            LastKillPosition = _target.GlobalPosition;
            LastKillWasBoss = _target.IsBoss;
            LastKillWasElite = _target.IsElite;
            LastKillItemDrop = _target.ItemDrop;
            _target.Die();
            _target = null;
            EmitSignal(SignalName.CombatEnded);

            if (playerDied)
            {
                _turnManager.SetState(TurnState.Defeat);
                return;
            }

            ExitCombat();
            return;
        }

        if (playerDied)
        {
            _turnManager.SetState(TurnState.Defeat);
            return;
        }

        if (!endTurnAfterAction)
        {
            _turnManager.SetState(TurnState.PlayerTurn);
            return;
        }

        var timer = GetTree().CreateTimer(EnemyRetaliationDelay);
        timer.Timeout += OnEnemyRetaliate;
    }

    private void OnEnemyRetaliate()
    {
        if (_target == null || !IsInstanceValid(_target)) return;

        _turnManager.SetState(TurnState.EnemyTurn);
        _target.OnOwnerTurnStarted();
        _target.PlayAttackAnimation(_player.GlobalPosition);

        var result = _target.ResolveOutgoingDamage(_player);
        if (result.Damage > 0)
        {
            _player.PlayHitReaction();
            SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.Damage, true);
        }
        if (result.Damage > 0)
            AudioManager.Instance?.PlayPlayerHit();
        if (result.HealingAmount > 0)
            SpawnFloatingText(_target.GlobalPosition + Vector3.Up * 0.6f, $"+{result.HealingAmount}", Palette.HealText);
        EmitCombatFeedback(result.BuildFeedbackText());
        _shakeTimeLeft = result.Damage > 0 ? 0.12f : 0.08f;
        _target.OnOwnerTurnEnded();

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
            _cameraController?.SetCombatMode(false);
            _cameraController?.RestoreCameraTransform();
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

    private void SpawnFloatingText(Vector3 worldPos, string text, Color color)
    {
        var instance = _damageNumberScene.Instantiate<Node3D>();
        instance.GlobalPosition = worldPos;
        GetTree().CurrentScene.AddChild(instance);

        if (instance is DamageNumber dn)
            dn.SetupText(text, color);
    }

    private void EmitCombatFeedback(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        EmitSignal(SignalName.CombatFeedback, text);
    }

    private static string CombineFeedback(params string[] parts)
    {
        return string.Join("  ", parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct());
    }
}
