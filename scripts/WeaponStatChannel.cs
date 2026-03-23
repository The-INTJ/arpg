using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds all weapon-applied modifiers for a single stat target.
/// The UI can treat each channel as a stable slot while still allowing stacking.
/// </summary>
public partial class WeaponStatChannel
{
    private readonly List<AppliedModifierEffect> _effects = new();

    public StatTarget Target { get; }
    public IReadOnlyList<AppliedModifierEffect> Effects => _effects;
    public bool IsEmpty => _effects.Count == 0;
    public string Summary => AppliedModifierEffect.DescribeCombined(Target, _effects);

    public WeaponStatChannel(StatTarget target)
    {
        Target = target;
    }

    public void Add(AppliedModifierEffect effect)
    {
        if (effect == null)
            return;

        if (effect.Target != Target)
            throw new ArgumentException($"Applied effect target {effect.Target} does not match channel target {Target}.");

        _effects.Add(effect);
    }

    public bool Remove(AppliedModifierEffect effect)
    {
        if (effect == null)
            return false;

        return _effects.Remove(effect);
    }

    public string SummaryWith(IEnumerable<AppliedModifierEffect> previewEffects)
    {
        if (previewEffects == null)
            return Summary;

        var combinedEffects = new List<AppliedModifierEffect>(_effects.Count);
        combinedEffects.AddRange(_effects);
        foreach (var effect in previewEffects)
        {
            if (effect != null && effect.Target == Target)
                combinedEffects.Add(effect);
        }

        return AppliedModifierEffect.DescribeCombined(Target, combinedEffects);
    }
}
