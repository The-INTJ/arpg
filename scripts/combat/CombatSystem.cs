using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public partial class CombatSystem : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private CombatManager _presentation;
    private readonly CombatQueryService _queryService = new();
    private Enemy _target;
    private float _playerAttackCooldownRemaining;
    private PendingPlayerAttack _pendingPlayerAttack;

    [Signal]
    public delegate void CombatEndedEventHandler();

    [Signal]
    public delegate void CombatFeedbackEventHandler(string text);

    public Vector3 LastKillPosition { get; private set; }
    public bool LastKillWasBoss { get; private set; }
    public bool LastKillWasElite { get; private set; }
    public InventoryItem LastKillItemDrop { get; private set; }
    public int LastKillRoom { get; private set; }

    public Enemy Target => _target != null && IsInstanceValid(_target) ? _target : null;
    public bool IsDefeated => _turnManager?.State == TurnState.Defeat;
    public bool IsPlayerAttackReady => _pendingPlayerAttack == null && _playerAttackCooldownRemaining <= 0.001f;
    public float PlayerAttackCooldownRemaining => Mathf.Max(0.0f, _playerAttackCooldownRemaining);

    public void Init(PlayerController player, TurnManager turnManager, CombatManager presentation)
    {
        _player = player;
        _turnManager = turnManager;
        _presentation = presentation;
    }

    public Enemy PreviewPrimaryTarget()
    {
        var hit = PreviewPlayerAttack(_player?.Stats?.Weapon?.BasicAttack ?? default);
        return hit?.Target as Enemy;
    }

    public bool CanEnemyAttackConnect(Enemy enemy)
    {
        if (!IsValidEnemy(enemy) || _player == null || _player.Hp <= 0)
            return false;

        if (enemy.CombatProfile.IsRanged)
        {
            if (Mathf.Abs(enemy.GlobalPosition.Y - _player.GlobalPosition.Y) > 1.6f)
                return false;

            return enemy.GetHorizontalDistanceToPlayer() <= enemy.AttackReach;
        }

        var query = BuildEnemyAttackQuery(enemy, isPreview: false);
        return _queryService.CanConnect(query);
    }

    public float GetEnemyPreferredStopDistance(Enemy enemy)
    {
        if (!IsValidEnemy(enemy) || _player == null)
            return 0.65f;

        float ownRadius = enemy.Hurtbox?.ApproximateRadius ?? 0.25f;
        float playerRadius = _player.Hurtbox?.ApproximateRadius ?? 0.25f;
        return Mathf.Max(0.65f, enemy.AttackReach * 0.5f + ownRadius + playerRadius);
    }

    public bool PlayerAttack()
    {
        if (_player == null || _player.Hp <= 0 || IsDefeated || !IsPlayerAttackReady || _player.Stats?.Weapon == null)
            return false;

        return StartPlayerAttack(
            _player.Stats.Weapon.BasicAttack,
            rawDamage: _player.AttackDamage,
            missText: "Whiff.",
            extraFeedback: null,
            isHeavy: false);
    }

    public bool PlayerAbility()
    {
        if (_player == null || _player.Hp <= 0 || IsDefeated)
            return false;

        var ability = _player.Ability;
        if (ability == null || !ability.IsReady)
            return false;

        ability.Use();
        if (ability.Attack.IsProjectile)
            return FirePlayerProjectile(ability);

        return StartPlayerAttack(
            ability.Attack,
            rawDamage: _player.AttackDamage,
            missText: $"{ability.Name} misses.",
            extraFeedback: null,
            isHeavy: true);
    }

    public bool PlayerUseDamageItem(Enemy enemy, int damage)
    {
        if (!IsValidEnemy(enemy) || damage <= 0 || _player == null || _player.Hp <= 0 || IsDefeated)
            return false;

        enemy.EnsureCombatStarted();
        _target = enemy;
        ResolveDamageAgainstEnemy(enemy, damage);
        return true;
    }

    public void EnemyFireProjectile(Enemy enemy, PlayerController target)
    {
        if (!IsValidEnemy(enemy) || target == null || target.Hp <= 0 || IsDefeated)
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        enemy.OnOwnerTurnStarted();
        enemy.PlayAttackAnimation(target.GlobalPosition);

        Vector3 origin = enemy.ProjectileOrigin;
        Vector3 targetPoint = target.ProjectileOrigin;
        Vector3 direction = (targetPoint - origin).Normalized();

        var projectile = Projectile.CreateEnemyProjectile(
            direction,
            enemy.AttackDamage,
            enemy,
            this,
            enemy.CombatProfile.Attack,
            Palette.EnemyGlow);
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;
        enemy.OnOwnerTurnEnded();
    }

    public void ResolveEnemyAttack(Enemy enemy, bool playerStillInRangeAtImpact)
    {
        if (!IsValidEnemy(enemy) || _player == null || _player.Hp <= 0 || IsDefeated)
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        enemy.OnOwnerTurnStarted();
        enemy.PlayAttackAnimation(_player.GlobalPosition);

        if (!playerStillInRangeAtImpact || !CanEnemyAttackConnect(enemy))
        {
            _presentation?.ShowFloatingText(_player.GlobalPosition + Vector3.Up * 0.7f, "Miss", Palette.TextDisabled);
            EmitCombatFeedback($"{enemy.DisplayName} misses.");
            _presentation?.TriggerShake(0.05f);
            enemy.OnOwnerTurnEnded();
            return;
        }

        var result = enemy.ResolveOutgoingDamage(_player);
        if (result.Damage > 0)
        {
            _player.PlayHitReaction();
            _presentation?.ShowDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.Damage, true);
            AudioManager.Instance?.PlayPlayerHit();
        }
        if (result.HealingAmount > 0)
            _presentation?.ShowFloatingText(enemy.GlobalPosition + Vector3.Up * 0.6f, $"+{result.HealingAmount}", Palette.HealText);

        EmitCombatFeedback(result.BuildFeedbackText());
        _presentation?.TriggerShake(result.Damage > 0 ? 0.12f : 0.06f);
        enemy.OnOwnerTurnEnded();

        if (_player.Hp <= 0)
            _turnManager?.SetState(TurnState.Defeat);
    }

    public void ResolveProjectileHitEnemy(CombatHurtbox hurtbox, int damage)
    {
        if (hurtbox?.OwnerCombatant is not Enemy enemy || !IsValidEnemy(enemy))
            return;

        enemy.EnsureCombatStarted();
        _target = enemy;
        ResolveDamageAgainstEnemy(enemy, damage);
    }

    public void ResolveProjectileHitPlayer(CombatHurtbox hurtbox, Enemy source, int fallbackDamage)
    {
        if (hurtbox?.OwnerCombatant is not PlayerController player || player.Hp <= 0 || IsDefeated)
            return;

        if (source != null && IsInstanceValid(source) && !source.IsDead)
        {
            var result = source.ResolveOutgoingDamage(player);
            if (result.Damage > 0)
            {
                player.PlayHitReaction();
                _presentation?.ShowDamageNumber(player.GlobalPosition + Vector3.Up * 0.7f, result.Damage, true);
                AudioManager.Instance?.PlayPlayerHit();
            }
            if (result.HealingAmount > 0)
                _presentation?.ShowFloatingText(source.GlobalPosition + Vector3.Up * 0.6f, $"+{result.HealingAmount}", Palette.HealText);

            EmitCombatFeedback(result.BuildFeedbackText());
            _presentation?.TriggerShake(result.Damage > 0 ? 0.10f : 0.04f);

            if (player.Hp <= 0)
                _turnManager?.SetState(TurnState.Defeat);
            return;
        }

        int finalDamage = player.Stats.ResolveIncomingDamage(fallbackDamage, out _);
        if (finalDamage > 0)
        {
            player.Hp = Mathf.Max(0, player.Hp - finalDamage);
            player.PlayHitReaction();
            _presentation?.ShowDamageNumber(player.GlobalPosition + Vector3.Up * 0.7f, finalDamage, true);
            AudioManager.Instance?.PlayPlayerHit();
        }
        _presentation?.TriggerShake(0.06f);

        if (player.Hp <= 0)
            _turnManager?.SetState(TurnState.Defeat);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _playerAttackCooldownRemaining = Mathf.Max(0.0f, _playerAttackCooldownRemaining - dt);
        _player?.Ability?.TickCooldown(dt);

        if (_pendingPlayerAttack != null)
            AdvancePendingPlayerAttack(dt);

        if (_target != null && (!IsInstanceValid(_target) || _target.IsDead))
            _target = null;
    }

    private bool FirePlayerProjectile(Ability ability)
    {
        if (_player == null)
            return false;

        int damage = (int)Mathf.Round(_player.AttackDamage * ability.Attack.DamageMultiplier);
        damage = _player.Stats.ConsumePreparedAttackDamage(damage, out string feedback);

        Vector3 origin = _player.ProjectileOrigin;
        Vector3 aimDirection = _player.CombatAimDirection;
        var projectile = Projectile.CreatePlayerProjectile(
            aimDirection,
            damage,
            _player,
            this,
            ability.Attack,
            ability.Type == AbilityType.Fireball ? Palette.ItemBomb : Palette.ItemPower);
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = origin;

        _player.PlayAttackAnimation(_player.GetAttackAimPoint(2.0f), isHeavy: true);
        _playerAttackCooldownRemaining = ability.Attack.Timeline.TotalSeconds;
        EmitCombatFeedback(CombineFeedback(feedback, $"{ability.Name} fired."));
        return true;
    }

    private bool StartPlayerAttack(
        AttackDefinition attack,
        int rawDamage,
        string missText,
        string extraFeedback,
        bool isHeavy)
    {
        if (_player == null || !IsPlayerAttackReady)
            return false;

        float reach = _player.Stats.AttackReach;
        float size = _player.Stats.AttackSize;
        int damage = (int)Mathf.Round(rawDamage * attack.DamageMultiplier);
        damage = _player.Stats.ConsumePreparedAttackDamage(damage, out string preparedFeedback);

        var previewHit = PreviewPlayerAttack(attack);
        Vector3 aimPoint = previewHit?.HitPoint ?? attack.Volume.BuildTransform(
            _player.AttackOrigin,
            _player.CombatAimDirection,
            reach,
            size).Origin;

        _player.ShowAttackTelegraph(attack, reach, size, isHeavy);
        _player.PlayAttackAnimation(previewHit?.Target?.CombatNode?.GlobalPosition ?? aimPoint, isHeavy);

        _pendingPlayerAttack = new PendingPlayerAttack(
            attack,
            damage,
            CombineFeedback(preparedFeedback, extraFeedback),
            missText,
            _player.CombatAimDirection,
            reach,
            size,
            isHeavy);
        _playerAttackCooldownRemaining = attack.Timeline.TotalSeconds;

        if (attack.Timeline.WindupSeconds <= 0.001f)
            ResolvePendingPlayerAttack();

        return true;
    }

    private void AdvancePendingPlayerAttack(float delta)
    {
        if (_pendingPlayerAttack == null)
            return;

        _pendingPlayerAttack.WindupRemaining = Mathf.Max(0.0f, _pendingPlayerAttack.WindupRemaining - delta);
        if (!_pendingPlayerAttack.Resolved && _pendingPlayerAttack.WindupRemaining <= 0.001f)
            ResolvePendingPlayerAttack();

        _pendingPlayerAttack.TotalRemaining = Mathf.Max(0.0f, _pendingPlayerAttack.TotalRemaining - delta);
        if (_pendingPlayerAttack.TotalRemaining <= 0.001f)
            _pendingPlayerAttack = null;
    }

    private void ResolvePendingPlayerAttack()
    {
        if (_pendingPlayerAttack == null || _pendingPlayerAttack.Resolved || _player == null)
            return;

        _pendingPlayerAttack.Resolved = true;
        var query = new AttackQuery(
            _player,
            _pendingPlayerAttack.Attack,
            _player.AttackOrigin,
            _pendingPlayerAttack.AimDirection,
            _pendingPlayerAttack.AttackReach,
            _pendingPlayerAttack.AttackSize,
            GameState.CurrentRoom);
        var hits = _queryService.QueryTargets(query);
        if (hits.Length == 0)
        {
            Vector3 missPoint = _pendingPlayerAttack.Attack.Volume.BuildTransform(
                _player.AttackOrigin,
                _pendingPlayerAttack.AimDirection,
                _pendingPlayerAttack.AttackReach,
                _pendingPlayerAttack.AttackSize).Origin;
            _target = null;
            _presentation?.ShowFloatingText(missPoint + Vector3.Up * 0.4f, "Miss", Palette.TextDisabled);
            EmitCombatFeedback(_pendingPlayerAttack.MissText);
            return;
        }

        var feedbackParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_pendingPlayerAttack.ExtraFeedback))
            feedbackParts.Add(_pendingPlayerAttack.ExtraFeedback);

        foreach (var hit in hits)
        {
            if (hit.Target is not Enemy enemy || !IsValidEnemy(enemy))
                continue;

            enemy.EnsureCombatStarted();
            _target ??= enemy;
            string hitFeedback = ResolveDamageAgainstEnemy(enemy, _pendingPlayerAttack.Damage);
            if (!string.IsNullOrWhiteSpace(hitFeedback))
                feedbackParts.Add(hitFeedback);
        }

        EmitCombatFeedback(string.Join("  ", feedbackParts.Where(text => !string.IsNullOrWhiteSpace(text)).Distinct()));
    }

    private AttackHitResult? PreviewPlayerAttack(AttackDefinition attack)
    {
        if (_player == null || attack.Equals(default(AttackDefinition)))
            return null;

        var query = new AttackQuery(
            _player,
            attack,
            _player.AttackOrigin,
            _player.CombatAimDirection,
            _player.Stats.AttackReach,
            _player.Stats.AttackSize,
            GameState.CurrentRoom,
            IsPreview: true);
        return _queryService.PreviewBestTarget(query);
    }

    private AttackQuery BuildEnemyAttackQuery(Enemy enemy, bool isPreview)
    {
        Vector3 origin = enemy.AttackOrigin;
        Vector3 direction = _player != null
            ? (_player.Hurtbox?.GlobalPosition ?? _player.GlobalPosition) - origin
            : Vector3.Forward;

        return new AttackQuery(
            enemy,
            enemy.CombatProfile.Attack,
            origin,
            direction,
            enemy.AttackReach,
            1.0f,
            enemy.ZoneRoom,
            isPreview);
    }

    private string ResolveDamageAgainstEnemy(Enemy enemy, int damage)
    {
        if (!IsValidEnemy(enemy) || _player == null)
            return string.Empty;

        LastKillWasBoss = false;
        LastKillWasElite = false;
        LastKillItemDrop = null;
        LastKillRoom = 0;

        var result = enemy.ResolveIncomingDamage(damage, _player);
        GameState.RecordDamageDone(result.Damage);

        if (result.Damage > 0)
        {
            _presentation?.ShowDamageNumber(enemy.GlobalPosition + Vector3.Up * 0.6f, result.Damage, false);
            enemy.PlayHitAnimation();
            AudioManager.Instance?.PlayHit();
        }

        if (result.RetaliationDamage > 0)
        {
            _player.PlayHitReaction();
            _presentation?.ShowDamageNumber(_player.GlobalPosition + Vector3.Up * 0.7f, result.RetaliationDamage, true);
            AudioManager.Instance?.PlayPlayerHit();
        }

        _presentation?.TriggerShake(result.Damage > 0 || result.RetaliationDamage > 0 ? 0.14f : 0.06f);

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

        return result.BuildFeedbackText();
    }

    private bool IsValidEnemy(Enemy enemy)
    {
        return enemy != null && IsInstanceValid(enemy) && !enemy.IsDead;
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

    private sealed class PendingPlayerAttack
    {
        public PendingPlayerAttack(
            AttackDefinition attack,
            int damage,
            string extraFeedback,
            string missText,
            Vector3 aimDirection,
            float attackReach,
            float attackSize,
            bool isHeavy)
        {
            Attack = attack;
            Damage = damage;
            ExtraFeedback = extraFeedback;
            MissText = missText;
            AimDirection = aimDirection;
            AttackReach = attackReach;
            AttackSize = attackSize;
            IsHeavy = isHeavy;
            WindupRemaining = attack.Timeline.WindupSeconds;
            TotalRemaining = attack.Timeline.TotalSeconds;
        }

        public AttackDefinition Attack { get; }
        public int Damage { get; }
        public string ExtraFeedback { get; }
        public string MissText { get; }
        public Vector3 AimDirection { get; }
        public float AttackReach { get; }
        public float AttackSize { get; }
        public bool IsHeavy { get; }
        public float WindupRemaining { get; set; }
        public float TotalRemaining { get; set; }
        public bool Resolved { get; set; }
    }
}
