using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ARPG;

/// <summary>
/// Overlay screen for managing flexible weapon modifiers and viewing effective stats.
/// Modifiers choose their stat targets at apply time, so the UI stages assignments before confirm.
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
	private ModifierAssignmentPlan _selectionPlan;
	private ModifierAssignmentPlan _pendingPlan;

	[Signal]
	public delegate void ClosedEventHandler();

	public void Open(PlayerStats stats, Modifier preferredModifier = null)
	{
		_stats = stats;
		_pendingPlan = null;
		SelectModifier(stats != null && stats.HasBackpackModifier(preferredModifier) ? preferredModifier : null);
		Visible = true;
		GetTree().Paused = true;
		_confirmPanel.Visible = false;
		Refresh();
	}

	private void Close()
	{
		Visible = false;
		_selectionPlan = null;
		_pendingPlan = null;
		_confirmPanel.Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}

	public override void _Ready()
	{
		var ui = ModifyStatsBuilder.Build(this, OnChannelPressed);
		_weaponTitle = ui.WeaponTitle;
		_selectedModifierLabel = ui.SelectedModifierLabel;
		_channelButtons = ui.ChannelButtons;
		_youStatsLabel = ui.YouStatsLabel;
		_backpackList = ui.BackpackList;
		_backpackEmptyLabel = ui.BackpackEmptyLabel;
		_confirmPanel = ui.ConfirmPanel;
		_confirmLabel = ui.ConfirmLabel;
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

		if (_selectionPlan != null && @event.IsActionPressed(GameKeys.Ability))
		{
			if (_selectionPlan.HasAnyAssignments)
				_selectionPlan.Reset();
			else
				SelectModifier(null);

			Refresh();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Refresh()
	{
		if (_stats == null)
			return;

		if (_selectionPlan != null && !_stats.HasBackpackModifier(_selectionPlan.Modifier))
			_selectionPlan = null;

		var weapon = _stats.Weapon;
		_weaponTitle.Text = weapon != null ? $"WEAPON: {weapon.Name}" : "WEAPON: None";

		RefreshWeaponChannels();
		RefreshSelectionPanel();
		RefreshStatsPanel();
		RefreshBackpack();
	}

	private void RefreshWeaponChannels()
	{
		var assignedEffects = _selectionPlan?.BuildAssignedEffects() ?? System.Array.Empty<AppliedModifierEffect>();

		for (int i = 0; i < StatTargetInfo.All.Length; i++)
		{
			StatTarget target = StatTargetInfo.All[i];
			var button = _channelButtons[i];
			var channel = _stats.GetWeaponChannel(target);

			bool hasSelection = _selectionPlan != null;
			bool canAssignNext = hasSelection && _selectionPlan.CanAssignNext(target);
			var previewEffects = hasSelection
				? (canAssignNext ? _selectionPlan.BuildPreviewEffectsIfNextAssigned(target) : assignedEffects)
				: System.Array.Empty<AppliedModifierEffect>();
			bool hasPreviewForTarget = previewEffects.Any(effect => effect.Target == target);

			string text = $"{StatTargetInfo.DisplayName(target)}\nCurrent: {channel?.Summary ?? "(unavailable)"}";
			if (hasSelection && hasPreviewForTarget && channel != null)
				text += $"\nAfter mod: {channel.SummaryWith(previewEffects)}";

			button.Text = text;
			button.TooltipText = canAssignNext
				? $"Assign {_selectionPlan.GetNextEffect().ShortLabel} to {StatTargetInfo.DisplayName(target)}."
				: hasSelection
					? "This row is not selectable for the next pending effect."
					: "Select a backpack modifier first.";

			ModifyStatsTheme.ApplyChannelButtonTheme(button, canAssignNext, hasSelection, hasPreviewForTarget);
		}
	}

	private void RefreshSelectionPanel()
	{
		if (_selectionPlan == null)
		{
			_selectedModifierLabel.Text =
				"No modifier selected.\n\nChoose one from the backpack to assign its effect values to any stat you want.";
			return;
		}

		var lines = new List<string>
		{
			_selectionPlan.Modifier.Description,
			string.Empty,
            "Assignments:"
		};

		for (int i = 0; i < _selectionPlan.SelectedTargets.Count; i++)
		{
			var effect = _selectionPlan.GetEffect(i);
			var selectedTarget = _selectionPlan.SelectedTargets[i];
			string targetText = selectedTarget.HasValue
				? StatTargetInfo.DisplayName(selectedTarget.Value)
				: $"choose {StatTargetInfo.DescribeTargetChoice(effect.AllowedTargets)}";

			lines.Add($"{i + 1}. {effect.ShortLabel} -> {targetText}");
		}

		var nextEffect = _selectionPlan.GetNextEffect();
		lines.Add(string.Empty);
		lines.Add(nextEffect != null
			? $"Next: choose a row for {nextEffect.ShortLabel}."
			: "All targets chosen. Confirm or cancel in the popup.");

		_selectedModifierLabel.Text = string.Join("\n", lines);
	}

	private void RefreshStatsPanel()
	{
		_youStatsLabel.Text =
			$"HP:     {_stats.CurrentHp} / {_stats.MaxHp}\n" +
			$"ATK:    {_stats.AttackDamage}\n" +
			$"SPD:    {_stats.MoveSpeed:0.#}\n" +
			$"Range:  {_stats.AttackRange:0.#}\n" +
			$"Slots:  {_stats.InventorySlotCount}";
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
			var rowPanel = ModifyStatsTheme.CreateSectionPanel();
			rowPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_backpackList.AddChild(rowPanel);

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			rowPanel.AddChild(row);

			var label = new Label();
			label.Text = modifier.Description;
			label.AddThemeColorOverride("font_color", Palette.TextLight);
			label.AddThemeFontSizeOverride("font_size", 16);
			label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(label);

			var selectButton = new Button();
			bool isSelected = _selectionPlan != null && modifier == _selectionPlan.Modifier;
			selectButton.Text = isSelected ? "Clear" : "Select";
			selectButton.CustomMinimumSize = new Vector2(92, 0);
			selectButton.FocusMode = Control.FocusModeEnum.None;
			ModifyStatsTheme.ApplyBackpackButtonTheme(selectButton, isSelected);

			var capturedModifier = modifier;
			selectButton.Pressed += () => ToggleSelection(capturedModifier);
			row.AddChild(selectButton);
		}
	}

	private void ToggleSelection(Modifier modifier)
	{
		if (_selectionPlan != null && _selectionPlan.Modifier == modifier)
			SelectModifier(null);
		else
			SelectModifier(modifier);

		Refresh();
	}

	private void SelectModifier(Modifier modifier)
	{
		_selectionPlan = modifier != null ? new ModifierAssignmentPlan(modifier) : null;
	}

	private void OnChannelPressed(StatTarget target)
	{
		if (_selectionPlan == null || !_selectionPlan.TryAssignNext(target))
			return;

		if (_selectionPlan.IsComplete)
			ShowConfirmPopup();
		else
			Refresh();
	}

	private void ShowConfirmPopup()
	{
		_pendingPlan = _selectionPlan?.Clone();
		if (_pendingPlan == null)
			return;

		var pendingEffects = _pendingPlan.BuildAppliedEffects();
		var lines = new List<string> { "Apply Modifier", string.Empty };

		foreach (var target in StatTargetInfo.All)
		{
			if (!pendingEffects.Any(effect => effect.Target == target))
				continue;

			var channel = _stats.GetWeaponChannel(target);
			lines.Add(StatTargetInfo.DisplayName(target));
			lines.Add($"Current: {channel?.Summary ?? "(unavailable)"}");
			lines.Add($"After mod: {channel?.SummaryWith(pendingEffects) ?? "(unavailable)"}");
			lines.Add(string.Empty);
		}

		lines.Add(BuildStatPreview(pendingEffects));
		lines.Add(string.Empty);
		lines.Add($"({GameKeys.DisplayName(GameKeys.Attack)}) Confirm  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Cancel");

		_confirmLabel.Text = string.Join("\n", lines);
		_confirmPanel.Visible = true;
	}

	private string BuildStatPreview(IReadOnlyList<AppliedModifierEffect> pendingEffects)
	{
		int afterMaxHp = (int)_stats.PreviewStatWithEffects(StatTarget.MaxHp, pendingEffects);
		return
			$"HP: {_stats.CurrentHp} / {_stats.MaxHp} -> {_stats.PreviewCurrentHpWithEffects(pendingEffects)} / {afterMaxHp}\n" +
			$"ATK: {_stats.AttackDamage} -> {StatTargetInfo.FormatStatValue(StatTarget.AttackDamage, _stats.PreviewStatWithEffects(StatTarget.AttackDamage, pendingEffects))}\n" +
			$"SPD: {_stats.MoveSpeed:0.#} -> {StatTargetInfo.FormatStatValue(StatTarget.MoveSpeed, _stats.PreviewStatWithEffects(StatTarget.MoveSpeed, pendingEffects))}\n" +
			$"Range: {_stats.AttackRange:0.#} -> {StatTargetInfo.FormatStatValue(StatTarget.AttackRange, _stats.PreviewStatWithEffects(StatTarget.AttackRange, pendingEffects))}\n" +
			$"Slots: {_stats.InventorySlotCount} -> {_stats.PreviewInventorySlotCountWithEffects(pendingEffects)}";
	}

	private void ConfirmApply()
	{
		if (_pendingPlan == null)
			return;

		if (_stats.ApplyBackpackModifier(_pendingPlan))
		{
			AudioManager.Instance?.PlayEffectApply();
			SelectModifier(null);
		}

		_pendingPlan = null;
		_confirmPanel.Visible = false;
		Refresh();
	}

	private void CancelApply()
	{
		_pendingPlan = null;
		_confirmPanel.Visible = false;
		Refresh();
	}

}
