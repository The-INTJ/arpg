using System;

namespace ARPG;

public partial class AttackWindupState
{
    public float DurationSeconds { get; }
    public float CommitWindowSeconds { get; }
    public float TimeRemainingSeconds { get; private set; }

    public bool IsActive => TimeRemainingSeconds > 0.0f;
    public bool IsCommitted => IsActive && TimeRemainingSeconds <= CommitWindowSeconds;

    public AttackWindupState(float durationSeconds, float commitWindowSeconds)
    {
        DurationSeconds = Math.Max(0.01f, durationSeconds);
        CommitWindowSeconds = Math.Clamp(commitWindowSeconds, 0.01f, DurationSeconds);
    }

    public void Start()
    {
        TimeRemainingSeconds = DurationSeconds;
    }

    public void Reset()
    {
        TimeRemainingSeconds = 0.0f;
    }

    public bool Advance(float deltaSeconds, bool targetStillInRange, out bool cancelled)
    {
        cancelled = false;
        if (!IsActive)
            return false;

        if (!IsCommitted && !targetStillInRange)
        {
            Reset();
            cancelled = true;
            return false;
        }

        TimeRemainingSeconds = Math.Max(0.0f, TimeRemainingSeconds - Math.Max(0.0f, deltaSeconds));
        return TimeRemainingSeconds <= 0.0f;
    }
}
