using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class RunHistorySnippet : VBoxContainer
{
	private static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/ui_components/RunHistoryCard.tscn");

	private VBoxContainer _historyList;

	public override void _Ready()
	{
		_historyList = GetNode<VBoxContainer>("ScrollContainer/HistoryList");
		RefreshFromGameState();
	}

	public void RefreshFromGameState()
	{
		ShowRuns(GameState.RunHistory);
	}

	public void ShowRuns(IReadOnlyList<RunScoreEntry> entries)
	{
		foreach (Node child in _historyList.GetChildren())
			child.QueueFree();

		if (entries == null || entries.Count == 0)
		{
			var empty = new Label();
			empty.Text = "No completed runs yet.";
			empty.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.7f));
			empty.AddThemeFontSizeOverride("font_size", 16);
			empty.HorizontalAlignment = HorizontalAlignment.Center;
			_historyList.AddChild(empty);
			return;
		}

		for (int i = 0; i < entries.Count; i++)
		{
			var card = CardScene.Instantiate<RunHistoryCard>();
			_historyList.AddChild(card);
			card.Populate(entries[i], i == 0);
		}
	}
}
