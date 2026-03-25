using System;
using System.Linq;
using Godot;

namespace ARPG;

public partial class GodModePanel : Control
{
    private static readonly PackedScene GroupPanelScene = GD.Load<PackedScene>(Scenes.DeveloperEffectGroupPanel);
    private static readonly PackedScene EffectRowScene = GD.Load<PackedScene>(Scenes.DeveloperEffectRow);

    [Signal]
    public delegate void BackRequestedEventHandler();

    private DeveloperToolsManager _developerTools;
    private Label _statusLabel;
    private Button _godModeButton;
    private Button _passThroughButton;
    private Button _cameraCollisionButton;
    private Button _backButton;
    private VBoxContainer _groupsList;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _statusLabel = GetNode<Label>("RootVBox/MovementPanel/MovementVBox/StatusLabel");
        _godModeButton = GetNode<Button>("RootVBox/MovementPanel/MovementVBox/ButtonRow/GodModeButton");
        _passThroughButton = GetNode<Button>("RootVBox/MovementPanel/MovementVBox/ButtonRow/PassThroughButton");
        _cameraCollisionButton = GetNode<Button>("RootVBox/MovementPanel/MovementVBox/ButtonRow/CameraCollisionButton");
        _backButton = GetNode<Button>("RootVBox/FooterRow/BackButton");
        _groupsList = GetNode<VBoxContainer>("RootVBox/EffectsPanel/EffectsVBox/ScrollContainer/GroupsList");

        _godModeButton.Pressed += OnToggleGodMode;
        _passThroughButton.Pressed += OnTogglePassThrough;
        _cameraCollisionButton.Pressed += OnToggleCameraCollision;
        _backButton.Pressed += () => EmitSignal(SignalName.BackRequested);
    }

    public void Init(DeveloperToolsManager developerTools)
    {
        if (_developerTools == developerTools)
            return;

        if (_developerTools != null)
        {
            _developerTools.GodMode.Changed -= Refresh;
            _developerTools.Effects.Changed -= Refresh;
        }

        _developerTools = developerTools;
        if (_developerTools != null)
        {
            _developerTools.GodMode.Changed += Refresh;
            _developerTools.Effects.Changed += Refresh;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (!IsNodeReady())
            return;

        if (_developerTools == null)
        {
            Visible = false;
            return;
        }

        Visible = true;

        var state = _developerTools.GodMode;
        var cameraEffect = FindCameraCollisionEffect();

        _godModeButton.Text = state.Enabled ? "Disable God Mode" : "Enable God Mode";
        _passThroughButton.Text = state.PassThroughEnabled ? "Pass-Through On" : "Pass-Through Off";
        _passThroughButton.Disabled = !state.Enabled;

        bool cameraCollisionEnabled = cameraEffect?.Enabled ?? state.CameraCollisionEnabled;
        _cameraCollisionButton.Text = cameraCollisionEnabled ? "Camera Collision On" : "Camera Collision Off";
        _cameraCollisionButton.Disabled = !state.Enabled || state.PassThroughEnabled || cameraEffect == null;
        _cameraCollisionButton.TooltipText = state.PassThroughEnabled
            ? "Pass-through forces camera collision off."
            : string.Empty;

        _statusLabel.Text =
            $"Mode: {state.SpeedBandLabel}\n" +
            $"Flight: {(state.Enabled ? "Enabled" : "Disabled")}  |  Pass-through: {(state.PassThroughEnabled ? "On" : "Off")}\n" +
            $"Camera collision: {(state.EffectiveCameraCollisionEnabled ? "On" : "Off")}";

        RefreshGroups();
    }

    private void RefreshGroups()
    {
        foreach (Node child in _groupsList.GetChildren())
            child.QueueFree();

        if (_developerTools == null)
            return;

        foreach (var group in _developerTools.Effects.SnapshotGroups())
        {
            var filteredEffects = group.Effects
                .Where(effect => effect.LocalId != CameraController.CameraCollisionEffectId)
                .ToArray();
            if (filteredEffects.Length == 0)
                continue;

            var filteredGroup = new DeveloperEffectGroupSnapshot(
                group.GroupId,
                group.DisplayName,
                filteredEffects.Count(effect => effect.Enabled),
                filteredEffects.Length,
                filteredEffects.All(effect => effect.Enabled),
                filteredEffects);

            var panel = GroupPanelScene.Instantiate<DeveloperEffectGroupPanel>();
            panel.Populate(
                filteredGroup,
                EffectRowScene,
                (groupId, enabled) => _developerTools.Effects.SetGroupEnabled(groupId, enabled),
                (runtimeId, enabled) => _developerTools.Effects.SetEnabled(runtimeId, enabled),
                runtimeId => _developerTools.Effects.TryTrigger(runtimeId));
            _groupsList.AddChild(panel);
        }
    }

    private void OnToggleGodMode()
    {
        if (_developerTools == null)
            return;

        if (_developerTools.GodMode.Enabled)
            _developerTools.DisableGodMode();
        else
            _developerTools.EnableGodMode();
    }

    private void OnTogglePassThrough()
    {
        if (_developerTools == null || !_developerTools.GodMode.Enabled)
            return;

        _developerTools.GodMode.SetPassThroughEnabled(!_developerTools.GodMode.PassThroughEnabled);
    }

    private void OnToggleCameraCollision()
    {
        var cameraEffect = FindCameraCollisionEffect();
        if (_developerTools == null || cameraEffect == null || _developerTools.GodMode.PassThroughEnabled)
            return;

        _developerTools.Effects.SetEnabled(cameraEffect.RuntimeId, !cameraEffect.Enabled);
    }

    private DeveloperEffectSnapshot FindCameraCollisionEffect()
    {
        return _developerTools?
            .Effects
            .SnapshotGroups()
            .SelectMany(group => group.Effects)
            .FirstOrDefault(effect => effect.LocalId == CameraController.CameraCollisionEffectId);
    }
}
