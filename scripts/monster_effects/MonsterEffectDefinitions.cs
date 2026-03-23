using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ARPG;

public static partial class MonsterEffectDefinitions
{
    private static readonly MonsterEffectDefinition[] Definitions =
    {
        new MonsterEffectDefinition(
            id: "invulnerable",
            name: "Invulnerable",
            badgeText: "INV",
            badgeColor: Palette.EffectInvulnerable,
            baseWeight: 1.0f,
            tags: MonsterEffectTag.Defense | MonsterEffectTag.Opener | MonsterEffectTag.PunishesBurst | MonsterEffectTag.BossSafe,
            resolutionPriority: 10,
            threatByTier: new[] { 6, 10, 15 },
            descriptionFormatter: DescribeInvulnerable,
            incompatibleEffectIds: new[] { "enraged" },
            onIncomingDamage: OnInvulnerableIncomingDamage),
        new MonsterEffectDefinition(
            id: "bulwark",
            name: "Bulwark",
            badgeText: "BLW",
            badgeColor: Palette.EffectBulwark,
            baseWeight: 1.0f,
            tags: MonsterEffectTag.Defense | MonsterEffectTag.Attrition | MonsterEffectTag.BossSafe,
            resolutionPriority: 20,
            threatByTier: new[] { 4, 8, 12 },
            descriptionFormatter: DescribeBulwark,
            onCombatStarted: OnBulwarkCombatStarted,
            onOwnerTurnEnded: OnBulwarkTurnEnded,
            onIncomingDamage: OnBulwarkIncomingDamage),
        new MonsterEffectDefinition(
            id: "thorns",
            name: "Thorns",
            badgeText: "THN",
            badgeColor: Palette.EffectThorns,
            baseWeight: 1.0f,
            tags: MonsterEffectTag.Retaliation | MonsterEffectTag.PunishesBurst,
            resolutionPriority: 100,
            threatByTier: new[] { 2, 5, 8 },
            descriptionFormatter: DescribeThorns,
            onIncomingDamage: OnThornsIncomingDamage),
        new MonsterEffectDefinition(
            id: "enraged",
            name: "Enraged",
            badgeText: "ENR",
            badgeColor: Palette.EffectEnraged,
            baseWeight: 0.9f,
            tags: MonsterEffectTag.Opener | MonsterEffectTag.PunishesSustain,
            resolutionPriority: 60,
            threatByTier: new[] { 3, 7, 11 },
            descriptionFormatter: DescribeEnraged,
            incompatibleEffectIds: new[] { "invulnerable" },
            onOutgoingDamage: OnEnragedOutgoingDamage),
        new MonsterEffectDefinition(
            id: "leech",
            name: "Leech",
            badgeText: "LEC",
            badgeColor: Palette.EffectLeech,
            baseWeight: 0.95f,
            tags: MonsterEffectTag.Attrition | MonsterEffectTag.PunishesSustain | MonsterEffectTag.BossSafe,
            resolutionPriority: 120,
            threatByTier: new[] { 3, 6, 10 },
            descriptionFormatter: DescribeLeech,
            onOutgoingDamage: OnLeechOutgoingDamage),
    };

    private static readonly Dictionary<string, MonsterEffectDefinition> DefinitionsById =
        Definitions.ToDictionary(definition => definition.Id);

    public static IReadOnlyList<MonsterEffectDefinition> All => Definitions;

    public static MonsterEffectDefinition Get(string id)
    {
        return DefinitionsById[id];
    }

    public static MonsterEffectDefinition GetOrNull(string id)
    {
        return DefinitionsById.TryGetValue(id, out var definition) ? definition : null;
    }

    private static void OnInvulnerableIncomingDamage(MonsterEffectInstance instance, MonsterIncomingDamageContext context)
    {
        if (context.Damage <= 0 || !IsInvulnerableActive(instance))
            return;

        context.Damage = 0;
        context.Trigger(instance, "Invulnerable negated the hit.");

        if (instance.Tier == 0)
            instance.MarkExpired();
    }

    private static void OnBulwarkCombatStarted(MonsterEffectInstance instance)
    {
        instance.SetState("bulwark_block", GetBulwarkBlock(instance.Tier));
    }

