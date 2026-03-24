using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    private readonly List<MonsterEffectInstance> _monsterEffects = new();
    private readonly Dictionary<MonsterEffectInstance, Node3D> _effectBadges = new();
    private readonly Dictionary<MonsterEffectInstance, Color> _effectBadgeColors = new();
    private Node3D _effectBadgeAnchor;
    private Tween _visualTween;

    public int MaxHp = 11;
    public int Hp = 11;
    public int AttackDamage = 3;
    public float SightRange = 4.0f;
    public bool IsBoss { get; private set; }
    public bool IsElite { get; private set; }
    public string VariantName { get; set; }
    public InventoryItem ItemDrop { get; private set; }

    public bool HasAggro { get; set; }

    public float HpPercent => MaxHp > 0 ? (float)Hp / MaxHp : 0f;
    public bool IsDead => Hp <= 0;
    public IReadOnlyList<MonsterEffectInstance> MonsterEffects => _monsterEffects;
    public string DisplayName => IsBoss
        ? "Boss"
        : IsElite
            ? $"Elite {VariantName ?? "Enemy"}"
            : (VariantName ?? "Enemy");

    /// <summary>
    /// Scale this enemy's stats for the given room number (1-based).
    /// Room 1 = baseline, Room 2 = tougher, Room 3 = hardest.
    /// </summary>
    public void ScaleForRoom(int room)
    {
        float hpMult = 1.0f + (room - 1) * 0.55f;   // 1.0, 1.55, 2.1
        float atkMult = 1.0f + (room - 1) * 0.4f;   // 1.0, 1.4, 1.8
        MaxHp = (int)(MaxHp * hpMult);
        Hp = MaxHp;
        AttackDamage = (int)(AttackDamage * atkMult);
    }

    /// <summary>
    /// Promote this enemy to a boss. Significantly more HP and damage, larger sprite.
    /// </summary>
    public void MakeBoss()
    {
        IsBoss = true;
        MaxHp = (int)(MaxHp * 2.5f);
        Hp = MaxHp;
        AttackDamage = (int)(AttackDamage * 1.8f);
        SightRange = 6.0f;
    }

    public void MakeElite()
    {
        IsElite = true;
        MaxHp = (int)(MaxHp * 1.7f);
        Hp = MaxHp;
        AttackDamage = (int)(AttackDamage * 1.4f);
        SightRange = 5.0f;
    }

    public void AssignItemDrop(InventoryItem item)
    {
        ItemDrop = item;
    }

    public void TakeDamage(int amount)
    {
        Hp = Mathf.Max(0, Hp - amount);
    }

    public void Heal(int amount)
    {
        Hp = Mathf.Min(MaxHp, Hp + Mathf.Max(0, amount));
    }

    public string GetEffectInfoText()
    {
        if (_monsterEffects.Count == 0)
            return "Effects: none";

        return string.Join("\n", _monsterEffects.Select(effect =>
            $"{effect.Definition.BadgeText} T{effect.Tier} {effect.Definition.Name}: {effect.Definition.DescribeTier(effect.Tier)}"));
    }

    public void Die()
    {
        QueueFree();
    }

    public void PlayAttackAnimation(Vector3 targetPosition)
    {
        var sprite = GetVisualSprite();
        if (sprite == null)
            return;

        Vector3 basePosition = GetSpriteBasePosition();
        Vector3 direction = (targetPosition - GlobalPosition).Normalized();
        Vector3 localOffset = ToLocal(GlobalPosition + direction * (IsBoss ? 0.22f : 0.14f));
        Vector3 lungePosition = basePosition + new Vector3(localOffset.X, 0, localOffset.Z);

        ResetVisualTween(sprite, basePosition);
        _visualTween = CreateTween();
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(sprite, "position", lungePosition, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(sprite, "scale", new Vector3(IsBoss ? 1.16f : 1.1f, IsBoss ? 1.16f : 1.1f, 1), 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(false);
        _visualTween.TweenProperty(sprite, "position", basePosition, 0.14f)
            .SetTrans(Tween.TransitionType.Bounce)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(sprite, "scale", Vector3.One, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    public void PlayHitAnimation()
    {
        var sprite = GetVisualSprite();
        if (sprite == null)
            return;

        Vector3 basePosition = GetSpriteBasePosition();
        ResetVisualTween(sprite, basePosition);
        _visualTween = CreateTween();
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(sprite, "modulate", new Color(1.0f, 0.72f, 0.72f), 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.TweenProperty(sprite, "scale", new Vector3(1.14f, 0.92f, 1), 0.06f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(false);
        _visualTween.TweenProperty(sprite, "scale", Vector3.One, 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _visualTween.SetParallel(true);
        _visualTween.TweenProperty(sprite, "modulate", Colors.White, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        _visualTween.TweenProperty(sprite, "position", basePosition, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    public void SetMonsterEffects(MonsterEffectAssignmentPlan plan)
    {
        _monsterEffects.Clear();
        if (plan != null)
        {
            foreach (var instance in plan.CreateInstances(this))
                _monsterEffects.Add(instance);
        }

        RefreshEffectBadges();
    }

    public void OnCombatStarted()
    {
        foreach (var effect in _monsterEffects)
            effect.CombatStarted();
    }

    public void OnOwnerTurnStarted()
    {
        foreach (var effect in _monsterEffects)
            effect.OwnerTurnStarted();
    }

    public void OnOwnerTurnEnded()
    {
        foreach (var effect in _monsterEffects)
            effect.OwnerTurnEnded();

        CleanupExpiredEffects();
    }

    public MonsterIncomingDamageContext ResolveIncomingDamage(int amount, PlayerController attacker)
    {
        var context = new MonsterIncomingDamageContext(this, attacker, amount);
        foreach (var effect in _monsterEffects)
            effect.ApplyIncomingDamage(context);

        context.Damage = Mathf.Max(0, context.Damage);
        if (context.Damage > 0)
            TakeDamage(context.Damage);

        if (attacker != null && context.RetaliationDamage > 0)
        {
            int finalRetaliationDamage = attacker.Stats.ResolveIncomingDamage(context.RetaliationDamage, out string wardFeedback);
            context.SetRetaliationDamage(finalRetaliationDamage);
            context.AddNote(wardFeedback);
            if (finalRetaliationDamage > 0)
                attacker.Hp = Mathf.Max(0, attacker.Hp - finalRetaliationDamage);
        }

        FlashTriggeredBadges(context.Triggers);
        CleanupExpiredEffects();
        return context;
    }

    public MonsterOutgoingDamageContext ResolveOutgoingDamage(PlayerController player)
    {
        var context = new MonsterOutgoingDamageContext(this, player, AttackDamage);
        foreach (var effect in _monsterEffects)
            effect.ApplyOutgoingDamage(context);

        context.Damage = Mathf.Max(0, context.Damage);
        if (player != null)
        {
            int finalDamage = player.Stats.ResolveIncomingDamage(context.Damage, out string wardFeedback);
            context.Damage = finalDamage;
            context.AddNote(wardFeedback);

            if (finalDamage > 0)
                player.Hp = Mathf.Max(0, player.Hp - finalDamage);
        }
        if (context.Damage > 0 && context.HealingAmount > 0)
            Heal(context.HealingAmount);

        FlashTriggeredBadges(context.Triggers);
        CleanupExpiredEffects();
        return context;
    }

    /// <summary>
    /// Shows a "!" indicator above this enemy. Called by GameManager on aggro.
    /// </summary>
    public void ShowAggroIndicator()
    {
        if (HasAggro) return;
        HasAggro = true;

        var label = new Label3D();
        label.Text = IsBoss ? "!!!" : IsElite ? "!!" : "!";
        label.FontSize = IsBoss ? 28 : IsElite ? 24 : 20;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.NoDepthTest = false;
        label.FixedSize = true;
        label.PixelSize = 0.005f;
        label.Modulate = new Color(1.0f, 0.3f, 0.2f);
        label.OutlineSize = 6;
        label.OutlineModulate = new Color(0, 0, 0);
        label.Position = new Vector3(0, IsBoss ? 0.85f : IsElite ? 0.65f : 0.55f, 0);
        label.Name = "AggroIndicator";
        AddChild(label);

        var tween = CreateTween();
        tween.TweenInterval(0.6f);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }

    private void CleanupExpiredEffects()
    {
        var expiredEffects = new List<MonsterEffectInstance>();
        for (int i = _monsterEffects.Count - 1; i >= 0; i--)
        {
            if (!_monsterEffects[i].IsExpired)
                continue;

            expiredEffects.Add(_monsterEffects[i]);
            _monsterEffects.RemoveAt(i);
        }

        if (expiredEffects.Count == 0)
            return;

        foreach (var effect in expiredEffects)
            FadeOutEffectBadge(effect);

        if (!IsInsideTree())
        {
            RefreshEffectBadges();
            return;
        }

        var timer = GetTree().CreateTimer(0.2f);
        timer.Timeout += () =>
        {
            if (IsInsideTree())
                RefreshEffectBadges();
        };
    }

    private void RefreshEffectBadges()
    {
        if (!IsInsideTree())
            return;

        EnsureEffectBadgeAnchor();

        foreach (Node child in _effectBadgeAnchor.GetChildren())
            child.QueueFree();

        _effectBadges.Clear();
        _effectBadgeColors.Clear();

        if (_monsterEffects.Count == 0)
            return;

        for (int i = 0; i < _monsterEffects.Count; i++)
        {
            var effect = _monsterEffects[i];
            var cluster = BuildEffectFlameCluster(effect, i, _monsterEffects.Count);
            _effectBadgeAnchor.AddChild(cluster);

            _effectBadges[effect] = cluster;
            _effectBadgeColors[effect] = GetEffectFlameColor(effect, i);
        }
    }

    private void EnsureEffectBadgeAnchor()
    {
        if (_effectBadgeAnchor != null && IsInstanceValid(_effectBadgeAnchor))
        {
            _effectBadgeAnchor.Position = new Vector3(0, 0.02f, 0);
            return;
        }

        _effectBadgeAnchor = GetNodeOrNull<Node3D>("EffectBadgeAnchor");
        if (_effectBadgeAnchor == null)
        {
            _effectBadgeAnchor = new Node3D();
            _effectBadgeAnchor.Name = "EffectBadgeAnchor";
            AddChild(_effectBadgeAnchor);
        }

        _effectBadgeAnchor.Position = new Vector3(0, 0.02f, 0);
    }

    private void FlashTriggeredBadges(IReadOnlyList<MonsterEffectTriggerRecord> triggers)
    {
        if (triggers == null || triggers.Count == 0)
            return;

        var seen = new HashSet<MonsterEffectInstance>();
        foreach (var trigger in triggers)
        {
            if (trigger?.Instance == null || !seen.Add(trigger.Instance))
                continue;

            FlashEffectBadge(trigger.Instance);
        }
    }

    private void FlashEffectBadge(MonsterEffectInstance effect)
    {
        if (effect == null || !_effectBadges.TryGetValue(effect, out var cluster) || !IsInstanceValid(cluster))
            return;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(cluster, "scale", new Vector3(1.22f, 1.22f, 1.22f), 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        foreach (Node child in cluster.GetChildren())
        {
            if (child is not Sprite3D sprite)
                continue;

            tween.TweenProperty(sprite, "modulate", sprite.Modulate.Lightened(0.2f), 0.08f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        }
        tween.SetParallel(false);
        tween.TweenProperty(cluster, "scale", Vector3.One, 0.12f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        foreach (Node child in cluster.GetChildren())
        {
            if (child is not Sprite3D sprite)
                continue;

            var baseColor = (Color)sprite.GetMeta("base_modulate", sprite.Modulate);
            tween.TweenProperty(sprite, "modulate", baseColor, 0.12f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
        }
    }

    private void FadeOutEffectBadge(MonsterEffectInstance effect)
    {
        if (effect == null || !_effectBadges.TryGetValue(effect, out var cluster) || !IsInstanceValid(cluster))
            return;

        _effectBadges.Remove(effect);
        _effectBadgeColors.Remove(effect);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(cluster, "scale", new Vector3(0.72f, 0.72f, 0.72f), 0.18f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        foreach (Node child in cluster.GetChildren())
        {
            if (child is not Sprite3D sprite)
                continue;

            tween.TweenProperty(sprite, "modulate:a", 0.0f, 0.18f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
        }
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => cluster.QueueFree()));
    }

    private Node3D BuildEffectFlameCluster(MonsterEffectInstance effect, int index, int totalCount)
    {
        var cluster = new Node3D();
        cluster.Name = $"EffectFlame_{effect.Definition.Id}";

        float ringRadius = IsBoss ? 0.42f : IsElite ? 0.34f : 0.28f;
        float angle = Mathf.Tau * index / Mathf.Max(1, totalCount);
        cluster.Position = new Vector3(Mathf.Cos(angle) * ringRadius, 0, Mathf.Sin(angle) * ringRadius);

        Color baseColor = GetEffectFlameColor(effect, index);
        int flameCount = IsBoss ? 4 : 3;
        for (int i = 0; i < flameCount; i++)
        {
            var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateFlameTexture(baseColor, i), IsBoss ? 0.028f : 0.024f);
            sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            sprite.NoDepthTest = false;
            sprite.Position = new Vector3(
                (float)GD.RandRange(-0.03, 0.03),
                0.08f + i * 0.04f + (float)GD.RandRange(-0.01, 0.02),
                (float)GD.RandRange(-0.03, 0.03));
            sprite.Modulate = baseColor;
            sprite.SetMeta("base_modulate", baseColor);
            sprite.SetMeta("base_position", sprite.Position);
            cluster.AddChild(sprite);
            StartFlameLoop(sprite);
        }

        return cluster;
    }

    private void StartFlameLoop(Sprite3D sprite)
    {
        Vector3 basePosition = (Vector3)sprite.GetMeta("base_position", sprite.Position);
        Vector3 tallScale = new(
            (float)GD.RandRange(0.9, 1.05),
            (float)GD.RandRange(1.08, 1.28),
            1);
        Vector3 compactScale = new(
            (float)GD.RandRange(0.92, 1.08),
            (float)GD.RandRange(0.88, 1.02),
            1);
        float upOffset = (float)GD.RandRange(0.03, 0.07);
        float riseDuration = (float)GD.RandRange(0.18, 0.28);
        float fallDuration = (float)GD.RandRange(0.16, 0.24);

        var tween = CreateTween().SetLoops();
        tween.SetParallel(true);
        tween.TweenProperty(sprite, "position:y", basePosition.Y + upOffset, riseDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(sprite, "scale", tallScale, riseDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenProperty(sprite, "position:y", basePosition.Y, fallDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.SetParallel(true);
        tween.TweenProperty(sprite, "scale", compactScale, fallDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private static Color GetEffectFlameColor(MonsterEffectInstance effect, int index)
    {
        if (effect?.Definition != null)
            return effect.Definition.BadgeColor;

        return (index % 3) switch
        {
            0 => new Color(0.97f, 0.34f, 0.12f),
            1 => new Color(0.96f, 0.58f, 0.12f),
            _ => new Color(0.98f, 0.82f, 0.28f),
        };
    }

    private Sprite3D GetVisualSprite()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Sprite3D sprite)
                return sprite;
        }

        return null;
    }

    private Vector3 GetSpriteBasePosition()
    {
        return new Vector3(0, IsBoss ? 0.35f : 0.25f, 0);
    }

    private void ResetVisualTween(Sprite3D sprite, Vector3 basePosition)
    {
        _visualTween?.Kill();
        sprite.Position = basePosition;
        sprite.Scale = Vector3.One;
        sprite.Modulate = Colors.White;
    }
}
