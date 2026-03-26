using System;

namespace ARPG;

public partial class EnemyCombatProfile
{
    public float MoveSpeed { get; }
    public float AttackIntervalSeconds { get; }
    public float AttackRange { get; }
    public float AttackWindupSeconds { get; }
    public float AttackCommitWindowSeconds { get; }

    public float RecoverySeconds => Math.Max(0.05f, AttackIntervalSeconds - AttackWindupSeconds);

    public EnemyCombatProfile(
        float moveSpeed,
        float attackIntervalSeconds,
        float attackRange,
        float attackWindupSeconds,
        float attackCommitWindowSeconds)
    {
        MoveSpeed = Math.Max(0.1f, moveSpeed);
        AttackIntervalSeconds = Math.Max(0.1f, attackIntervalSeconds);
        AttackRange = Math.Max(0.1f, attackRange);
        AttackWindupSeconds = Math.Clamp(attackWindupSeconds, 0.05f, AttackIntervalSeconds);
        AttackCommitWindowSeconds = Math.Clamp(
            attackCommitWindowSeconds,
            0.01f,
            AttackWindupSeconds);
    }

    public static EnemyCombatProfile CreateMeleeMvp()
    {
        return new EnemyCombatProfile(
            moveSpeed: 2.8f,
            attackIntervalSeconds: 1.0f,
            attackRange: 1.15f,
            attackWindupSeconds: 0.45f,
            attackCommitWindowSeconds: 0.2f);
    }
}
