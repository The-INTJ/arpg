using Godot;

namespace ARPG;

/// <summary>
/// HUD helpers for elements that require runtime construction or styling.
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

    public record EnemyHpDisplay(ProgressBar Bar, Label HpLabel, Label EffectInfoLabel, VBoxContainer Container);

    public static (Label[] labels, StyleBoxFlat[] styles) BuildItemBar(HBoxContainer hbox, int slotCount)
    {
        var labels = new Label[slotCount];
        var styles = new StyleBoxFlat[slotCount];

        // Clear existing slots
        foreach (Node child in hbox.GetChildren())
            child.QueueFree();

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
