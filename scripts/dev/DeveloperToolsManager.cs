using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class DeveloperToolsManager : Node
{
    private readonly Dictionary<ulong, string> _ownerKeys = new();

    public DeveloperGodModeState GodMode { get; } = new();
    public DeveloperEffectRegistry Effects { get; } = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public void EnableGodMode()
    {
        GodMode.Enable();
    }

    public void DisableGodMode()
    {
        GodMode.Disable();
    }

    public void BindProvider(Node owner, IDeveloperEffectProvider provider)
    {
        if (owner == null || provider == null)
            return;

        provider.RegisterDeveloperEffects(this);
    }

    public void RegisterEffect(
        Node owner,
        DeveloperEffectDescriptor descriptor,
        System.Action<bool> toggleChanged = null,
        System.Action triggerAction = null)
    {
        if (owner == null)
            return;

        string ownerKey = EnsureOwnerKey(owner);
        Effects.Register(ownerKey, descriptor, toggleChanged, triggerAction);
    }

    public bool IsEffectEnabled(Node owner, string effectId)
    {
        if (owner == null)
            return true;

        string ownerKey = EnsureOwnerKey(owner);
        return Effects.IsEnabled(ownerKey, effectId);
    }

    private string EnsureOwnerKey(Node owner)
    {
        ulong instanceId = owner.GetInstanceId();
        if (_ownerKeys.TryGetValue(instanceId, out string existing))
            return existing;

        string ownerKey = instanceId.ToString();
        _ownerKeys[instanceId] = ownerKey;
        owner.TreeExiting += () =>
        {
            Effects.RemoveOwner(ownerKey);
            _ownerKeys.Remove(instanceId);
        };
        return ownerKey;
    }
}
