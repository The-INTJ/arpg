using Godot;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Overlay screen for managing weapon modifier slots and viewing stats.
/// Drag modifiers from backpack onto weapon slots, with before/after confirmation.
/// </summary>
public partial class ModifyStatsSimple : Control
{
	private PlayerStats _stats;
	private Label _weaponTitle;
	private Label[] _slotLabels = new Label[2];
	private Panel[] _slotPanels = new Panel[2];
	private Label _youStatsLabel;
	private VBoxContainer _backpackList;
	private Label _backpackEmptyLabel;

	// Confirmation popup
	private Panel _confirmPanel;
	private Label _confirmLabel;
	private int _pendingSlot = -1;
	private Modifier _pendingMod;

	[Signal]
	public delegate void ClosedEventHandler();

	public void Open(PlayerStats stats)
	{
		_stats = stats;
		Visible = true;
		GetTree().Paused = true;
		Refresh();
	}

	private void Close()
	{
		Visible = false;
		_pendingSlot = -1;
		_pendingMod = null;
		_confirmPanel.Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}

	public override void _Ready()
	{
		Visible = false;
		// Must fill the entire screen and always process (so input works while paused)
		SetAnchorsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;

		// Dark overlay
		var overlay = new ColorRect();
		overlay.Color = new Color(0, 0, 0, 0.7f);
		overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		overlay.MouseFilter = MouseFilterEnum.Stop;
		AddChild(overlay);

		// Main layout: HBox with weapon+backpack on left, you on right
		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 80);
		margin.AddThemeConstantOverride("margin_right", 80);
		margin.AddThemeConstantOverride("margin_top", 60);
		margin.AddThemeConstantOverride("margin_bottom", 60);
		AddChild(margin);

		var mainHBox = new HBoxContainer();
		mainHBox.AddThemeConstantOverride("separation", 40);
		margin.AddChild(mainHBox);

		// Left column: weapon + backpack
		var leftVBox = new VBoxContainer();
		leftVBox.AddThemeConstantOverride("separation", 20);
		leftVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		mainHBox.AddChild(leftVBox);

		BuildWeaponSection(leftVBox);
		BuildBackpackSection(leftVBox);

		// Right column: you
		var rightVBox = new VBoxContainer();
		rightVBox.AddThemeConstantOverride("separation", 10);
		rightVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		rightVBox.CustomMinimumSize = new Vector2(280, 0);
		mainHBox.AddChild(rightVBox);

		BuildYouSection(rightVBox);

		// Confirmation popup (centered, hidden)
		BuildConfirmPopup();

