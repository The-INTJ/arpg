using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    private readonly List<MonsterEffectInstance> _monsterEffects = new();
    private readonly Dictionary<MonsterEffectInstance, Label3D> _effectBadges = new();
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

        float spacing = IsBoss ? 0.25f : 0.2f;
        float startX = -spacing * (_monsterEffects.Count - 1) * 0.5f;
        for (int i = 0; i < _monsterEffects.Count; i++)
        {
            var effect = _monsterEffects[i];
            var label = new Label3D();
            label.Name = $"EffectBadge_{effect.Definition.Id}";
            label.Text = effect.Tier > 0
                ? $"{effect.Definition.BadgeText}{effect.Tier}"
                : effect.Definition.BadgeText;
            label.FontSize = IsBoss ? 12 : 10;
            label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            label.NoDepthTest = false;
            label.FixedSize = true;
            label.PixelSize = IsBoss ? 0.003f : 0.0025f;
            label.Modulate = effect.Definition.BadgeColor;
            label.OutlineSize = 3;
            label.OutlineModulate = Palette.BgDark;
            label.Position = new Vector3(startX + i * spacing, 0, 0);
            _effectBadgeAnchor.AddChild(label);

            _effectBadges[effect] = label;
            _effectBadgeColors[effect] = effect.Definition.BadgeColor;
        }
    }

    private void EnsureEffectBadgeAnchor()
    {
        if (_effectBadgeAnchor != null && IsInstanceValid(_effectBadgeAnchor))
        {
            _effectBadgeAnchor.Position = new Vector3(0, IsBoss ? 0.75f : IsElite ? 0.62f : 0.55f, 0);
            return;
        }

        _effectBadgeAnchor = GetNodeOrNull<Node3D>("EffectBadgeAnchor");
        if (_effectBadgeAnchor == null)
        {
            _effectBadgeAnchor = new Node3D();
            _effectBadgeAnchor.Name = "EffectBadgeAnchor";
            AddChild(_effectBadgeAnchor);
        }

        _effectBadgeAnchor.Position = new Vector3(0, IsBoss ? 0.75f : IsElite ? 0.62f : 0.55f, 0);
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
        if (effect == null || !_effectBadges.TryGetValue(effect, out var label) || !IsInstanceValid(label))
            return;
        if (!_effectBadgeColors.TryGetValue(effect, out var baseColor))
            baseColor = Palette.TextLight;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(label, "scale", new Vector3(1.2f, 1.2f, 1.2f), 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "modulate", Palette.TextLight, 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenProperty(label, "scale", Vector3.One, 0.12f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(label, "modulate", baseColor, 0.12f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    private void FadeOutEffectBadge(MonsterEffectInstance effect)
    {
        if (effect == null || !_effectBadges.TryGetValue(effect, out var label) || !IsInstanceValid(label))
            return;

        _effectBadges.Remove(effect);
        _effectBadgeColors.Remove(effect);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.18f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(label, "scale", new Vector3(0.75f, 0.75f, 0.75f), 0.18f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
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
