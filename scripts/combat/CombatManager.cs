using System.Linq;
using Godot;

namespace ARPG;

public partial class CombatManager : Node
{
    private const float ShakeIntensity = 0.08f;
    private const float PlayerAttackCooldownSeconds = 0.32f;
    private const float MaxHitVerticalDelta = 1.6f;
    private const float PlayerAttackArcDegrees = 100.0f;
    private const float PlayerAbilityArcDegrees = 120.0f;

    private PlayerController _player;
    private TurnManager _turnManager;
    private Node3D _cameraRig;
    private PackedScene _damageNumberScene;
    private Enemy _target;
    private float _shakeTimeLeft;
    private float _playerAttackCooldownRemaining;

    /// <summary>Where the last killed enemy stood - used for loot spawning.</summary>
    public Vector3 LastKillPosition { get; private set; }
    public bool LastKillWasBoss { get; private set; }
    public bool LastKillWasElite { get; private set; }
    public InventoryItem LastKillItemDrop { get; private set; }
    public int LastKillRoom { get; private set; }

    [Signal]
    public delegate void CombatEndedEventHandler();

    [Signal]
    public delegate void CombatFeedbackEventHandler(string text);

    public Enemy Target => _target != null && IsInstanceValid(_target) ? _target : null;
    public bool IsDefeated => _turnManager?.State == TurnState.Defeat;
    public bool IsPlayerAttackReady => _playerAttackCooldownRemaining <= 0.001f;
    public float PlayerAttackCooldownRemaining => Mathf.Max(0.0f, _playerAttackCooldownRemaining);

    public void Init(PlayerController player, TurnManager turnManager, Camera3D camera)
    {
        _player = player;
        _turnManager = turnManager;
        _cameraRig = camera?.GetParent<Node3D>();
        _damageNumberScene = GD.Load<PackedScene>(Scenes.DamageNumber);
    }

    public void SetFocusTarget(Enemy enemy)
    {
        _target = enemy != null && IsInstanceValid(enemy) && !enemy.IsDead ? enemy : null;
    }

    public bool PlayerAttack()
    {
        if (_player == null || _player.Hp <= 0 || IsDefeated || !IsPlayerAttackReady)
            return false;

        _playerAttackCooldownRemaining = PlayerAttackCooldownSeconds;
        Vector3 aimPoint = _player.GetAttackAimPoint(_player.Stats.AttackRange);
        _player.ShowAttackTelegraph(_player.Stats.AttackRange, PlayerAttackArcDegrees, isHeavy: false);

        var enemy = FindBestEnemyInAttackVolume(_player.Stats.AttackRange, PlayerAttackArcDegrees);
        _player.PlayAttackAnimation(enemy != null ? enemy.GlobalPosition : aimPoint, isHeavy: false);

        if (enemy == null)
        {
            _target = null;
            SpawnFloatingText(aimPoint + Vector3.Up * 0.6f, "Miss", Palette.TextDisabled);
            EmitCombatFeedback("Whiff.");
            return true;
        }

        enemy.EnsureCombatStarted();
        _target = enemy;

        int damage = _player.Stats.ConsumePreparedAttackDamage(_player.AttackDamage, out string feedback);
        ResolveDamageAgainstEnemy(enemy, damage, feedback);
        return true;
    }

    public bool PlayerAbility()
    {
        if (_player == null || _player.Hp <= 0 || IsDefeated)
            return false;

        var ability = _player.Ability;
        if (ability == null || !ability.IsReady)
            return false;

        ability.Use();
        _playerAttackCooldownRemaining = PlayerAttackCooldownSeconds;

        if (ability.IsRanged)
            return PlayerFireProjectile(ability);

        Vector3 aimPoint = _player.GetAttackAimPoint(_player.Stats.AttackRange);
        _player.ShowAttackTelegraph(_player.Stats.AttackRange, PlayerAbilityArcDegrees, isHeavy: true);

        var enemy = FindBestEnemyInAttackVolume(_player.Stats.AttackRange, PlayerAbilityArcDegrees);
        _player.PlayAttackAnimation(enemy != null ? enemy.GlobalPosition : aimPoint, isHeavy: true);

        if (enemy == null)
        {
            _target = null;
            SpawnFloatingText(aimPoint + Vector3.Up * 0.6f, "Miss", Palette.TextDisabled);
            EmitCombatFeedback($"{ability.Name} misses.");
            return true;
        }

        enemy.EnsureCombatStarted();
        _target = enemy;

        int damage = (int)(_player.AttackDamage * ability.DamageMultiplier);
        damage = _player.Stats.ConsumePreparedAttackDamage(damage, out string feedback);
        ResolveDamageAgainstEnemy(enemy, damage, feedback);
        return true;
    }

