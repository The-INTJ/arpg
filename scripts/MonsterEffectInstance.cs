using System.Collections.Generic;

namespace ARPG;

public partial class MonsterEffectInstance
{
    private readonly Dictionary<string, int> _state = new();

    public Enemy Owner { get; }
    public MonsterEffectDefinition Definition { get; }
    public int Tier { get; }
    public bool IsExpired { get; private set; }
    public int OwnerTurnsStarted { get; private set; }
    public int OwnerTurnsEnded { get; private set; }
    public int TriggerCount { get; private set; }
    public int LastTriggerTurn { get; private set; } = -1;

    public MonsterEffectInstance(Enemy owner, MonsterEffectDefinition definition, int tier)
    {
        Owner = owner;
        Definition = definition;
        Tier = tier;
    }

    public void MarkExpired()
    {
        IsExpired = true;
    }

    public int GetState(string key, int defaultValue = 0)
    {
        return _state.TryGetValue(key, out int value) ? value : defaultValue;
    }

    public void SetState(string key, int value)
    {
        _state[key] = value;
    }

    public int IncrementState(string key, int amount = 1)
    {
        int newValue = GetState(key) + amount;
        _state[key] = newValue;
        return newValue;
    }

    public void RecordTrigger()
    {
        TriggerCount++;
        LastTriggerTurn = OwnerTurnsStarted;
    }

    public void CombatStarted()
    {
        if (IsExpired)
            return;

        Definition.OnCombatStarted?.Invoke(this);
    }

    public void OwnerTurnStarted()
    {
        if (IsExpired)
            return;

        OwnerTurnsStarted++;
        Definition.OnOwnerTurnStarted?.Invoke(this);
    }

    public void OwnerTurnEnded()
    {
        if (IsExpired)
            return;

        OwnerTurnsEnded++;
        Definition.OnOwnerTurnEnded?.Invoke(this);
    }

    public void ApplyIncomingDamage(MonsterIncomingDamageContext context)
    {
        if (IsExpired)
            return;

        Definition.OnIncomingDamage?.Invoke(this, context);
    }

    public void ApplyOutgoingDamage(MonsterOutgoingDamageContext context)
    {
        if (IsExpired)
            return;

        Definition.OnOutgoingDamage?.Invoke(this, context);
    }
}
