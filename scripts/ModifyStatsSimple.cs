using Godot;

namespace ARPG;

/// <summary>
/// Overlay screen for managing weapon modifiers and viewing effective stats.
/// The UI exposes one persistent row per stat target instead of anonymous numbered slots.
/// </summary>
public partial class ModifyStatsSimple : Control
{
	private PlayerStats _stats;
	private Label _weaponTitle;
	private Label _selectedModifierLabel;
	private Button[] _channelButtons = new Button[StatTargetInfo.All.Length];
	private Label _youStatsLabel;
	private VBoxContainer _backpackList;
	private Label _backpackEmptyLabel;

	private PanelContainer _confirmPanel;
	private Label _confirmLabel;
	private Modifier _selectedModifier;
	private Modifier _pendingModifier;

	[Signal]
	public delegate void ClosedEventHandler();

	public void Open(PlayerStats stats, Modifier preferredModifier = null)
	{
		_stats = stats;
		_selectedModifier = stats != null && stats.HasBackpackModifier(preferredModifier)
			? preferredModifier
			: null;
		_pendingModifier = null;
		Visible = true;
		GetTree().Paused = true;
		Refresh();
	}

	private void Close()
	{
		Visible = false;
		_selectedModifier = null;
		_pendingModifier = null;
		_confirmPanel.Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}

	public override void _Ready()
	{
		Visible = false;
		SetAnchorsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;

		var overlay = new ColorRect();
		overlay.Color = new Color(0, 0, 0, 0.72f);
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		overlay.MouseFilter = MouseFilterEnum.Stop;
		AddChild(overlay);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 72);
		margin.AddThemeConstantOverride("margin_right", 72);
		margin.AddThemeConstantOverride("margin_top", 48);
		margin.AddThemeConstantOverride("margin_bottom", 48);
		AddChild(margin);

		var rootVBox = new VBoxContainer();
		rootVBox.AddThemeConstantOverride("separation", 18);
		margin.AddChild(rootVBox);

