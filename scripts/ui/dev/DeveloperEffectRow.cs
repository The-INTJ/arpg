using System;
using Godot;

namespace ARPG;

public partial class DeveloperEffectRow : HBoxContainer
{
    private Label _titleLabel;
    private Label _detailLabel;
    private Button _toggleButton;
    private Button _triggerButton;
    private Action _toggleAction;
    private Action _triggerAction;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("TextVBox/TitleLabel");
        _detailLabel = GetNode<Label>("TextVBox/DetailLabel");
        _toggleButton = GetNode<Button>("ToggleButton");
        _triggerButton = GetNode<Button>("TriggerButton");

        _toggleButton.Pressed += () => _toggleAction?.Invoke();
        _triggerButton.Pressed += () => _triggerAction?.Invoke();
    }

    public void Populate(DeveloperEffectSnapshot snapshot, Action toggleAction, Action triggerAction)
    {
        _toggleAction = toggleAction;
        _triggerAction = triggerAction;

        _titleLabel.Text = snapshot.DisplayName;
        _detailLabel.Text = string.IsNullOrWhiteSpace(snapshot.OwnerLabel)
            ? snapshot.Description
            : $"{snapshot.Description}\nOwner: {snapshot.OwnerLabel}";
        _toggleButton.Text = snapshot.Enabled ? "On" : "Off";
        _triggerButton.Visible = snapshot.CanTrigger;
        _triggerButton.Disabled = !snapshot.CanTrigger;
    }
}