    private bool PlayerFireProjectile(Ability ability)
    {
        int damage = (int)(_player.AttackDamage * ability.DamageMultiplier);
        damage = _player.Stats.ConsumePreparedAttackDamage(damage, out string feedback);

        Vector3 aimDirection = _player.CombatAimDirection;
        Vector3 origin = _player.GlobalPosition + new Vector3(0, 0.3f, 0);

        Color color = ability.Type == AbilityType.Fireball ? Palette.ItemBomb : Palette.ItemPower;

        var projectile = Projectile.CreatePlayerProjectile(
            aimDirection, ability.ProjectileSpeed, damage,
            _player, this,
            ability.ProjectileVisualRadius, color);
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;

        _player.PlayAttackAnimation(_player.GetAttackAimPoint(2.0f), isHeavy: true);
        EmitCombatFeedback(CombineFeedback(feedback, $"{ability.Name} fired."));
        return true;
    }

    public void EnemyFireProjectile(Enemy enemy, PlayerController target)
    {
        if (!IsValidEnemy(enemy) || target == null || target.Hp <= 0 || IsDefeated)
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        enemy.OnOwnerTurnStarted();

        Vector3 origin = enemy.GlobalPosition + new Vector3(0, 0.35f, 0);
        Vector3 targetPoint = target.GlobalPosition + new Vector3(0, 0.3f, 0);
        Vector3 direction = (targetPoint - origin).Normalized();

        enemy.PlayAttackAnimation(target.GlobalPosition);

        var projectile = Projectile.CreateEnemyProjectile(
            direction, enemy.CombatProfile.ProjectileSpeed,
            enemy, this,
            0.05f, Palette.EnemyGlow);
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;

        enemy.OnOwnerTurnEnded();
    }

    public void ResolveProjectileHitEnemy(Enemy enemy, int damage)
    {
        if (!IsValidEnemy(enemy))
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        ResolveDamageAgainstEnemy(enemy, damage);
    }

    public void ResolveProjectileHitPlayer(Enemy source)
    {
        if (_player == null || _player.Hp <= 0 || IsDefeated)
            return;

        if (source != null && IsInstanceValid(source) && !source.IsDead)
        {
            var result = source.ResolveOutgoingDamage(_player);
            if (result.Damage > 0)
            {
                _player.PlayHitReaction();
                SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.Damage, true);
                AudioManager.Instance?.PlayPlayerHit();
            }
            if (result.HealingAmount > 0)
                SpawnFloatingText(source.GlobalPosition + Vector3.Up * 0.6f, $"+{result.HealingAmount}", Palette.HealText);

            EmitCombatFeedback(result.BuildFeedbackText());
            _shakeTimeLeft = result.Damage > 0 ? 0.10f : 0.04f;

            if (_player.Hp <= 0)
                _turnManager?.SetState(TurnState.Defeat);
        }
        else
        {
            // Source enemy died or was freed before projectile landed — apply raw damage
            int rawDamage = 2;
            int finalDamage = _player.Stats.ResolveIncomingDamage(rawDamage, out _);
            if (finalDamage > 0)
            {
                _player.Hp = Mathf.Max(0, _player.Hp - finalDamage);
                _player.PlayHitReaction();
                SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, finalDamage, true);
                AudioManager.Instance?.PlayPlayerHit();
            }
            _shakeTimeLeft = 0.06f;

