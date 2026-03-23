using System;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

public partial class MonsterEffectTriggerRecord
{
    public MonsterEffectInstance Instance { get; }
    public string EffectId => Instance?.Definition.Id ?? string.Empty;
    public string Message { get; }

    public MonsterEffectTriggerRecord(MonsterEffectInstance instance, string message)
    {
        Instance = instance;
        Message = message;
    }
}

public abstract partial class MonsterEffectResolutionContext
{
    private readonly List<MonsterEffectTriggerRecord> _triggers = new();

    public IReadOnlyList<MonsterEffectTriggerRecord> Triggers => _triggers;

    public void Trigger(MonsterEffectInstance instance, string message)
    {
        if (instance == null || string.IsNullOrWhiteSpace(message))
            return;

        instance.RecordTrigger();
        _triggers.Add(new MonsterEffectTriggerRecord(instance, message));
    }

    public string BuildFeedbackText()
    {
        return string.Join("  ", _triggers.Select(trigger => trigger.Message).Distinct());
    }
}

public partial class MonsterIncomingDamageContext : MonsterEffectResolutionContext
{

    public Enemy Target { get; }
    public PlayerController Attacker { get; }
    public int BaseDamage { get; }
    public int Damage { get; set; }
    public int RetaliationDamage { get; private set; }

    public MonsterIncomingDamageContext(Enemy target, PlayerController attacker, int baseDamage)
    {
        Target = target;
        Attacker = attacker;
        BaseDamage = Math.Max(0, baseDamage);
        Damage = BaseDamage;
    }

    public void AddRetaliationDamage(int amount)
    {
        RetaliationDamage += Math.Max(0, amount);
    }
}

public partial class MonsterOutgoingDamageContext : MonsterEffectResolutionContext
{
    public Enemy Attacker { get; }
    public PlayerController Target { get; }
    public int BaseDamage { get; }
    public int Damage { get; set; }

    public MonsterOutgoingDamageContext(Enemy attacker, PlayerController target, int baseDamage)
    {
        Attacker = attacker;
        Target = target;
        BaseDamage = Math.Max(0, baseDamage);
        Damage = BaseDamage;
    }
}