		var header = new Label();
		header.Text = "Select a backpack modifier, then click its matching stat row.";
		header.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.9f));
		header.AddThemeFontSizeOverride("font_size", 18);
		header.HorizontalAlignment = HorizontalAlignment.Center;
		rootVBox.AddChild(header);

		var mainHBox = new HBoxContainer();
		mainHBox.AddThemeConstantOverride("separation", 28);
		mainHBox.SizeFlagsVertical = SizeFlags.ExpandFill;
		rootVBox.AddChild(mainHBox);

		var leftVBox = new VBoxContainer();
		leftVBox.AddThemeConstantOverride("separation", 18);
		leftVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		leftVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
		mainHBox.AddChild(leftVBox);

		BuildWeaponSection(leftVBox);
		BuildBackpackSection(leftVBox);

		var rightVBox = new VBoxContainer();
		rightVBox.AddThemeConstantOverride("separation", 18);
		rightVBox.CustomMinimumSize = new Vector2(320, 0);
		mainHBox.AddChild(rightVBox);

		BuildSelectedSection(rightVBox);
		BuildYouSection(rightVBox);

		BuildConfirmPopup();

		var footer = new Label();
		footer.Text = $"Esc Close  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Clear selection";
		footer.HorizontalAlignment = HorizontalAlignment.Center;
		footer.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.55f));
		footer.AddThemeFontSizeOverride("font_size", 15);
		rootVBox.AddChild(footer);
	}

	private void BuildWeaponSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		_weaponTitle = new Label();
		_weaponTitle.AddThemeColorOverride("font_color", Palette.Accent);
		_weaponTitle.AddThemeFontSizeOverride("font_size", 26);
		_weaponTitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_weaponTitle);

		var subtitle = new Label();
		subtitle.Text = "Stable stat channels replace the old numbered swap slots.";
		subtitle.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.75f));
		subtitle.AddThemeFontSizeOverride("font_size", 15);
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vbox.AddChild(subtitle);

		for (int i = 0; i < StatTargetInfo.All.Length; i++)
		{
			StatTarget target = StatTargetInfo.All[i];
			var button = new Button();
			button.Alignment = HorizontalAlignment.Left;
			button.CustomMinimumSize = new Vector2(0, 88);
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			button.FocusMode = Control.FocusModeEnum.None;
			button.Pressed += () => StartApply(target);
			vbox.AddChild(button);
			_channelButtons[i] = button;
		}
	}

	private void BuildBackpackSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = SizeFlags.ExpandFill;
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		vbox.SizeFlagsVertical = SizeFlags.ExpandFill;
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "BACKPACK";
		title.AddThemeColorOverride("font_color", Palette.Accent);
		title.AddThemeFontSizeOverride("font_size", 24);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.CustomMinimumSize = new Vector2(0, 180);
		vbox.AddChild(scroll);

		_backpackList = new VBoxContainer();
		_backpackList.AddThemeConstantOverride("separation", 8);
		_backpackList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(_backpackList);

		_backpackEmptyLabel = new Label();
		_backpackEmptyLabel.Text = "(empty)";
		_backpackEmptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_backpackEmptyLabel.AddThemeColorOverride("font_color", Palette.TextDisabled);
		_backpackEmptyLabel.AddThemeFontSizeOverride("font_size", 16);
		_backpackList.AddChild(_backpackEmptyLabel);
	}

	private void BuildSelectedSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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

		_selectedModifierLabel = new Label();
		_selectedModifierLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		_selectedModifierLabel.AddThemeFontSizeOverride("font_size", 17);
		_selectedModifierLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vbox.AddChild(_selectedModifierLabel);
	}

	private void BuildYouSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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

		_youStatsLabel = new Label();
		_youStatsLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		_youStatsLabel.AddThemeFontSizeOverride("font_size", 20);
		vbox.AddChild(_youStatsLabel);
	}

	private void BuildConfirmPopup()
	{
		_confirmPanel = CreateSectionPanel();
		_confirmPanel.Visible = false;
		_confirmPanel.AnchorLeft = 0.5f;
		_confirmPanel.AnchorRight = 0.5f;
		_confirmPanel.AnchorTop = 0.5f;
		_confirmPanel.AnchorBottom = 0.5f;
		_confirmPanel.OffsetLeft = -280;
		_confirmPanel.OffsetRight = 280;
		_confirmPanel.OffsetTop = -190;
		_confirmPanel.OffsetBottom = 190;
		_confirmPanel.MouseFilter = MouseFilterEnum.Stop;
		AddChild(_confirmPanel);

		var popupStyle = new StyleBoxFlat();
		popupStyle.BgColor = new Color(0.16f, 0.11f, 0.08f, 0.98f);
		popupStyle.BorderColor = Palette.Accent;
		popupStyle.SetBorderWidthAll(2);
		popupStyle.SetCornerRadiusAll(12);
		popupStyle.SetContentMarginAll(22);
		_confirmPanel.AddThemeStyleboxOverride("panel", popupStyle);

		_confirmLabel = new Label();
		_confirmLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		_confirmLabel.AddThemeFontSizeOverride("font_size", 18);
		_confirmLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_confirmPanel.AddChild(_confirmLabel);
	}

	private static PanelContainer CreateSectionPanel()
	{
		var panel = new PanelContainer();
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.12f, 0.09f, 0.06f, 0.92f);
		style.BorderColor = new Color(Palette.TextDisabled, 0.55f);
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(12);
		style.SetContentMarginAll(18);
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event.IsActionPressed(GameKeys.Pause))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_confirmPanel.Visible)
		{
			if (@event.IsActionPressed(GameKeys.Attack))
			{
				ConfirmApply();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed(GameKeys.Ability))
			{
				CancelApply();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (_selectedModifier != null && @event.IsActionPressed(GameKeys.Ability))
		{
			ClearSelection();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Refresh()
	{
		if (_stats == null)
			return;

		if (_selectedModifier != null && !_stats.HasBackpackModifier(_selectedModifier))
			_selectedModifier = null;

		var weapon = _stats.Weapon;
		_weaponTitle.Text = weapon != null ? $"WEAPON: {weapon.Name}" : "WEAPON: None";

		RefreshWeaponChannels();
		RefreshSelectionPanel();
		RefreshStatsPanel();
		RefreshBackpack();
	}

	private void RefreshWeaponChannels()
	{
		for (int i = 0; i < StatTargetInfo.All.Length; i++)
		{
			StatTarget target = StatTargetInfo.All[i];
			var button = _channelButtons[i];
			var channel = _stats.GetWeaponChannel(target);
			bool isSelectedTarget = _selectedModifier != null && _selectedModifier.Target == target;
			string currentSummary = channel?.Summary ?? "(unavailable)";

			string text = $"{StatTargetInfo.DisplayName(target)}\nCurrent: {currentSummary}";
			if (isSelectedTarget && channel != null)
				text += $"\nAfter mod: {channel.SummaryWith(_selectedModifier)}";

			button.Text = text;
			button.TooltipText = isSelectedTarget
				? $"Apply {_selectedModifier.Description} to {StatTargetInfo.DisplayName(target)}."
				: _selectedModifier != null
					? $"Selected modifier affects {StatTargetInfo.DisplayName(_selectedModifier.Target)}."
					: "Select a backpack modifier first.";

			ApplyChannelButtonTheme(button, isSelectedTarget, _selectedModifier != null);
		}
	}

	private void RefreshSelectionPanel()
	{
		if (_selectedModifier == null)
		{
			_selectedModifierLabel.Text =
				"No modifier selected.\n\nChoose one from the backpack to preview how it will stack.";
			return;
		}

		_selectedModifierLabel.Text =
			$"{_selectedModifier.Description}\n\n" +
			$"Next step: click the {StatTargetInfo.DisplayName(_selectedModifier.Target)} row to apply it.\n" +
			$"Press {GameKeys.DisplayName(GameKeys.Ability)} to clear this selection.";
	}

	private void RefreshStatsPanel()
	{
		_youStatsLabel.Text =
			$"HP:    {_stats.CurrentHp} / {_stats.MaxHp}\n" +
			$"ATK:   {_stats.AttackDamage}\n" +
			$"SPD:   {_stats.MoveSpeed:0.#}\n" +
			$"Range: {_stats.AttackRange:0.#}";
	}

	private void RefreshBackpack()
	{
		foreach (Node child in _backpackList.GetChildren())
		{
			if (child != _backpackEmptyLabel)
				child.QueueFree();
		}

		var backpack = _stats.Backpack;
		_backpackEmptyLabel.Visible = backpack.Count == 0;

		foreach (var modifier in backpack)
		{
			var rowPanel = CreateSectionPanel();
			rowPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_backpackList.AddChild(rowPanel);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			rowPanel.AddChild(row);

			var label = new Label();
			label.Text = modifier.Description;
			label.AddThemeColorOverride("font_color", Palette.TextLight);
			label.AddThemeFontSizeOverride("font_size", 16);
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(label);

			var selectButton = new Button();
			bool isSelected = modifier == _selectedModifier;
			selectButton.Text = isSelected ? "Clear" : "Select";
			selectButton.CustomMinimumSize = new Vector2(92, 0);
			selectButton.FocusMode = Control.FocusModeEnum.None;
			ApplyBackpackButtonTheme(selectButton, isSelected);

			var capturedModifier = modifier;
			selectButton.Pressed += () => ToggleSelection(capturedModifier);
			row.AddChild(selectButton);
		}
	}

	private void ToggleSelection(Modifier modifier)
	{
		if (modifier == null)
			return;

		_selectedModifier = _selectedModifier == modifier ? null : modifier;
		Refresh();
	}

	private void ClearSelection()
	{
		_selectedModifier = null;
		Refresh();
	}

	private void StartApply(StatTarget target)
	{
		if (_selectedModifier == null || _selectedModifier.Target != target || _stats == null)
			return;

		var channel = _stats.GetWeaponChannel(target);
		if (channel == null)
			return;

		_pendingModifier = _selectedModifier;

		_confirmLabel.Text =
			$"Apply Modifier\n\n" +
			$"Target: {StatTargetInfo.DisplayName(target)}\n" +
			$"Current: {channel.Summary}\n" +
			$"After mod: {channel.SummaryWith(_pendingModifier)}\n\n" +
			BuildStatPreview(_pendingModifier) +
			$"\n({GameKeys.DisplayName(GameKeys.Attack)}) Confirm  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Cancel";

		_confirmPanel.Visible = true;
	}

	private string BuildStatPreview(Modifier modifier)
	{
		string beforeHp = $"{_stats.CurrentHp} / {_stats.MaxHp}";
		string afterHp = $"{_stats.PreviewCurrentHpWithModifier(modifier)} / {(int)_stats.PreviewStatWithModifier(StatTarget.MaxHp, modifier)}";

		return
			$"HP: {beforeHp} -> {afterHp}\n" +
			$"ATK: {_stats.AttackDamage} -> {FormatStat(_stats.PreviewStatWithModifier(StatTarget.AttackDamage, modifier))}\n" +
			$"SPD: {_stats.MoveSpeed:0.#} -> {FormatStat(_stats.PreviewStatWithModifier(StatTarget.MoveSpeed, modifier))}\n" +
			$"Range: {_stats.AttackRange:0.#} -> {FormatStat(_stats.PreviewStatWithModifier(StatTarget.AttackRange, modifier))}";
	}

	private void ConfirmApply()
	{
		if (_pendingModifier == null)
			return;

		if (_stats.ApplyBackpackModifier(_pendingModifier))
			_selectedModifier = null;

		_pendingModifier = null;
		_confirmPanel.Visible = false;
		Refresh();
	}

	private void CancelApply()
	{
		_pendingModifier = null;
		_confirmPanel.Visible = false;
	}

	private static void ApplyChannelButtonTheme(Button button, bool isSelectedTarget, bool hasSelection)
	{
		if (isSelectedTarget)
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

	private static void ApplyBackpackButtonTheme(Button button, bool isSelected)
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

	private static void ApplyButtonTheme(
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

	private static StyleBoxFlat CreateButtonStyle(Color backgroundColor, Color borderColor, int padding)
	{
		var style = new StyleBoxFlat();
		style.BgColor = backgroundColor;
		style.BorderColor = borderColor;
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(10);
		style.SetContentMarginAll(padding);
		return style;
	}

	private static string FormatStat(float value)
	{
		return value == (int)value ? $"{(int)value}" : $"{value:0.#}";
	}
}