            if (_player.Hp <= 0)
                _turnManager?.SetState(TurnState.Defeat);
        }
    }

    public bool PlayerUseDamageItem(Enemy enemy, int damage)
    {
        if (!IsValidAttackTarget(enemy))
            return false;

        enemy.EnsureCombatStarted();
        _target = enemy;
        ResolveDamageAgainstEnemy(enemy, damage);
        return true;
    }

    public void ResolveEnemyAttack(Enemy enemy, bool playerStillInRangeAtImpact)
    {
        if (!IsValidEnemy(enemy) || _player == null || _player.Hp <= 0 || IsDefeated)
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        enemy.OnOwnerTurnStarted();
        enemy.PlayAttackAnimation(_player.GlobalPosition);

        if (!playerStillInRangeAtImpact || !CanEnemyHitPlayer(enemy))
        {
            SpawnFloatingText(_player.GlobalPosition + Vector3.Up * 0.7f, "Miss", Palette.TextDisabled);
            EmitCombatFeedback($"{enemy.DisplayName} misses.");
            _shakeTimeLeft = 0.05f;
            enemy.OnOwnerTurnEnded();
            return;
        }

        var result = enemy.ResolveOutgoingDamage(_player);
        if (result.Damage > 0)
        {
            _player.PlayHitReaction();
            SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.Damage, true);
            AudioManager.Instance?.PlayPlayerHit();
        }
        if (result.HealingAmount > 0)
            SpawnFloatingText(enemy.GlobalPosition + Vector3.Up * 0.6f, $"+{result.HealingAmount}", Palette.HealText);

        EmitCombatFeedback(result.BuildFeedbackText());
        _shakeTimeLeft = result.Damage > 0 ? 0.12f : 0.06f;
        enemy.OnOwnerTurnEnded();

        if (_player.Hp <= 0)
            _turnManager?.SetState(TurnState.Defeat);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _playerAttackCooldownRemaining = Mathf.Max(0.0f, _playerAttackCooldownRemaining - dt);
        _player?.Ability?.TickCooldown(dt);

        if (_target != null && (!IsInstanceValid(_target) || _target.IsDead))
            _target = null;

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

    private void ResolveDamageAgainstEnemy(Enemy enemy, int damage, string extraFeedback = null)
    {
        if (!IsValidEnemy(enemy))
            return;

        LastKillWasBoss = false;
        LastKillWasElite = false;
        LastKillItemDrop = null;
        LastKillRoom = 0;

        var result = enemy.ResolveIncomingDamage(damage, _player);
        GameState.RecordDamageDone(result.Damage);

        if (result.Damage > 0)
        {
            SpawnDamageNumber(enemy.GlobalPosition + Vector3.Up * 0.6f, result.Damage, false);
            enemy.PlayHitAnimation();
            AudioManager.Instance?.PlayHit();
        }

        if (result.RetaliationDamage > 0)
        {
            _player.PlayHitReaction();
            SpawnDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.RetaliationDamage, true);
            AudioManager.Instance?.PlayPlayerHit();
        }

        EmitCombatFeedback(CombineFeedback(extraFeedback, result.BuildFeedbackText()));
        _shakeTimeLeft = result.Damage > 0 || result.RetaliationDamage > 0 ? 0.14f : 0.06f;

        bool targetDied = enemy.IsDead;
        bool playerDied = _player.Hp <= 0;

        if (targetDied)
        {
            LastKillPosition = enemy.GlobalPosition;
            LastKillWasBoss = enemy.IsBoss;
            LastKillWasElite = enemy.IsElite;
            LastKillItemDrop = enemy.ItemDrop;
            LastKillRoom = enemy.ZoneRoom;
            enemy.Die();
            if (_target == enemy)
                _target = null;

            EmitSignal(SignalName.CombatEnded);
        }

        if (playerDied)
            _turnManager?.SetState(TurnState.Defeat);
    }

    private bool IsValidEnemy(Enemy enemy)
    {
        return enemy != null && IsInstanceValid(enemy) && !enemy.IsDead;
    }

    private bool IsValidAttackTarget(Enemy enemy)
    {
        return IsValidEnemy(enemy) && CanPlayerHit(enemy) && _player != null && _player.Hp > 0 && !IsDefeated;
    }

    private bool CanPlayerHit(Enemy enemy)
    {
        if (_player == null || !IsValidEnemy(enemy) || enemy.ZoneRoom != GameState.CurrentRoom)
            return false;

        if (Mathf.Abs(enemy.GlobalPosition.Y - _player.GlobalPosition.Y) > MaxHitVerticalDelta)
            return false;

        return GetHorizontalDistance(_player.GlobalPosition, enemy.GlobalPosition) <= _player.Stats.AttackRange;
    }

    private bool CanEnemyHitPlayer(Enemy enemy)
    {
        if (_player == null || !IsValidEnemy(enemy) || enemy.ZoneRoom != GameState.CurrentRoom)
            return false;

        if (Mathf.Abs(enemy.GlobalPosition.Y - _player.GlobalPosition.Y) > MaxHitVerticalDelta)
            return false;

        return GetHorizontalDistance(_player.GlobalPosition, enemy.GlobalPosition) <= enemy.AttackRange;
    }

    private Enemy FindBestEnemyInAttackVolume(float range, float arcDegrees)
    {
        if (_player == null)
            return null;

        Vector3 aimDirection = _player.CombatAimDirection;
        aimDirection.Y = 0.0f;
        if (aimDirection.LengthSquared() <= 0.001f)
            aimDirection = Vector3.Forward;
        else
            aimDirection = aimDirection.Normalized();

        float minDot = Mathf.Cos(Mathf.DegToRad(arcDegrees * 0.5f));
        Enemy bestEnemy = null;
        float bestScore = float.MinValue;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !IsValidEnemy(enemy) || enemy.ZoneRoom != GameState.CurrentRoom)
                continue;

            if (Mathf.Abs(enemy.GlobalPosition.Y - _player.GlobalPosition.Y) > MaxHitVerticalDelta)
                continue;

            Vector3 toEnemy = enemy.GlobalPosition - _player.GlobalPosition;
            toEnemy.Y = 0.0f;
            float distance = toEnemy.Length();
            if (distance > range)
                continue;

            Vector3 attackDirection = distance <= 0.001f ? aimDirection : toEnemy / distance;
            float dot = aimDirection.Dot(attackDirection);
            if (dot < minDot)
                continue;

            float score = dot * 100.0f - distance;
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestEnemy = enemy;
        }

        return bestEnemy;
    }

    private static float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 delta = a - b;
        return new Vector2(delta.X, delta.Z).Length();
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
