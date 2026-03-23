using Godot;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Full-screen stats overlay showing player stats, active modifiers, and backpack.
/// Opened as a pause overlay from the game scene.
/// </summary>
public partial class ModifyStatsSimple : Control
{
    private PlayerStats _stats;
    private Label _hpValue;
    private Label _atkValue;
    private Label _spdValue;
    private Label _rangeValue;
    private VBoxContainer _modifierList;

    public void Open(PlayerStats stats)
    {
        _stats = stats;
        Visible = true;
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
        RefreshStats();
        RefreshModifiers();
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsPreset(LayoutPreset.FullRect);

        // Dark overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.7f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // Outer centering container
        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(center);

        // Main panel background
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(520, 400);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.14f, 0.10f, 0.07f, 0.95f);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = Palette.Accent;
        panelStyle.SetCornerRadiusAll(12);
        panelStyle.SetContentMarginAll(24);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        // Main vertical layout inside the panel
        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(mainVBox);

        // Title
        var title = new Label();
        title.Text = "STATS & MODIFIERS";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 28);
        mainVBox.AddChild(title);

        // Separator
        mainVBox.AddChild(CreateSeparator());

        // Two-column layout: Stats on left, Modifiers on right
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        columns.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mainVBox.AddChild(columns);

        // Left column: Stats
        columns.AddChild(BuildStatsPanel());

        // Vertical divider
        var divider = new VSeparator();
        divider.AddThemeColorOverride("separator", new Color(Palette.Accent, 0.4f));
        columns.AddChild(divider);

        // Right column: Modifiers
        columns.AddChild(BuildModifiersPanel());

        // Separator
        mainVBox.AddChild(CreateSeparator());

        // Close hint
        var hint = new Label();
        hint.Text = "Press Escape to close";
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.5f));
        hint.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(hint);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event.IsActionPressed(GameKeys.Pause) || @event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private Control BuildStatsPanel()
    {
        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 8);

        var heading = new Label();
        heading.Text = "YOU";
        heading.AddThemeColorOverride("font_color", Palette.Accent);
        heading.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(heading);

        _hpValue = AddStatRow(vbox, "HP");
        _atkValue = AddStatRow(vbox, "ATK");
        _spdValue = AddStatRow(vbox, "SPD");
        _rangeValue = AddStatRow(vbox, "Range");

        return vbox;
    }

    private Label AddStatRow(VBoxContainer parent, string statName)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var nameLabel = new Label();
        nameLabel.Text = statName;
        nameLabel.CustomMinimumSize = new Vector2(70, 0);
        nameLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(nameLabel);

        var valueLabel = new Label();
        valueLabel.AddThemeColorOverride("font_color", Palette.Accent);
        valueLabel.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(valueLabel);

        parent.AddChild(row);
        return valueLabel;
    }

    private Control BuildModifiersPanel()
    {
        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 8);

        var heading = new Label();
        heading.Text = "MODIFIERS";
        heading.AddThemeColorOverride("font_color", Palette.Accent);
        heading.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(heading);

        // Scrollable modifier list
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.CustomMinimumSize = new Vector2(200, 200);
        vbox.AddChild(scroll);

        _modifierList = new VBoxContainer();
        _modifierList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _modifierList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_modifierList);

        return vbox;
    }

    private void RefreshStats()
    {
        if (_stats == null) return;
        _hpValue.Text = $"{_stats.CurrentHp} / {_stats.MaxHp}";
        _atkValue.Text = $"{_stats.AttackDamage}";
        _spdValue.Text = $"{_stats.MoveSpeed:0.#}";
        _rangeValue.Text = $"{_stats.AttackRange:0.#}";
    }

    private void RefreshModifiers()
    {
        if (_stats == null) return;

        // Clear old entries
        foreach (var child in _modifierList.GetChildren())
            child.QueueFree();

        if (_stats.Modifiers.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No modifiers yet";
            empty.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.4f));
            empty.AddThemeFontSizeOverride("font_size", 15);
            _modifierList.AddChild(empty);
            return;
        }

        foreach (var mod in _stats.Modifiers)
        {
            var item = new PanelContainer();
            var itemStyle = new StyleBoxFlat();
            itemStyle.BgColor = new Color(0.20f, 0.15f, 0.10f, 0.8f);
            itemStyle.SetCornerRadiusAll(6);
            itemStyle.SetContentMarginAll(8);
            item.AddThemeStyleboxOverride("panel", itemStyle);

            var label = new Label();
            label.Text = mod.Description;
            label.AddThemeColorOverride("font_color", Palette.TextLight);
            label.AddThemeFontSizeOverride("font_size", 16);
            item.AddChild(label);

            _modifierList.AddChild(item);
        }
    }

    private static HSeparator CreateSeparator()
    {
        var sep = new HSeparator();
        sep.AddThemeColorOverride("separator", new Color(Palette.Accent, 0.3f));
        return sep;
    }
}
