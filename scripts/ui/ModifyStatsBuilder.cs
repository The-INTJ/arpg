using System;
using Godot;

namespace ARPG;

/// <summary>
/// Static factory that constructs the full UI tree for ModifyStatsSimple.
/// Returns a UiRefs record with all the node references the caller needs.
/// </summary>
public static class ModifyStatsBuilder
{
	public record UiRefs(
		Label WeaponTitle,
		Label SelectedModifierLabel,
		Button[] ChannelButtons,
		Label YouStatsLabel,
		VBoxContainer BackpackList,
		Label BackpackEmptyLabel,
		PanelContainer ConfirmPanel,
		Label ConfirmLabel);

	public static UiRefs Build(Control parent, Action<StatTarget> onChannelPressed)
	{
		parent.Visible = false;
		parent.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		parent.GrowHorizontal = Control.GrowDirection.Both;
		parent.GrowVertical = Control.GrowDirection.Both;
		parent.ProcessMode = Node.ProcessModeEnum.Always;
		parent.MouseFilter = Control.MouseFilterEnum.Stop;

		var overlay = new ColorRect();
		overlay.Color = new Color(0, 0, 0, 0.72f);
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.MouseFilter = Control.MouseFilterEnum.Stop;
		parent.AddChild(overlay);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 72);
		margin.AddThemeConstantOverride("margin_right", 72);
		margin.AddThemeConstantOverride("margin_top", 48);
		margin.AddThemeConstantOverride("margin_bottom", 48);
		parent.AddChild(margin);

		var rootVBox = new VBoxContainer();
		rootVBox.AddThemeConstantOverride("separation", 18);
		margin.AddChild(rootVBox);