		// Close instructions
		var closeLabel = new Label();
		closeLabel.Text = "Press Escape to close";
		closeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		closeLabel.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.5f));
		closeLabel.AddThemeFontSizeOverride("font_size", 16);
		closeLabel.SetAnchorsPreset(LayoutPreset.CenterBottom);
		closeLabel.GrowHorizontal = GrowDirection.Both;
		closeLabel.OffsetTop = -40;
		AddChild(closeLabel);
	}

	private void BuildWeaponSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		panel.AddChild(vbox);

		_weaponTitle = new Label();
		_weaponTitle.AddThemeColorOverride("font_color", Palette.Accent);
		_weaponTitle.AddThemeFontSizeOverride("font_size", 26);
		_weaponTitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_weaponTitle);

		for (int i = 0; i < 2; i++)
		{
			var slotPanel = new Panel();
			var slotStyle = new StyleBoxFlat();
			slotStyle.BgColor = new Color(0.2f, 0.16f, 0.12f);
			slotStyle.SetCornerRadiusAll(6);
			slotStyle.SetContentMarginAll(12);
			slotPanel.AddThemeStyleboxOverride("panel", slotStyle);
			slotPanel.CustomMinimumSize = new Vector2(0, 44);
			vbox.AddChild(slotPanel);

			var slotLabel = new Label();
			slotLabel.AddThemeColorOverride("font_color", Palette.TextLight);
			slotLabel.AddThemeFontSizeOverride("font_size", 18);
			slotPanel.AddChild(slotLabel);

			_slotPanels[i] = slotPanel;
			_slotLabels[i] = slotLabel;
		}
	}

	private void BuildBackpackSection(VBoxContainer parent)
	{
		var sectionLabel = new Label();
		sectionLabel.Text = "BACKPACK";
		sectionLabel.AddThemeColorOverride("font_color", Palette.Accent);
		sectionLabel.AddThemeFontSizeOverride("font_size", 22);
		sectionLabel.HorizontalAlignment = HorizontalAlignment.Center;
		parent.AddChild(sectionLabel);

		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
		scroll.CustomMinimumSize = new Vector2(0, 150);
		parent.AddChild(scroll);

		_backpackList = new VBoxContainer();
		_backpackList.AddThemeConstantOverride("separation", 6);
		_backpackList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(_backpackList);

		_backpackEmptyLabel = new Label();
		_backpackEmptyLabel.Text = "(empty)";
		_backpackEmptyLabel.AddThemeColorOverride("font_color", Palette.TextDisabled);
		_backpackEmptyLabel.AddThemeFontSizeOverride("font_size", 16);
		_backpackList.AddChild(_backpackEmptyLabel);
	}

	private void BuildYouSection(VBoxContainer parent)
	{
		var panel = CreateSectionPanel();
		parent.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "YOU";
		title.AddThemeColorOverride("font_color", Palette.Accent);
		title.AddThemeFontSizeOverride("font_size", 26);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		_youStatsLabel = new Label();
		_youStatsLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		_youStatsLabel.AddThemeFontSizeOverride("font_size", 20);
		vbox.AddChild(_youStatsLabel);
	}

	private void BuildConfirmPopup()
	{
		_confirmPanel = new Panel();
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.15f, 0.10f, 0.08f, 0.95f);
		style.SetCornerRadiusAll(12);
		style.SetContentMarginAll(24);
		style.BorderColor = Palette.Accent;
		style.SetBorderWidthAll(2);
		_confirmPanel.AddThemeStyleboxOverride("panel", style);
		_confirmPanel.SetAnchorsPreset(LayoutPreset.Center);
		_confirmPanel.GrowHorizontal = GrowDirection.Both;
		_confirmPanel.GrowVertical = GrowDirection.Both;
		_confirmPanel.CustomMinimumSize = new Vector2(400, 200);
		_confirmPanel.Visible = false;
		AddChild(_confirmPanel);

		_confirmLabel = new Label();
		_confirmLabel.AddThemeColorOverride("font_color", Palette.TextLight);
		_confirmLabel.AddThemeFontSizeOverride("font_size", 18);
		_confirmLabel.SetAnchorsPreset(LayoutPreset.FullRect);
		_confirmLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_confirmPanel.AddChild(_confirmLabel);
	}

	private Panel CreateSectionPanel()
	{
		var panel = new Panel();
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.12f, 0.09f, 0.06f, 0.9f);
		style.SetCornerRadiusAll(10);
		style.SetContentMarginAll(16);
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible) return;

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
				ConfirmSwap();
				GetViewport().SetInputAsHandled();
			}
			else if (@event.IsActionPressed(GameKeys.Ability))
			{
				CancelSwap();
				GetViewport().SetInputAsHandled();
			}
			return;
		}
	}

	private void Refresh()
	{
		if (_stats == null) return;

		// Weapon section
		var weapon = _stats.Weapon;
		_weaponTitle.Text = weapon != null ? $"WEAPON: {weapon.Name}" : "WEAPON: None";

		for (int i = 0; i < 2; i++)
		{
			var mod = weapon?.Slots[i];
			_slotLabels[i].Text = mod != null ? $"Slot {i + 1}: {mod.Description}" : $"Slot {i + 1}: (empty)";
		}

		// You section
		_youStatsLabel.Text =
			$"HP:    {_stats.CurrentHp} / {_stats.MaxHp}\n" +
			$"ATK:   {_stats.AttackDamage}\n" +
			$"SPD:   {_stats.MoveSpeed:0.#}\n" +
			$"Range: {_stats.AttackRange:0.#}";

		// Backpack
		RefreshBackpack();
	}

	private void RefreshBackpack()
	{
		// Clear existing items (keep the empty label)
		foreach (var child in _backpackList.GetChildren())
		{
			if (child != _backpackEmptyLabel)
				child.QueueFree();
		}

		var backpack = _stats.Backpack;
		_backpackEmptyLabel.Visible = backpack.Count == 0;

		foreach (var mod in backpack)
		{
			var hbox = new HBoxContainer();
			hbox.AddThemeConstantOverride("separation", 8);
			_backpackList.AddChild(hbox);

			var modLabel = new Label();
			modLabel.Text = mod.Description;
			modLabel.AddThemeColorOverride("font_color", Palette.TextLight);
			modLabel.AddThemeFontSizeOverride("font_size", 16);
			modLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			hbox.AddChild(modLabel);

			// Slot 1 button
			for (int s = 0; s < 2; s++)
			{
				int slot = s; // capture
				var captured = mod; // capture
				var btn = new Button();
				btn.Text = $"-> Slot {s + 1}";
				btn.CustomMinimumSize = new Vector2(90, 0);
				Palette.StyleButton(btn, 14);
				btn.Pressed += () => StartSwap(slot, captured);
				hbox.AddChild(btn);
			}
		}
	}

	private void StartSwap(int slotIndex, Modifier newMod)
	{
		_pendingSlot = slotIndex;
		_pendingMod = newMod;

		var weapon = _stats.Weapon;
		var oldMod = weapon?.Slots[slotIndex];

		// Build before/after text for all stats
		string text = $"Swap Slot {slotIndex + 1}\n\n";
		text += $"Current: {(oldMod != null ? oldMod.Description : "(empty)")}\n";
		text += $"New:     {newMod.Description}\n\n";

		var targets = new[] { StatTarget.MaxHp, StatTarget.AttackDamage, StatTarget.MoveSpeed, StatTarget.AttackRange };
		var names = new[] { "HP", "ATK", "SPD", "Range" };

		for (int i = 0; i < targets.Length; i++)
		{
			float before = targets[i] == StatTarget.MaxHp ? _stats.MaxHp :
						   targets[i] == StatTarget.AttackDamage ? _stats.AttackDamage :
						   targets[i] == StatTarget.MoveSpeed ? _stats.MoveSpeed : _stats.AttackRange;
			float after = _stats.PreviewStatWithSwap(targets[i], slotIndex, newMod);

			string arrow = after > before ? " ^" : after < before ? " v" : "";
			text += $"{names[i]}: {FormatStat(before)} -> {FormatStat(after)}{arrow}\n";
		}

		text += $"\n({GameKeys.DisplayName(GameKeys.Attack)}) Confirm  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Cancel";

		_confirmLabel.Text = text;
		_confirmPanel.Visible = true;
	}

	private void ConfirmSwap()
	{
		if (_pendingSlot < 0 || _pendingMod == null) return;
		_stats.SwapWeaponSlot(_pendingSlot, _pendingMod);
		_pendingSlot = -1;
		_pendingMod = null;
		_confirmPanel.Visible = false;
		Refresh();
	}

	private void CancelSwap()
	{
		_pendingSlot = -1;
		_pendingMod = null;
		_confirmPanel.Visible = false;
	}

	private static string FormatStat(float value)
	{
		return value == (int)value ? $"{(int)value}" : $"{value:0.#}";
	}
}
