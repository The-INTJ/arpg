using Godot;

namespace ARPG;

/// <summary>
/// HUD helpers for elements that require runtime construction or styling.
/// </summary>
public static class GameHudBuilder
{
	private static readonly PackedScene ItemSlotScene =
		GD.Load<PackedScene>(Scenes.ItemSlot);

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

	public record ItemSlotEntry(
		PanelContainer Panel,
		MarginContainer Margin,
		HBoxContainer Row,
		TextureRect Icon,
		Label Label,
		StyleBoxFlat Style,
		Vector2 BasePanelMinimumSize,
		Vector2 BaseLabelMinimumSize);

	/// <summary>
	/// Instantiates an ItemSlot scene and applies runtime styling.
	/// </summary>
	public static ItemSlotEntry CreateItemSlot()
	{
		var panel = ItemSlotScene.Instantiate<PanelContainer>();
		var margin = panel.GetNodeOrNull<MarginContainer>("MarginContainer");
		var row = panel.GetNodeOrNull<HBoxContainer>("MarginContainer/HBoxContainer");
		var icon = panel.GetNodeOrNull<TextureRect>("MarginContainer/HBoxContainer/Icon");
		var label = panel.GetNodeOrNull<Label>("MarginContainer/HBoxContainer/Label");

		if (margin == null || row == null || icon == null || label == null)
		{
			throw new System.InvalidOperationException(
				"ItemSlot.tscn is missing the expected layout nodes for the HUD item bar.");
		}

		margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		margin.MouseFilter = Control.MouseFilterEnum.Ignore;
		row.MouseFilter = Control.MouseFilterEnum.Ignore;
		icon.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.MouseFilter = Control.MouseFilterEnum.Ignore;

		var style = CreateItemSlotStyle();
		panel.AddThemeStyleboxOverride("panel", style);
		panel.MouseFilter = Control.MouseFilterEnum.Stop;
		panel.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
		icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		var entry = new ItemSlotEntry(
			panel,
			margin,
			row,
			icon,
			label,
			style,
			panel.CustomMinimumSize,
			label.CustomMinimumSize);
		RefreshItemSlotSize(entry);
		return entry;
	}

	public static void RefreshItemSlotSize(ItemSlotEntry entry)
	{
		float desiredLabelWidth = Mathf.Max(
			entry.BaseLabelMinimumSize.X,
			MeasureLabelTextWidth(entry.Label) + 2.0f);
		entry.Label.CustomMinimumSize = new Vector2(desiredLabelWidth, entry.BaseLabelMinimumSize.Y);

		Vector2 contentMinimum = entry.Margin.GetCombinedMinimumSize();
		entry.Panel.CustomMinimumSize = new Vector2(
			Mathf.Max(entry.BasePanelMinimumSize.X, Mathf.Ceil(contentMinimum.X)),
			Mathf.Max(entry.BasePanelMinimumSize.Y, Mathf.Ceil(contentMinimum.Y)));
	}

	public static StyleBoxFlat CreateItemSlotStyle()
	{
		var style = new StyleBoxFlat();
		style.BgColor = new Color(Palette.BgDark, 0.9f);
		style.BorderColor = new Color(Palette.TextDisabled, 0.85f);
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(12);
		style.SetContentMarginAll(10);
		style.ShadowColor = new Color(0, 0, 0, 0.35f);
		style.ShadowSize = 6;
		style.ShadowOffset = new Vector2(0, 3);
		return style;
	}

	private static float MeasureLabelTextWidth(Label label)
	{
		if (label == null || string.IsNullOrEmpty(label.Text))
			return 0.0f;

		var font = label.GetThemeFont("font");
		if (font == null)
			return 0.0f;

		int fontSize = label.GetThemeFontSize("font_size");
		float widestLine = 0.0f;
		foreach (string rawLine in label.Text.Split('\n'))
		{
			string line = rawLine.TrimEnd('\r');
			widestLine = Mathf.Max(
				widestLine,
				font.GetStringSize(line, HorizontalAlignment.Left, -1, fontSize).X);
		}

		return Mathf.Ceil(widestLine);
	}
}
