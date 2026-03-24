using System;
using Godot;

namespace ARPG;

/// <summary>
/// Scene-backed row for a single backpack modifier in the ModifyStats UI.
/// Instantiated by ModifyStatsSimple for each modifier in the backpack.
/// </summary>
public partial class BackpackItemRow : PanelContainer
{
	private Label _descriptionLabel;
	private Button _selectButton;

	public override void _Ready()
	{
		_descriptionLabel = GetNode<Label>("Row/DescriptionLabel");
		_selectButton = GetNode<Button>("Row/SelectButton");
	}

	public void Populate(Modifier modifier, bool isSelected, Action onToggle)
	{
		_descriptionLabel.Text = modifier.Description;
		_selectButton.Text = isSelected ? "Clear" : "Select";

		ModifyStatsTheme.ApplySectionPanelStyle(this);
		ModifyStatsTheme.ApplyBackpackButtonTheme(_selectButton, isSelected);

		// Disconnect any previous signal connections before adding new one
		foreach (var connection in _selectButton.GetSignalConnectionList("pressed"))
			_selectButton.Disconnect("pressed", (Callable)connection["callable"]);

		_selectButton.Pressed += () => onToggle();
	}
}
