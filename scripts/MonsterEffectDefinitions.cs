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
            threatByTier: new[] { 4 },
            onIncomingDamage: OnInvulnerableIncomingDamage),
        new MonsterEffectDefinition(
            id: "bulwark",
            name: "Bulwark",
            badgeText: "BLW",
            badgeColor: Palette.EffectBulwark,
            baseWeight: 1.0f,
            tags: MonsterEffectTag.Defense | MonsterEffectTag.Attrition | MonsterEffectTag.BossSafe,
            threatByTier: new[] { 2 },
            onOwnerTurnEnded: OnBulwarkTurnEnded,
            onIncomingDamage: OnBulwarkIncomingDamage),
        new MonsterEffectDefinition(
            id: "thorns",
            name: "Thorns",
            badgeText: "THN",
            badgeColor: Palette.EffectThorns,
            baseWeight: 1.0f,
            tags: MonsterEffectTag.Retaliation | MonsterEffectTag.PunishesBurst,
            threatByTier: new[] { 2 },
            onIncomingDamage: OnThornsIncomingDamage),
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
        if (instance.TriggerCount > 0 || context.Damage <= 0)
            return;

        context.Damage = 0;
        context.Trigger(instance, "Invulnerable negated the hit.");
        instance.MarkExpired();
    }

    private static void OnBulwarkIncomingDamage(MonsterEffectInstance instance, MonsterIncomingDamageContext context)
    {
        if (instance.OwnerTurnsEnded >= 1 || context.Damage <= 0)
            return;

        int absorbed = Mathf.Min(1, context.Damage);
        if (absorbed <= 0)
            return;

        context.Damage -= absorbed;
        context.Trigger(instance, $"Bulwark absorbed {absorbed} damage.");
    }

    private static void OnBulwarkTurnEnded(MonsterEffectInstance instance)
    {
        if (instance.OwnerTurnsEnded >= 1)
            instance.MarkExpired();
    }

    private static void OnThornsIncomingDamage(MonsterEffectInstance instance, MonsterIncomingDamageContext context)
    {
        if (context.BaseDamage <= 0 || context.Attacker == null)
            return;

        context.AddRetaliationDamage(1);
        context.Trigger(instance, "Thorns dealt 1 back.");
    }
}
