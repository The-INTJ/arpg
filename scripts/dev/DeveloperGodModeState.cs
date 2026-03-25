using System;

namespace ARPG;

public partial class DeveloperGodModeState
{
    public event Action Changed;

    public bool Enabled { get; private set; }
    public bool FlightEnabled { get; private set; }
    public bool PassThroughEnabled { get; private set; }
    public bool CameraCollisionEnabled { get; private set; } = true;
    public bool VeryFastBurstActive { get; private set; }

    public bool EffectiveCameraCollisionEnabled => !Enabled || (CameraCollisionEnabled && !PassThroughEnabled);

    public string SpeedBandLabel => !Enabled
        ? "Off"
        : VeryFastBurstActive
            ? "Very Fast"
            : "Flight";

    public void Enable()
    {
        bool changed = !Enabled || !FlightEnabled || !PassThroughEnabled || VeryFastBurstActive;
        Enabled = true;
        FlightEnabled = true;
        PassThroughEnabled = true;
        VeryFastBurstActive = false;
        if (changed)
            Changed?.Invoke();
    }

    public void Disable()
    {
        bool changed = Enabled || FlightEnabled || PassThroughEnabled || VeryFastBurstActive;
        Enabled = false;
        FlightEnabled = false;
        PassThroughEnabled = false;
        VeryFastBurstActive = false;
        if (changed)
            Changed?.Invoke();
    }

    public void SetPassThroughEnabled(bool enabled)
    {
        if (PassThroughEnabled == enabled)
            return;

        PassThroughEnabled = enabled;
        Changed?.Invoke();
    }

    public void SetCameraCollisionEnabled(bool enabled)
    {
        if (CameraCollisionEnabled == enabled)
            return;

        CameraCollisionEnabled = enabled;
        Changed?.Invoke();
    }

    public void SetVeryFastBurstActive(bool active)
    {
        if (VeryFastBurstActive == active)
            return;

        VeryFastBurstActive = active;
        Changed?.Invoke();
    }
}