    private static void OnBulwarkIncomingDamage(MonsterEffectInstance instance, MonsterIncomingDamageContext context)
    {
        int block = instance.GetState("bulwark_block");
        if (block <= 0 || context.Damage <= 0)
            return;

        int absorbed = Mathf.Min(block, context.Damage);
        instance.SetState("bulwark_block", block - absorbed);
        context.Damage -= absorbed;
        context.Trigger(instance, $"Bulwark absorbed {absorbed} damage.");
    }

    private static void OnBulwarkTurnEnded(MonsterEffectInstance instance)
    {
        if (instance.OwnerTurnsEnded >= GetBulwarkTurnCount(instance.Tier))
        {
            instance.MarkExpired();
            return;
        }

        instance.SetState("bulwark_block", GetBulwarkBlock(instance.Tier));
    }

    private static void OnThornsIncomingDamage(MonsterEffectInstance instance, MonsterIncomingDamageContext context)
    {
        if (context.Damage <= 0 || context.Attacker == null)
            return;

        int retaliation = GetThornsDamage(instance.Tier);
        context.AddRetaliationDamage(retaliation);
        context.Trigger(instance, $"Thorns dealt {retaliation} back.");
    }

    private static void OnEnragedOutgoingDamage(MonsterEffectInstance instance, MonsterOutgoingDamageContext context)
    {
        int empoweredAttacks = GetEnragedEmpoweredAttackCount(instance.Tier);
        int attacksUsed = instance.GetState("enraged_attacks_used");
        if (attacksUsed >= empoweredAttacks)
            return;

        int bonus = GetEnragedDamageBonus(instance.Tier);
        context.Damage += bonus;
        instance.SetState("enraged_attacks_used", attacksUsed + 1);
        context.Trigger(instance, $"Enraged added {bonus} damage.");
    }

    private static void OnLeechOutgoingDamage(MonsterEffectInstance instance, MonsterOutgoingDamageContext context)
    {
        if (context.Damage <= 0)
            return;

        int heal = GetLeechHeal(instance.Tier);
        context.AddHealing(heal);
        context.Trigger(instance, $"Leech restored {heal} HP.");
    }

    private static string DescribeInvulnerable(int tier)
    {
        return tier switch
        {
            0 => "Negates the first damaging hit in combat.",
            1 => "Negates all damage during the first two enemy turns.",
            _ => "Negates all damage on alternating enemy turns.",
        };
    }

    private static string DescribeBulwark(int tier)
    {
        return tier switch
        {
            0 => "Starts combat with 5 block and refreshes it through the first turn.",
            1 => "Starts each of the first two turns with 8 block.",
            _ => "Starts each of the first three turns with 12 block.",
        };
    }

    private static string DescribeThorns(int tier)
    {
        return $"Deals {GetThornsDamage(tier)} retaliation damage when this enemy is hit.";
    }

    private static string DescribeEnraged(int tier)
    {
        return tier switch
        {
            0 => "Adds 2 damage to the first enemy attack.",
            1 => "Adds 4 damage to the first two enemy attacks.",
            _ => "Adds 6 damage to every enemy attack.",
        };
    }

    private static string DescribeLeech(int tier)
    {
        return $"Heals {GetLeechHeal(tier)} HP after a successful enemy hit.";
    }

    private static bool IsInvulnerableActive(MonsterEffectInstance instance)
    {
        return instance.Tier switch
        {
            0 => instance.TriggerCount == 0,
            1 => instance.OwnerTurnsEnded < 2,
            _ => instance.OwnerTurnsStarted % 2 == 0,
        };
    }

    private static int GetBulwarkBlock(int tier)
    {
        return tier switch
        {
            0 => 5,
            1 => 8,
            _ => 12,
        };
    }

    private static int GetBulwarkTurnCount(int tier)
    {
        return tier switch
        {
            0 => 1,
            1 => 2,
            _ => 3,
        };
    }

    private static int GetThornsDamage(int tier)
    {
        return tier switch
        {
            0 => 1,
            1 => 3,
            _ => 5,
        };
    }

    private static int GetEnragedDamageBonus(int tier)
    {
        return tier switch
        {
            0 => 2,
            1 => 4,
            _ => 6,
        };
    }

    private static int GetEnragedEmpoweredAttackCount(int tier)
    {
        return tier switch
        {
            0 => 1,
            1 => 2,
            _ => int.MaxValue,
        };
    }

    private static int GetLeechHeal(int tier)
    {
        return tier switch
        {
            0 => 2,
            1 => 5,
            _ => 8,
        };
    }
}
