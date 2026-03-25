using System;
using Godot;

namespace ARPG;

public partial class DeveloperEffectGroupPanel : VBoxContainer
{
    private Label _titleLabel;
    private Label _countLabel;
    private Button _toggleGroupButton;
    private VBoxContainer _effectsList;
    private Action _toggleGroupAction;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Header/TitleLabel");
        _countLabel = GetNode<Label>("Header/CountLabel");
        _toggleGroupButton = GetNode<Button>("Header/ToggleGroupButton");
        _effectsList = GetNode<VBoxContainer>("EffectsList");

        _toggleGroupButton.Pressed += () => _toggleGroupAction?.Invoke();
    }

    public void Populate(
        DeveloperEffectGroupSnapshot snapshot,
        PackedScene rowScene,
        Action<string, bool> setGroupEnabled,
        Action<string, bool> setEffectEnabled,
        Action<string> triggerEffect)
    {
        _titleLabel.Text = snapshot.DisplayName;
        _countLabel.Text = $"{snapshot.EnabledCount}/{snapshot.TotalCount} on";
        _toggleGroupAction = () => setGroupEnabled?.Invoke(snapshot.GroupId, !snapshot.AllEnabled);
        _toggleGroupButton.Text = snapshot.AllEnabled ? "Disable All" : "Enable All";

        foreach (Node child in _effectsList.GetChildren())
            child.QueueFree();

        foreach (var effect in snapshot.Effects)
        {
            var row = rowScene.Instantiate<DeveloperEffectRow>();
            row.Populate(
                effect,
                () => setEffectEnabled?.Invoke(effect.RuntimeId, !effect.Enabled),
                effect.CanTrigger ? () => triggerEffect?.Invoke(effect.RuntimeId) : null);
            _effectsList.AddChild(row);
        }
    }
}
