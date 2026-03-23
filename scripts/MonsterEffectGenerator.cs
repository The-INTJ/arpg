using System;
using System.Collections.Generic;
using Godot;

namespace ARPG;

public static partial class MonsterEffectGenerator
{
    public static MonsterEffectAssignmentPlan Generate(MonsterEffectRollContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var plan = new MonsterEffectAssignmentPlan();
        bool allowOptionalRolls = ApplyGuaranteedEffects(context, plan);

        if (!allowOptionalRolls || plan.Count >= context.MaxTotalEffects)
            return plan;

        if (GD.Randf() > context.Profile.BaseEffectChance)
            return plan;

        int optionalCount = MonsterEffectRoller.RollWeightedInt(
            context.Profile.GetOptionalCountOptions(context.IsBoss),
            fallback: 0);
        optionalCount = Math.Min(optionalCount, context.MaxOptionalEffects);
        optionalCount = Math.Min(optionalCount, Math.Max(0, context.MaxTotalEffects - plan.Count));

        for (int i = 0; i < optionalCount; i++)
        {
            MonsterEffectDefinition definition = RollDefinition(context, plan);
            if (definition == null)
                break;

            int tier = RollTier(definition, context, plan);
            if (tier < 0)
                break;

            plan.Add(new MonsterEffectAssignment(
                definition,
                tier,
                MonsterEffectSource.Optional,
                consumesThreatBudget: true));
        }

        return plan;
    }

    private static bool ApplyGuaranteedEffects(MonsterEffectRollContext context, MonsterEffectAssignmentPlan plan)
    {
        bool allowOptionalRolls = true;

        foreach (var grant in context.Profile.GuaranteedEffects)
        {
            if (grant == null || !grant.AppliesTo(context.IsBoss))
                continue;

            MonsterEffectDefinition definition = MonsterEffectDefinitions.GetOrNull(grant.EffectId);
            if (!CanAddDefinition(definition, context, plan))
                continue;

            int tier = Math.Clamp(grant.Tier, 0, Math.Min(definition.MaxTier, context.MaxTier));
            int threatCost = grant.ConsumesThreatBudget ? definition.GetThreatCost(tier) : 0;
            if (plan.TotalThreat + threatCost > context.ThreatBudget)
                continue;

            plan.Add(new MonsterEffectAssignment(
                definition,
                tier,
                MonsterEffectSource.Granted,
                grant.ConsumesThreatBudget));

            if (!grant.AllowOptionalRollsAfterGrant)
                allowOptionalRolls = false;
        }

        return allowOptionalRolls;
    }

    private static MonsterEffectDefinition RollDefinition(MonsterEffectRollContext context, MonsterEffectAssignmentPlan plan)
    {
        var candidates = new List<(MonsterEffectDefinition Definition, float Weight)>();

        foreach (var definition in MonsterEffectDefinitions.All)
        {
            if (!CanAddDefinition(definition, context, plan))
                continue;
            if (!HasFittingTier(definition, context, plan))
                continue;

            float weight = definition.BaseWeight
                * context.Profile.GetWeightMultiplier(definition)
                * context.GetContextWeightMultiplier(definition);
            if (weight <= 0.0f)
                continue;

            candidates.Add((definition, weight));
        }

        if (candidates.Count == 0)
            return null;

        float totalWeight = 0.0f;
        foreach (var candidate in candidates)
            totalWeight += candidate.Weight;

        float roll = GD.Randf() * totalWeight;
        foreach (var candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll <= 0.0f)
                return candidate.Definition;
        }

        return candidates[^1].Definition;
    }

    private static int RollTier(
        MonsterEffectDefinition definition,
        MonsterEffectRollContext context,
        MonsterEffectAssignmentPlan plan)
    {
        int maxTier = Math.Min(definition.MaxTier, context.MaxTier);
        var candidates = new List<WeightedIntOption>();

        foreach (var option in context.Profile.GetTierOptions(context.IsBoss, maxTier))
        {
            int threatCost = definition.GetThreatCost(option.Value);
            if (plan.TotalThreat + threatCost > context.ThreatBudget)
                continue;

            candidates.Add(option);
        }

        if (candidates.Count == 0)
            return -1;

        return MonsterEffectRoller.RollWeightedInt(candidates, fallback: candidates[0].Value);
    }

    private static bool HasFittingTier(
        MonsterEffectDefinition definition,
        MonsterEffectRollContext context,
        MonsterEffectAssignmentPlan plan)
    {
        int maxTier = Math.Min(definition.MaxTier, context.MaxTier);
        foreach (var option in context.Profile.GetTierOptions(context.IsBoss, maxTier))
        {
            if (plan.TotalThreat + definition.GetThreatCost(option.Value) <= context.ThreatBudget)
                return true;
        }

        return false;
    }

    private static bool CanAddDefinition(
        MonsterEffectDefinition definition,
        MonsterEffectRollContext context,
        MonsterEffectAssignmentPlan plan)
    {
        if (definition == null || context.IsBlocked(definition))
            return false;
        if (plan.Count >= context.MaxTotalEffects)
            return false;

        foreach (var existing in plan.Assignments)
        {
            if (!definition.AllowDuplicates && existing.Definition.Id == definition.Id)
                return false;
            if (!definition.IsCompatibleWith(existing.Definition) || !existing.Definition.IsCompatibleWith(definition))
                return false;
        }

        return true;
    }
}
