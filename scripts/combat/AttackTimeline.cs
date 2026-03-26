using System;

namespace ARPG;

public readonly partial record struct AttackTimeline(
    float WindupSeconds,
    float ActiveSeconds,
    float RecoverySeconds)
{
    public float TotalSeconds => WindupSeconds + ActiveSeconds + RecoverySeconds;

    public static AttackTimeline Create(float windupSeconds, float activeSeconds, float recoverySeconds)
    {
        return new AttackTimeline(
            Math.Max(0.0f, windupSeconds),
            Math.Max(0.01f, activeSeconds),
            Math.Max(0.0f, recoverySeconds));
    }
}
