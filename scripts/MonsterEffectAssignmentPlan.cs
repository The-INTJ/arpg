using System.Collections.Generic;

namespace ARPG;

public enum MonsterEffectSource
{
    Granted,
    Optional,
}

public partial class MonsterEffectAssignment
{
    public MonsterEffectDefinition Definition { get; }
    public int Tier { get; }
    public MonsterEffectSource Source { get; }
    public bool ConsumesThreatBudget { get; }
    public int ThreatCost => ConsumesThreatBudget ? Definition.GetThreatCost(Tier) : 0;

    public MonsterEffectAssignment(
        MonsterEffectDefinition definition,
        int tier,
        MonsterEffectSource source,
        bool consumesThreatBudget)
    {
        Definition = definition;
        Tier = tier;
        Source = source;
        ConsumesThreatBudget = consumesThreatBudget;
    }
}

public partial class MonsterEffectAssignmentPlan
{
    private readonly List<MonsterEffectAssignment> _assignments = new();

    public IReadOnlyList<MonsterEffectAssignment> Assignments => _assignments;
    public int Count => _assignments.Count;
    public int TotalThreat { get; private set; }

    public bool ContainsEffect(string effectId)
    {
        foreach (var assignment in _assignments)
        {
            if (assignment.Definition.Id == effectId)
                return true;
        }

        return false;
    }

    public void Add(MonsterEffectAssignment assignment)
    {
        if (assignment == null)
            return;

        _assignments.Add(assignment);
        TotalThreat += assignment.ThreatCost;
    }

    public MonsterEffectInstance[] CreateInstances(Enemy owner)
    {
        var instances = new MonsterEffectInstance[_assignments.Count];
        for (int i = 0; i < _assignments.Count; i++)
            instances[i] = new MonsterEffectInstance(owner, _assignments[i].Definition, _assignments[i].Tier);

        return instances;
    }
}
