using System.Collections.Generic;
using System.Linq;

namespace ARPG;

/// <summary>
/// Stages target choices for a modifier before the player confirms the apply action.
/// This lets future multi-effect upgrades ask for more than one stat selection cleanly.
/// </summary>
public partial class ModifierAssignmentPlan
{
    private readonly StatTarget?[] _selectedTargets;

    public Modifier Modifier { get; }
    public IReadOnlyList<StatTarget?> SelectedTargets => _selectedTargets;
    public bool IsComplete => NextEffectIndex < 0;
    public bool HasAnyAssignments => _selectedTargets.Any(target => target.HasValue);

    public int NextEffectIndex
    {
        get
        {
            for (int i = 0; i < _selectedTargets.Length; i++)
            {
                if (!_selectedTargets[i].HasValue)
                    return i;
            }

            return -1;
        }
    }

    public ModifierAssignmentPlan(Modifier modifier)
    {
        Modifier = modifier;
        _selectedTargets = new StatTarget?[modifier.Effects.Count];
    }

    private ModifierAssignmentPlan(Modifier modifier, StatTarget?[] selectedTargets)
    {
        Modifier = modifier;
        _selectedTargets = selectedTargets;
    }

    public ModifierAssignmentPlan Clone() => new(Modifier, (StatTarget?[])_selectedTargets.Clone());

    public void Reset()
    {
        for (int i = 0; i < _selectedTargets.Length; i++)
            _selectedTargets[i] = null;
    }

    public ModifierEffect GetEffect(int index) => Modifier.Effects[index];

    public ModifierEffect GetNextEffect()
    {
        int nextIndex = NextEffectIndex;
        return nextIndex >= 0 ? Modifier.Effects[nextIndex] : null;
    }

    public bool CanAssignNext(StatTarget target)
    {
        var nextEffect = GetNextEffect();
        return nextEffect != null && nextEffect.AllowsTarget(target, GetChosenTargets());
    }

    public bool TryAssignNext(StatTarget target)
    {
        int nextIndex = NextEffectIndex;
        if (nextIndex < 0 || !CanAssignNext(target))
            return false;

        _selectedTargets[nextIndex] = target;
        return true;
    }

    public AppliedModifierEffect[] BuildAssignedEffects()
    {
        var effects = new List<AppliedModifierEffect>();
        for (int i = 0; i < _selectedTargets.Length; i++)
        {
            if (!_selectedTargets[i].HasValue)
                continue;

            effects.Add(new AppliedModifierEffect(Modifier, Modifier.Effects[i], _selectedTargets[i].Value));
        }

        return effects.ToArray();
    }

    public AppliedModifierEffect[] BuildAppliedEffects()
    {
        if (!IsComplete)
            return BuildAssignedEffects();

        var effects = new AppliedModifierEffect[_selectedTargets.Length];
        for (int i = 0; i < _selectedTargets.Length; i++)
            effects[i] = new AppliedModifierEffect(Modifier, Modifier.Effects[i], _selectedTargets[i].Value);

        return effects;
    }

    public AppliedModifierEffect[] BuildPreviewEffectsIfNextAssigned(StatTarget target)
    {
        var assignedEffects = new List<AppliedModifierEffect>(BuildAssignedEffects());
        var nextEffect = GetNextEffect();
        if (nextEffect != null && nextEffect.AllowsTarget(target, GetChosenTargets()))
            assignedEffects.Add(new AppliedModifierEffect(Modifier, nextEffect, target));

        return assignedEffects.ToArray();
    }

    private IReadOnlyCollection<StatTarget> GetChosenTargets()
    {
        var chosenTargets = new List<StatTarget>();
        foreach (var target in _selectedTargets)
        {
            if (target.HasValue)
                chosenTargets.Add(target.Value);
        }

        return chosenTargets;
    }
}