		var header = new Label();
		header.Text = "Pick a modifier, then assign each effect to any stat row you want.";
		header.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.9f));
		header.AddThemeFontSizeOverride("font_size", 18);
		header.HorizontalAlignment = HorizontalAlignment.Center;
		rootVBox.AddChild(header);

		var mainHBox = new HBoxContainer();
		mainHBox.AddThemeConstantOverride("separation", 28);
		mainHBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rootVBox.AddChild(mainHBox);

		var leftVBox = new VBoxContainer();
		leftVBox.AddThemeConstantOverride("separation", 18);
		leftVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		leftVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		mainHBox.AddChild(leftVBox);

		var (weaponTitle, channelButtons) = BuildWeaponSection(leftVBox, onChannelPressed);
		var (backpackList, backpackEmptyLabel) = BuildBackpackSection(leftVBox);

		var rightVBox = new VBoxContainer();
		rightVBox.AddThemeConstantOverride("separation", 18);
		rightVBox.CustomMinimumSize = new Vector2(340, 0);
		mainHBox.AddChild(rightVBox);

		var selectedModifierLabel = BuildSelectedSection(rightVBox);
		var youStatsLabel = BuildYouSection(rightVBox);
		var (confirmPanel, confirmLabel) = BuildConfirmPopup(parent);

		var footer = new Label();
		footer.Text = $"Esc Close  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Reset picks / clear selection";
		footer.HorizontalAlignment = HorizontalAlignment.Center;
		footer.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.55f));
		footer.AddThemeFontSizeOverride("font_size", 15);
		rootVBox.AddChild(footer);

		return new UiRefs(weaponTitle, selectedModifierLabel, channelButtons,
			youStatsLabel, backpackList, backpackEmptyLabel, confirmPanel, confirmLabel);
	}

	private static (Label weaponTitle, Button[] channelButtons) BuildWeaponSection(
		VBoxContainer parent, Action<StatTarget> onChannelPressed)
	{
		var panel = ModifyStatsTheme.CreateSectionPanel();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var weaponTitle = new Label();
		weaponTitle.AddThemeColorOverride("font_color", Palette.Accent);
		weaponTitle.AddThemeFontSizeOverride("font_size", 26);
		weaponTitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(weaponTitle);

		var subtitle = new Label();
		subtitle.Text = "Rows are persistent stats. Flexible modifiers bind to a row only when you apply them.";
		subtitle.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.75f));
		subtitle.AddThemeFontSizeOverride("font_size", 15);
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vbox.AddChild(subtitle);

		var channelButtons = new Button[StatTargetInfo.All.Length];
		for (int i = 0; i < StatTargetInfo.All.Length; i++)
		{
			StatTarget target = StatTargetInfo.All[i];
			var button = new Button();
			button.Alignment = HorizontalAlignment.Left;
			button.CustomMinimumSize = new Vector2(0, 88);
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.FocusMode = Control.FocusModeEnum.None;
			button.Pressed += () => onChannelPressed(target);
			vbox.AddChild(button);
			channelButtons[i] = button;
		}

		return (weaponTitle, channelButtons);
	}

	private static (VBoxContainer backpackList, Label emptyLabel) BuildBackpackSection(VBoxContainer parent)
	{
		var panel = ModifyStatsTheme.CreateSectionPanel();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "BACKPACK";
		title.AddThemeColorOverride("font_color", Palette.Accent);
		title.AddThemeFontSizeOverride("font_size", 24);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		scroll.CustomMinimumSize = new Vector2(0, 180);
		vbox.AddChild(scroll);

		var backpackList = new VBoxContainer();
		backpackList.AddThemeConstantOverride("separation", 8);
		backpackList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		scroll.AddChild(backpackList);

		var emptyLabel = new Label();
		emptyLabel.Text = "(empty)";
		emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		emptyLabel.AddThemeColorOverride("font_color", Palette.TextDisabled);
		emptyLabel.AddThemeFontSizeOverride("font_size", 16);
		backpackList.AddChild(emptyLabel);

		return (backpackList, emptyLabel);
	}

	private static Label BuildSelectedSection(VBoxContainer parent)
	{
		var panel = ModifyStatsTheme.CreateSectionPanel();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "SELECTED";
		title.AddThemeColorOverride("font_color", Palette.Accent);
		title.AddThemeFontSizeOverride("font_size", 24);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var label = new Label();
		label.AddThemeColorOverride("font_color", Palette.TextLight);
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vbox.AddChild(label);

		return label;
	}

	private static Label BuildYouSection(VBoxContainer parent)
	{
		var panel = ModifyStatsTheme.CreateSectionPanel();
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "YOU";
		title.AddThemeColorOverride("font_color", Palette.Accent);
		title.AddThemeFontSizeOverride("font_size", 24);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var label = new Label();
		label.AddThemeColorOverride("font_color", Palette.TextLight);
		label.AddThemeFontSizeOverride("font_size", 20);
		vbox.AddChild(label);

		return label;
	}

	private static (PanelContainer panel, Label label) BuildConfirmPopup(Control parent)
	{
		var confirmPanel = ModifyStatsTheme.CreateSectionPanel();
		confirmPanel.Visible = false;
		confirmPanel.AnchorLeft = 0.5f;
		confirmPanel.AnchorRight = 0.5f;
		confirmPanel.AnchorTop = 0.5f;
		confirmPanel.AnchorBottom = 0.5f;
		confirmPanel.OffsetLeft = -300;
		confirmPanel.OffsetRight = 300;
		confirmPanel.OffsetTop = -210;
		confirmPanel.OffsetBottom = 210;
		confirmPanel.MouseFilter = Control.MouseFilterEnum.Stop;
		parent.AddChild(confirmPanel);

		var popupStyle = new StyleBoxFlat();
		popupStyle.BgColor = new Color(0.16f, 0.11f, 0.08f, 0.98f);
		popupStyle.BorderColor = Palette.Accent;
		popupStyle.SetBorderWidthAll(2);
		popupStyle.SetCornerRadiusAll(12);
		popupStyle.SetContentMarginAll(22);
		confirmPanel.AddThemeStyleboxOverride("panel", popupStyle);

		var confirmLabel = new Label();
		confirmLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		confirmLabel.AddThemeFontSizeOverride("font_size", 18);
		confirmLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		confirmPanel.AddChild(confirmLabel);

		return (confirmPanel, confirmLabel);
	}
}
