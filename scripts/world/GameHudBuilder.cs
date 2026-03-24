using Godot;

namespace ARPG;

/// <summary>
/// HUD helpers for elements that require runtime construction or styling.
/// </summary>
public static class GameHudBuilder
{
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

    public static (Control[] slots, TextureRect[] icons, Label[] labels, StyleBoxFlat[] styles) BindItemBar(HBoxContainer hbox)
    {
        int slotCount = Mathf.Min(GameKeys.ItemSlots.Length, hbox.GetChildCount());
        var slots = new Control[slotCount];
        var icons = new TextureRect[slotCount];
        var labels = new Label[slotCount];
        var styles = new StyleBoxFlat[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            var panel = hbox.GetChild<Panel>(i);
            var icon = panel.GetNode<TextureRect>("Content/Icon");
            var label = panel.GetNode<Label>("Content/Label");
            var style = CreateItemSlotStyle();
            panel.AddThemeStyleboxOverride("panel", style);
            panel.CustomMinimumSize = new Vector2(228, 72);
            label.AddThemeFontSizeOverride("font_size", 17);
            icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

            slots[i] = panel;
            icons[i] = icon;
            labels[i] = label;
            styles[i] = style;
        }

        return (slots, icons, labels, styles);
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
