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
	private static readonly PackedScene BackpackRowScene = GD.Load<PackedScene>(Scenes.BackpackItemRow);
	private static readonly PackedScene StatChannelRowScene = GD.Load<PackedScene>(Scenes.StatChannelRow);

	private PlayerStats _stats;
	private Label _weaponTitle;
	private Label _selectedModifierLabel;
	private Label _youStatsLabel;
	private VBoxContainer _channelList;
	private VBoxContainer _backpackList;
	private Label _backpackEmptyLabel;
	private readonly Dictionary<StatTarget, StatChannelRow> _channelRows = new();

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
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;

		_weaponTitle = GetNode<Label>("MarginContainer/RootVBox/MainHBox/LeftVBox/WeaponPanel/WeaponVBox/WeaponTitle");
		_channelList = GetNode<VBoxContainer>("MarginContainer/RootVBox/MainHBox/LeftVBox/WeaponPanel/WeaponVBox/ScrollContainer/StatChannelList");
		_selectedModifierLabel = GetNode<Label>("MarginContainer/RootVBox/MainHBox/RightVBox/SelectedPanel/SelectedVBox/SelectedModifierLabel");
		_youStatsLabel = GetNode<Label>("MarginContainer/RootVBox/MainHBox/RightVBox/YouPanel/YouVBox/YouStatsLabel");
		_backpackList = GetNode<VBoxContainer>("MarginContainer/RootVBox/MainHBox/LeftVBox/BackpackPanel/BackpackVBox/ScrollContainer/BackpackList");
		_backpackEmptyLabel = GetNode<Label>("MarginContainer/RootVBox/MainHBox/LeftVBox/BackpackPanel/BackpackVBox/ScrollContainer/BackpackList/BackpackEmptyLabel");
		_confirmPanel = GetNode<PanelContainer>("ConfirmPanel");
		_confirmLabel = GetNode<Label>("ConfirmPanel/ConfirmLabel");
		BuildChannelRows();

		// Footer text uses runtime key display names
		var footer = GetNode<Label>("MarginContainer/RootVBox/Footer");
		footer.Text = $"Esc Close  |  ({GameKeys.DisplayName(GameKeys.Ability)}) Reset picks / clear selection";
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

		foreach (var metadata in StatTargetInfo.Ordered)
		{
			StatTarget target = metadata.Target;
			var row = _channelRows[target];
			var channel = _stats.GetWeaponChannel(target);

			bool hasSelection = _selectionPlan != null;
			bool canAssignNext = hasSelection && _selectionPlan.CanAssignNext(target);
			var previewEffects = hasSelection
				? (canAssignNext ? _selectionPlan.BuildPreviewEffectsIfNextAssigned(target) : assignedEffects)
				: System.Array.Empty<AppliedModifierEffect>();
			bool hasPreviewForTarget = previewEffects.Any(effect => effect.Target == target);

			string text =
				$"{StatTargetInfo.DisplayName(target)}\n" +
				$"Mods: {channel?.Summary ?? "(unavailable)"}\n" +
				$"Value: {StatTargetInfo.FormatStatValueWithProgress(target, _stats.GetEffectiveStatValue(target))}";
			if (hasSelection && hasPreviewForTarget)
				text += $"\nAfter mod: {StatTargetInfo.FormatStatValueWithProgress(target, _stats.PreviewStatWithEffects(target, previewEffects))}";

			string tooltip = canAssignNext
				? $"Assign {_selectionPlan.GetNextEffect().ShortLabel} to {StatTargetInfo.DisplayName(target)}."
				: hasSelection
					? "This row is not selectable for the next pending effect."
					: "Select a backpack modifier first.";

			row.Populate(target, text, tooltip, canAssignNext, hasSelection, hasPreviewForTarget, OnChannelPressed);
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
		var lines = new List<string>
		{
			$"Current HP: {_stats.CurrentHp} / {StatTargetInfo.FormatStatValueWithProgress(StatTarget.MaxHp, _stats.GetEffectiveStatValue(StatTarget.MaxHp))}"
		};

		foreach (var metadata in StatTargetInfo.Ordered)
		{
			if (metadata.Target == StatTarget.MaxHp)
				continue;

			lines.Add($"{StatTargetInfo.DisplayName(metadata.Target)}: {StatTargetInfo.FormatStatValueWithProgress(metadata.Target, _stats.GetEffectiveStatValue(metadata.Target))}");
		}

		_youStatsLabel.Text = string.Join("\n", lines);
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
			bool isSelected = _selectionPlan != null && modifier == _selectionPlan.Modifier;
			var capturedModifier = modifier;

			var row = BackpackRowScene.Instantiate<BackpackItemRow>();
			_backpackList.AddChild(row);
			row.Populate(capturedModifier, isSelected, () => ToggleSelection(capturedModifier));
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

		foreach (var metadata in StatTargetInfo.Ordered)
		{
			StatTarget target = metadata.Target;
			if (!pendingEffects.Any(effect => effect.Target == target))
				continue;

			var channel = _stats.GetWeaponChannel(target);
			lines.Add(StatTargetInfo.DisplayName(target));
			lines.Add($"Mods: {channel?.Summary ?? "(unavailable)"}");
			lines.Add($"Current value: {StatTargetInfo.FormatStatValueWithProgress(target, _stats.GetEffectiveStatValue(target))}");
			lines.Add($"After mod: {StatTargetInfo.FormatStatValueWithProgress(target, _stats.PreviewStatWithEffects(target, pendingEffects))}");
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
		var lines = new List<string>
		{
			$"HP: {_stats.CurrentHp} / {_stats.MaxHp} -> {_stats.PreviewCurrentHpWithEffects(pendingEffects)} / {afterMaxHp}"
		};

		foreach (var metadata in StatTargetInfo.Ordered)
		{
			StatTarget target = metadata.Target;
			if (target == StatTarget.MaxHp)
				continue;

			lines.Add(
				$"{StatTargetInfo.DisplayName(target)}: " +
				$"{StatTargetInfo.FormatStatValueWithProgress(target, _stats.GetEffectiveStatValue(target))} -> " +
				$"{StatTargetInfo.FormatStatValueWithProgress(target, _stats.PreviewStatWithEffects(target, pendingEffects))}");
		}

		return string.Join("\n", lines);
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

	private void BuildChannelRows()
	{
		foreach (var metadata in StatTargetInfo.Ordered)
		{
			var row = StatChannelRowScene.Instantiate<StatChannelRow>();
			_channelList.AddChild(row);
			_channelRows[metadata.Target] = row;
		}
	}

}
