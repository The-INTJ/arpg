using System;

namespace ARPG;

public readonly partial record struct AttackDefinition(
    string Id,
    AttackVolumeDefinition Volume,
    AttackTimeline Timeline,
    int MaxTargets = 1,
    float DamageMultiplier = 1.0f,
    bool RequiresClearPath = true,
    bool IsProjectile = false,
    float ProjectileSpeed = 0.0f,
    float ProjectileVisualRadius = 0.05f)
{
    public int TargetCap => MaxTargets <= 0 ? int.MaxValue : MaxTargets;
    public bool HitsMultipleTargets => TargetCap > 1;

    public static AttackDefinition CreateMelee(
        string id,
        AttackVolumeDefinition volume,
        AttackTimeline timeline,
        int maxTargets = 1,
        float damageMultiplier = 1.0f,
        bool requiresClearPath = true)
    {
        return new AttackDefinition(
            id,
            volume,
            timeline,
            Math.Max(1, maxTargets),
            damageMultiplier,
            requiresClearPath);
    }

    public static AttackDefinition CreateProjectile(
        string id,
        AttackVolumeDefinition volume,
        AttackTimeline timeline,
        float damageMultiplier,
        float projectileSpeed,
        float projectileVisualRadius,
        bool requiresClearPath = true)
    {
        return new AttackDefinition(
            id,
            volume,
            timeline,
            1,
            damageMultiplier,
            requiresClearPath,
            true,
            Math.Max(0.1f, projectileSpeed),
            Math.Max(0.01f, projectileVisualRadius));
    }
}
