using Godot;

namespace ARPG;

/// <summary>
/// Static factory methods that construct HUD UI nodes programmatically for the game scene.
/// </summary>
public static class GameHudBuilder
{
    public static void StyleHudLabels(Label[] labels, float viewportHeight)
    {
        int fontSize = Mathf.Max(18, (int)(viewportHeight * 0.03f));
        foreach (var label in labels)
        {
            label.AddThemeColorOverride("font_color", Palette.TextLight);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        }
    }

    public static Button BuildAbilityButton(CanvasLayer canvas)
    {
        var button = new Button();
        button.Name = "AbilityButton";
        button.Visible = false;
        Palette.StyleButton(button, 18);
        canvas.AddChild(button);
        return button;
    }

    public record EnemyHpDisplay(ProgressBar Bar, Label HpLabel, Label EffectInfoLabel, VBoxContainer Container);

    public static EnemyHpDisplay BuildEnemyHpBar(CanvasLayer canvas)
    {
        var container = new VBoxContainer();
        container.Name = "EnemyHpDisplay";
        container.Visible = false;
        canvas.AddChild(container);

        var hpLabel = new Label();
        hpLabel.Text = "Enemy";
        hpLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        hpLabel.AddThemeFontSizeOverride("font_size", 16);
        hpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        container.AddChild(hpLabel);

        var bar = new ProgressBar();
        bar.MinValue = 0;
        bar.MaxValue = 1;
        bar.CustomMinimumSize = new Vector2(120, 14);
        bar.ShowPercentage = false;

        var barStyle = new StyleBoxFlat();
        barStyle.BgColor = new Color(0.7f, 0.15f, 0.15f);
        barStyle.SetCornerRadiusAll(4);
        bar.AddThemeStyleboxOverride("fill", barStyle);

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.15f, 0.12f);
        bgStyle.SetCornerRadiusAll(4);
        bar.AddThemeStyleboxOverride("background", bgStyle);

        container.AddChild(bar);

        var effectInfoLabel = new Label();
        effectInfoLabel.CustomMinimumSize = new Vector2(220, 0);
        effectInfoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        effectInfoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        effectInfoLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        effectInfoLabel.AddThemeFontSizeOverride("font_size", 15);
        container.AddChild(effectInfoLabel);

        return new EnemyHpDisplay(bar, hpLabel, effectInfoLabel, container);
    }

    public static (Label roomLabel, Label ruleLabel) BuildRoomLabels(
        CanvasLayer canvas, int room, RoomMonsterEffectProfile profile)
    {
        var roomLabel = new Label();
        roomLabel.Text = $"Room {room}/{GameState.TotalRooms}";
        roomLabel.AddThemeColorOverride("font_color", Palette.Accent);
        roomLabel.AddThemeFontSizeOverride("font_size", 22);
        roomLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        roomLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        roomLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        roomLabel.AnchorLeft = 1.0f;
        roomLabel.AnchorRight = 1.0f;
        roomLabel.AnchorTop = 0.0f;
        roomLabel.AnchorBottom = 0.0f;
        roomLabel.GrowHorizontal = Control.GrowDirection.Begin;
        roomLabel.OffsetLeft = -200;
        roomLabel.OffsetTop = 20;
        roomLabel.HorizontalAlignment = HorizontalAlignment.Right;
        canvas.AddChild(roomLabel);

        var ruleLabel = new Label();
        ruleLabel.Text = $"{profile.DisplayName}\n{profile.Description}";
        ruleLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        ruleLabel.AddThemeFontSizeOverride("font_size", 14);
        ruleLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        ruleLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        ruleLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        ruleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ruleLabel.CustomMinimumSize = new Vector2(280, 0);
        ruleLabel.AnchorLeft = 1.0f;
        ruleLabel.AnchorRight = 1.0f;
        ruleLabel.AnchorTop = 0.0f;
        ruleLabel.AnchorBottom = 0.0f;
        ruleLabel.GrowHorizontal = Control.GrowDirection.Begin;
        ruleLabel.OffsetLeft = -320;
        ruleLabel.OffsetTop = 52;
        ruleLabel.HorizontalAlignment = HorizontalAlignment.Right;
        canvas.AddChild(ruleLabel);

        return (roomLabel, ruleLabel);
    }

    public static (Label[] labels, StyleBoxFlat[] styles) BuildItemBar(CanvasLayer canvas, int slotCount)
    {
        var labels = new Label[slotCount];
        var styles = new StyleBoxFlat[slotCount];

        var existing = canvas.GetNodeOrNull<CenterContainer>("ItemBarCenter");
        existing?.QueueFree();

        var center = new CenterContainer();
        center.Name = "ItemBarCenter";
        center.AnchorLeft = 0.0f;
        center.AnchorRight = 1.0f;
        center.AnchorTop = 1.0f;
        center.AnchorBottom = 1.0f;
        center.OffsetTop = -100;
        center.OffsetBottom = -20;
        canvas.AddChild(center);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        center.AddChild(hbox);

        for (int i = 0; i < slotCount; i++)
        {
            var panel = new Panel();
            panel.CustomMinimumSize = new Vector2(180, 58);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(Palette.BgDark, 0.92f);
            style.BorderColor = Palette.TextDisabled;
            style.SetBorderWidthAll(2);
            style.SetCornerRadiusAll(8);
            style.SetContentMarginAll(10);
            panel.AddThemeStyleboxOverride("panel", style);
            hbox.AddChild(panel);

            var label = new Label();
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.AddThemeFontSizeOverride("font_size", 15);
            panel.AddChild(label);

            labels[i] = label;
            styles[i] = style;
        }

        return (labels, styles);
    }
}
