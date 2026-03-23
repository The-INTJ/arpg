using System;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Holds all weapon-applied modifiers for a single stat target.
/// The UI can treat each channel as a stable slot while still allowing stacking.
/// </summary>
public partial class WeaponStatChannel
{
    private readonly List<Modifier> _modifiers = new();

    public StatTarget Target { get; }
    public IReadOnlyList<Modifier> Modifiers => _modifiers;
    public bool IsEmpty => _modifiers.Count == 0;
    public string Summary => Modifier.DescribeCombined(Target, _modifiers);

    public WeaponStatChannel(StatTarget target)
    {
        Target = target;
    }

    public void Add(Modifier modifier)
    {
        if (modifier == null)
            return;

        if (modifier.Target != Target)
            throw new ArgumentException($"Modifier target {modifier.Target} does not match channel target {Target}.");

        _modifiers.Add(modifier);
    }

    public bool Remove(Modifier modifier)
    {
        if (modifier == null)
            return false;

        return _modifiers.Remove(modifier);
    }

    public string SummaryWith(Modifier modifier)
    {
        if (modifier == null || modifier.Target != Target)
            return Summary;

        var previewModifiers = new List<Modifier>(_modifiers.Count + 1);
        previewModifiers.AddRange(_modifiers);
        previewModifiers.Add(modifier);
        return Modifier.DescribeCombined(Target, previewModifiers);
    }
}
