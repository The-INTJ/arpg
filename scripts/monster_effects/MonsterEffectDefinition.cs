using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public delegate void MonsterEffectLifecycleHook(MonsterEffectInstance instance);
public delegate void MonsterEffectIncomingDamageHook(MonsterEffectInstance instance, MonsterIncomingDamageContext context);
public delegate void MonsterEffectOutgoingDamageHook(MonsterEffectInstance instance, MonsterOutgoingDamageContext context);
public delegate string MonsterEffectDescriptionFormatter(int tier);

public partial class MonsterEffectDefinition
{
    private readonly int[] _threatByTier;
    private readonly string[] _incompatibleEffectIds;
    private readonly MonsterEffectDescriptionFormatter _descriptionFormatter;

    public string Id { get; }
    public string Name { get; }
    public string BadgeText { get; }
    public Color BadgeColor { get; }
    public float BaseWeight { get; }
    public MonsterEffectTag Tags { get; }
    public int ResolutionPriority { get; }
    public bool AllowDuplicates { get; }
    public IReadOnlyList<string> IncompatibleEffectIds => _incompatibleEffectIds;
    public int MaxTier => _threatByTier.Length - 1;

    public MonsterEffectLifecycleHook OnCombatStarted { get; }
    public MonsterEffectLifecycleHook OnOwnerTurnStarted { get; }
    public MonsterEffectLifecycleHook OnOwnerTurnEnded { get; }
    public MonsterEffectIncomingDamageHook OnIncomingDamage { get; }
    public MonsterEffectOutgoingDamageHook OnOutgoingDamage { get; }

    public MonsterEffectDefinition(
        string id,
        string name,
        string badgeText,
        Color badgeColor,
        float baseWeight,
        MonsterEffectTag tags,
        int resolutionPriority,
        IEnumerable<int> threatByTier,
        MonsterEffectDescriptionFormatter descriptionFormatter = null,
        bool allowDuplicates = false,
        IEnumerable<string> incompatibleEffectIds = null,
        MonsterEffectLifecycleHook onCombatStarted = null,
        MonsterEffectLifecycleHook onOwnerTurnStarted = null,
        MonsterEffectLifecycleHook onOwnerTurnEnded = null,
        MonsterEffectIncomingDamageHook onIncomingDamage = null,
        MonsterEffectOutgoingDamageHook onOutgoingDamage = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Monster effects need an id.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Monster effects need a display name.", nameof(name));
        if (string.IsNullOrWhiteSpace(badgeText))
            throw new ArgumentException("Monster effects need badge text.", nameof(badgeText));

        _threatByTier = threatByTier?.ToArray() ?? Array.Empty<int>();
        if (_threatByTier.Length == 0)
            throw new ArgumentException("Monster effects need at least one threat value.", nameof(threatByTier));

        Id = id;
        Name = name;
        BadgeText = badgeText;
        BadgeColor = badgeColor;
        BaseWeight = Math.Max(0.0f, baseWeight);
        Tags = tags;
        ResolutionPriority = resolutionPriority;
        AllowDuplicates = allowDuplicates;
        _descriptionFormatter = descriptionFormatter;
        _incompatibleEffectIds = incompatibleEffectIds?
            .Where(idValue => !string.IsNullOrWhiteSpace(idValue))
            .Distinct()
            .ToArray() ?? Array.Empty<string>();

        OnCombatStarted = onCombatStarted;
        OnOwnerTurnStarted = onOwnerTurnStarted;
        OnOwnerTurnEnded = onOwnerTurnEnded;
        OnIncomingDamage = onIncomingDamage;
        OnOutgoingDamage = onOutgoingDamage;
    }

    public int GetThreatCost(int tier)
    {
        int clampedTier = Math.Clamp(tier, 0, MaxTier);
        return _threatByTier[clampedTier];
    }

    public string DescribeTier(int tier)
    {
        int clampedTier = Math.Clamp(tier, 0, MaxTier);
        return _descriptionFormatter?.Invoke(clampedTier) ?? Name;
    }

    public bool AllowsCoexistenceWith(MonsterEffectDefinition other)
    {
        if (other == null)
            return true;

        return Array.IndexOf(_incompatibleEffectIds, other.Id) < 0;
    }
}
