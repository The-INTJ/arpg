using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class RunHistorySnippet : VBoxContainer
{
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
            var empty = CreateMutedLabel("No completed runs yet.");
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            _historyList.AddChild(empty);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
            _historyList.AddChild(CreateRunCard(entries[i], i == 0));
    }

    private Control CreateRunCard(RunScoreEntry entry, bool isLatest)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride("panel", CreateCardStyle(isLatest));

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        string runLabel = isLatest ? "Latest Run" : $"Run {entry.RunNumber}";
        var header = new Label();
        header.Text = $"{runLabel}  |  {entry.OutcomeLabel}";
        header.AddThemeColorOverride("font_color", isLatest ? Palette.Accent : Palette.TextLight);
        header.AddThemeFontSizeOverride("font_size", 22);
        header.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(header);

        var meta = CreateBodyLabel(
            $"{ArchetypeData.DisplayName(entry.Archetype)}  |  Room {entry.RoomReached}/{entry.TotalRooms}");
        meta.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(meta);

        vbox.AddChild(CreateSectionLabel("Score"));
        var scoreGrid = CreateDataGrid();
        AddGridRow(scoreGrid, "Monsters Killed", entry.MonstersKilled.ToString());
        AddGridRow(scoreGrid, "Bosses Defeated", entry.BossesDefeated.ToString());
        AddGridRow(scoreGrid, "Damage Done", entry.DamageDone.ToString());
        AddGridRow(scoreGrid, "Highest Damage", entry.HighestDamage.ToString());
        vbox.AddChild(scoreGrid);

        vbox.AddChild(CreateSectionLabel("Stats"));
        var statsGrid = CreateDataGrid();
        foreach (var stat in entry.Stats)
            AddGridRow(statsGrid, stat.Name, stat.Value);
        vbox.AddChild(statsGrid);

        return panel;
    }

    private static StyleBoxFlat CreateCardStyle(bool isLatest)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(Palette.BgDark, 0.94f);
        style.BorderColor = isLatest ? Palette.Accent : Palette.TextDisabled;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(10);
        style.SetContentMarginAll(14);
        return style;
    }

    private static GridContainer CreateDataGrid()
    {
        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 6);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return grid;
    }

    private static void AddGridRow(GridContainer grid, string labelText, string valueText)
    {
        var label = CreateBodyLabel(labelText);
        label.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.78f));
        grid.AddChild(label);

        var value = CreateBodyLabel(valueText);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.AddThemeColorOverride("font_color", Palette.TextLight);
        grid.AddChild(value);
    }

    private static Label CreateSectionLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", Palette.Accent);
        label.AddThemeFontSizeOverride("font_size", 18);
        return label;
    }

    private static Label CreateBodyLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", Palette.TextLight);
        label.AddThemeFontSizeOverride("font_size", 16);
        return label;
    }

    private static Label CreateMutedLabel(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.7f));
        return label;
    }
}
