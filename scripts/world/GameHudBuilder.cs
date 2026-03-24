using Godot;

namespace ARPG;

/// <summary>
/// HUD helpers for elements that require runtime construction or styling.
/// </summary>
public static class GameHudBuilder
{
    private static readonly PackedScene ItemSlotScene =
        GD.Load<PackedScene>("res://scenes/ItemSlot.tscn");

    public static void StyleHudLabels(Label[] labels, float viewportHeight)
    {
        int fontSize = Mathf.Max(20, (int)(viewportHeight * 0.032f));
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

    public record ItemSlotEntry(Panel Panel, TextureRect Icon, Label Label, StyleBoxFlat Style);

    /// <summary>
    /// Instantiates an ItemSlot scene and applies runtime styling.
    /// </summary>
    public static ItemSlotEntry CreateItemSlot()
    {
        var panel = ItemSlotScene.Instantiate<Panel>();
        var icon = panel.GetNode<TextureRect>("Content/Icon");
        var label = panel.GetNode<Label>("Content/Label");
        var style = CreateItemSlotStyle();
        panel.AddThemeStyleboxOverride("panel", style);
        icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        return new ItemSlotEntry(panel, icon, label, style);
    }

    public static StyleBoxFlat CreateItemSlotStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(Palette.BgDark, 0.9f);
        style.BorderColor = new Color(Palette.TextDisabled, 0.85f);
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(12);
        style.SetContentMarginAll(12);
        style.ShadowColor = new Color(0, 0, 0, 0.35f);
        style.ShadowSize = 6;
        style.ShadowOffset = new Vector2(0, 3);
        return style;
    }
}
