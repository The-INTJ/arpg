using Godot;

namespace ARPG;

/// <summary>
/// Static theming helpers for the modifier assignment UI buttons and panels.
/// </summary>
public static class ModifyStatsTheme
{
	public static PanelContainer CreateSectionPanel()
	{
		var panel = new PanelContainer();
		ApplySectionPanelStyle(panel);
		return panel;
	}

	public static void ApplySectionPanelStyle(PanelContainer panel)
	{
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.12f, 0.09f, 0.06f, 0.92f);
		style.BorderColor = new Color(Palette.TextDisabled, 0.55f);
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(12);
		style.SetContentMarginAll(18);
		panel.AddThemeStyleboxOverride("panel", style);
	}

	public static void ApplyChannelButtonTheme(Button button, bool canAssign, bool hasSelection, bool hasPreview)
	{
		if (canAssign)
		{
			ApplyButtonTheme(
				button,
				new Color(Palette.ButtonBg, 0.96f),
				new Color(Palette.ButtonHover, 0.96f),
				new Color(Palette.Accent, 0.96f),
				Palette.Accent,
				Palette.TextLight,
				18,
				14);
			return;
		}

		if (hasSelection && hasPreview)
		{
			ApplyButtonTheme(
				button,
				new Color(0.18f, 0.14f, 0.10f, 0.96f),
				new Color(0.18f, 0.14f, 0.10f, 0.96f),
				new Color(0.18f, 0.14f, 0.10f, 0.96f),
				new Color(Palette.Accent, 0.45f),
				Palette.TextLight,
				18,
				14);
			return;
		}

		if (hasSelection)
		{
			ApplyButtonTheme(
				button,
				new Color(0.14f, 0.11f, 0.08f, 0.96f),
				new Color(0.14f, 0.11f, 0.08f, 0.96f),
				new Color(0.14f, 0.11f, 0.08f, 0.96f),
				new Color(Palette.TextDisabled, 0.35f),
				new Color(Palette.TextLight, 0.75f),
				18,
				14);
			return;
		}

		ApplyButtonTheme(
			button,
			new Color(0.20f, 0.16f, 0.12f, 0.96f),
			new Color(0.24f, 0.19f, 0.14f, 0.96f),
			new Color(0.24f, 0.19f, 0.14f, 0.96f),
			new Color(Palette.TextDisabled, 0.55f),
			Palette.TextLight,
			18,
			14);
	}

	public static void ApplyBackpackButtonTheme(Button button, bool isSelected)
	{
		if (isSelected)
		{
			ApplyButtonTheme(
				button,
				new Color(Palette.Accent, 0.96f),
				new Color(Palette.Accent, 0.96f),
				new Color(Palette.ButtonHover, 0.96f),
				Palette.Accent,
				Palette.BgDark,
				14,
				10);
			return;
		}

		ApplyButtonTheme(
			button,
			new Color(Palette.ButtonBg, 0.96f),
			new Color(Palette.ButtonHover, 0.96f),
			new Color(Palette.Accent, 0.96f),
			new Color(Palette.ButtonHover, 0.85f),
			Palette.TextLight,
			14,
			10);
	}

	public static void ApplyButtonTheme(
		Button button,
		Color normalBg,
		Color hoverBg,
		Color pressedBg,
		Color borderColor,
		Color fontColor,
		int fontSize,
		int padding)
	{
		button.AddThemeStyleboxOverride("normal", CreateButtonStyle(normalBg, borderColor, padding));
		button.AddThemeStyleboxOverride("hover", CreateButtonStyle(hoverBg, borderColor, padding));
		button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(pressedBg, borderColor, padding));
		button.AddThemeStyleboxOverride("focus", CreateButtonStyle(hoverBg, borderColor, padding));
		button.AddThemeColorOverride("font_color", fontColor);
		button.AddThemeColorOverride("font_hover_color", fontColor);
		button.AddThemeColorOverride("font_pressed_color", fontColor);
		button.AddThemeFontSizeOverride("font_size", fontSize);
	}

	public static StyleBoxFlat CreateButtonStyle(Color backgroundColor, Color borderColor, int padding)
	{
		var style = new StyleBoxFlat();
		style.BgColor = backgroundColor;
		style.BorderColor = borderColor;
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(10);
		style.SetContentMarginAll(padding);
		return style;
	}
}
