using System;
using Godot;

namespace ARPG;

public partial class EnemyCombatProfile
{
    public float MoveSpeed { get; }
    public float AttackIntervalSeconds { get; }
    public float AttackReach { get; }
    public float AttackWindupSeconds { get; }
    public float AttackCommitWindowSeconds { get; }
    public bool IsRanged { get; }
    public float ProjectileSpeed { get; }
    public AttackDefinition Attack { get; }

    public float RecoverySeconds => Math.Max(0.05f, AttackIntervalSeconds - AttackWindupSeconds);

    public EnemyCombatProfile(
        float moveSpeed,
        float attackIntervalSeconds,
        float attackReach,
        float attackWindupSeconds,
        float attackCommitWindowSeconds,
        AttackDefinition attack,
        bool isRanged = false,
        float projectileSpeed = 0.0f)
    {
        MoveSpeed = Math.Max(0.1f, moveSpeed);
        AttackIntervalSeconds = Math.Max(0.1f, attackIntervalSeconds);
        AttackReach = Math.Max(0.1f, attackReach);
        AttackWindupSeconds = Math.Clamp(attackWindupSeconds, 0.05f, AttackIntervalSeconds);
        AttackCommitWindowSeconds = Math.Clamp(
            attackCommitWindowSeconds,
            0.01f,
            AttackWindupSeconds);
        IsRanged = isRanged;
        ProjectileSpeed = isRanged ? Math.Max(1.0f, projectileSpeed) : 0.0f;
        Attack = attack;
    }

    public static EnemyCombatProfile CreateMeleeMvp()
    {
        return new EnemyCombatProfile(
            moveSpeed: 2.8f,
            attackIntervalSeconds: 1.0f,
            attackReach: 1.15f,
            attackWindupSeconds: 0.45f,
            attackCommitWindowSeconds: 0.2f,
            attack: AttackDefinition.CreateMelee(
                "enemy_melee",
                AttackVolumeDefinition.CreateBox(
                    new Vector3(0.85f, 1.0f, 0.95f),
                    new Vector3(0.0f, 0.0f, 0.72f)),
                AttackTimeline.Create(0.45f, 0.04f, 0.2f)));
    }

    public static EnemyCombatProfile CreateRangedMvp()
    {
        return new EnemyCombatProfile(
            moveSpeed: 2.2f,
            attackIntervalSeconds: 1.6f,
            attackReach: 7.0f,
            attackWindupSeconds: 0.6f,
            attackCommitWindowSeconds: 0.3f,
            attack: AttackDefinition.CreateProjectile(
                "enemy_projectile",
                AttackVolumeDefinition.CreateSphere(0.14f, new Vector3(0.0f, 0.0f, 0.0f)),
                AttackTimeline.Create(0.6f, 0.04f, 0.3f),
                damageMultiplier: 1.0f,
                projectileSpeed: 8.0f,
                projectileVisualRadius: 0.05f),
            isRanged: true,
            projectileSpeed: 8.0f);
    }
}
