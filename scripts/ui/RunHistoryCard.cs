using Godot;

namespace ARPG;

/// <summary>
/// Scene-backed card that displays a single run history entry.
/// Instantiated by RunHistorySnippet for each RunScoreEntry.
/// </summary>
public partial class RunHistoryCard : PanelContainer
{
	private Label _header;
	private Label _meta;
	private GridContainer _scoreGrid;
	private GridContainer _statsGrid;

	public override void _Ready()
	{
		_header = GetNode<Label>("CardVBox/Header");
		_meta = GetNode<Label>("CardVBox/Meta");
		_scoreGrid = GetNode<GridContainer>("CardVBox/ScoreGrid");
		_statsGrid = GetNode<GridContainer>("CardVBox/StatsGrid");
	}

	public void Populate(RunScoreEntry entry, bool isLatest)
	{
		ApplyCardStyle(isLatest);

		string runLabel = isLatest ? "Latest Run" : $"Run {entry.RunNumber}";
		_header.Text = $"{runLabel}  |  {entry.OutcomeLabel}";
		_header.AddThemeColorOverride("font_color", isLatest ? Palette.Accent : Palette.TextLight);

		_meta.Text = $"{ArchetypeData.DisplayName(entry.Archetype)}  |  Room {entry.RoomReached}/{entry.TotalRooms}";

		ClearGrid(_scoreGrid);
		AddGridRow(_scoreGrid, "Monsters Killed", entry.MonstersKilled.ToString());
		AddGridRow(_scoreGrid, "Bosses Defeated", entry.BossesDefeated.ToString());
		AddGridRow(_scoreGrid, "Damage Done", entry.DamageDone.ToString());
		AddGridRow(_scoreGrid, "Highest Damage", entry.HighestDamage.ToString());

		ClearGrid(_statsGrid);
		foreach (var stat in entry.Stats)
			AddGridRow(_statsGrid, stat.Name, stat.Value);
	}

	private void ApplyCardStyle(bool isLatest)
	{
		var style = new StyleBoxFlat();
		style.BgColor = new Color(Palette.BgDark, 0.94f);
		style.BorderColor = isLatest ? Palette.Accent : Palette.TextDisabled;
		style.SetBorderWidthAll(2);
		style.SetCornerRadiusAll(10);
		style.SetContentMarginAll(14);
		AddThemeStyleboxOverride("panel", style);
	}

	private static void ClearGrid(GridContainer grid)
	{
		foreach (Node child in grid.GetChildren())
			child.QueueFree();
	}

	private static void AddGridRow(GridContainer grid, string labelText, string valueText)
	{
		var label = new Label();
		label.Text = labelText;
		label.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.78f));
		label.AddThemeFontSizeOverride("font_size", 16);
		grid.AddChild(label);

		var value = new Label();
		value.Text = valueText;
		value.HorizontalAlignment = HorizontalAlignment.Right;
		value.AddThemeColorOverride("font_color", Palette.TextLight);
		value.AddThemeFontSizeOverride("font_size", 16);
		grid.AddChild(value);
	}
}
