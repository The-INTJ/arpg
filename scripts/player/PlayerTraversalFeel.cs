using System;
using Godot;

namespace ARPG;

public static partial class PlayerTraversalFeel
{
    public const float DefaultFloorSnapLength = 0.45f;
    public const float CoyoteTime = 0.12f;
    public const float JumpBufferTime = 0.12f;
    public const float AirControlSpeedFactor = 0.72f;

    private const float GroundStandingStartAcceleration = 52.0f;
    private const float GroundDriveAcceleration = 30.0f;
    private const float GroundTurnAcceleration = 60.0f;
    private const float GroundDeceleration = 38.0f;
    private const float AirAcceleration = 14.0f;
    private const float AirDeceleration = 9.0f;
    private const float StandingStartSpeedThreshold = 0.45f;
    private const float ReversalDotThreshold = -0.2f;
    private const float RisingGravity = 15.0f;
    private const float ApexGravity = 9.0f;
    private const float FallingGravity = 24.0f;
    private const float JumpCutGravity = 32.0f;
    private const float ApexVerticalSpeedThreshold = 1.1f;

    public static float GetHorizontalMoveRate(Vector3 currentHorizontalVelocity, Vector3 desiredHorizontalVelocity, bool grounded)
    {
        if (!grounded)
            return desiredHorizontalVelocity.LengthSquared() > 0.001f ? AirAcceleration : AirDeceleration;

        if (desiredHorizontalVelocity.LengthSquared() <= 0.001f)
            return GroundDeceleration;

        if (currentHorizontalVelocity.Length() <= StandingStartSpeedThreshold)
            return GroundStandingStartAcceleration;

        Vector3 currentDirection = currentHorizontalVelocity.Normalized();
        Vector3 desiredDirection = desiredHorizontalVelocity.Normalized();
        return currentDirection.Dot(desiredDirection) <= ReversalDotThreshold
            ? GroundTurnAcceleration
            : GroundDriveAcceleration;
    }

    public static float GetVerticalGravity(float verticalVelocity, bool jumpHeld)
    {
        if (verticalVelocity > 0.0f)
        {
            if (!jumpHeld)
                return JumpCutGravity;

            return Math.Abs(verticalVelocity) <= ApexVerticalSpeedThreshold
                ? ApexGravity
                : RisingGravity;
        }

        return FallingGravity;
    }

    public static float ComputeJumpVelocity(float jumpHeight)
    {
        return MathF.Sqrt(2.0f * RisingGravity * MathF.Max(jumpHeight, 0.0f));
    }
}
