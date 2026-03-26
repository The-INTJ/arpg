using System;
using Godot;

namespace ARPG;

public partial class StatChannelRow : Button
{
    private Action<StatTarget> _onPressed;

    public StatTarget Target { get; private set; }

    public override void _Ready()
    {
        Alignment = HorizontalAlignment.Left;
        FocusMode = FocusModeEnum.None;
        Pressed += HandlePressed;
    }

    public void Populate(
        StatTarget target,
        string text,
        string tooltip,
        bool canAssign,
        bool hasSelection,
        bool hasPreview,
        Action<StatTarget> onPressed)
    {
        Target = target;
        Text = text;
        TooltipText = tooltip;
        _onPressed = onPressed;
        ModifyStatsTheme.ApplyChannelButtonTheme(this, canAssign, hasSelection, hasPreview);
    }

    private void HandlePressed()
    {
        _onPressed?.Invoke(Target);
    }
}
