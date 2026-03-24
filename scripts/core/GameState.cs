using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ARPG;

/// <summary>
/// Static state passed between scenes. Holds choices made before gameplay starts
/// and persistent data that survives room transitions.
/// </summary>
public static partial class GameState
{
    private const string RunHistorySavePath = "user://run_history.json";
    private static readonly JsonSerializerOptions RunHistoryJsonOptions = CreateRunHistoryJsonOptions();
    private static readonly List<RunScoreEntry> _runHistory = new();
    private static int _currentRunMonstersKilled;
    private static int _currentRunBossesDefeated;
    private static int _currentRunDamageDone;
    private static int _currentRunHighestDamage;
    private static bool _currentRunFinalized;
    private static bool _runHistoryLoaded;

    public static Archetype SelectedArchetype { get; set; } = Archetype.Fighter;

    /// <summary>Current room number (1-based). 3 rooms total; room 3 has the boss.</summary>
    public static int CurrentRoom { get; set; } = 1;
    public const int TotalRooms = 3;
    public const int BossRoom = 3;

    /// <summary>
    /// Persistent player stats that carry over between rooms.
    /// Null until first room starts - PlayerController creates and stores it.
    /// </summary>
    public static PlayerStats PersistentStats { get; set; }

    /// <summary>Excess dark energy carried from the previous chunk.</summary>
    public static int DarkEnergyCarryOver { get; set; }

    /// <summary>Completed runs for the current app session, newest first.</summary>
    public static IReadOnlyList<RunScoreEntry> RunHistory
    {
        get
        {
            EnsureRunHistoryLoaded();
            return _runHistory;
        }
    }

    /// <summary>Call when starting a new run from archetype select.</summary>
    public static void StartNewRun(Archetype archetype)
    {
        SelectedArchetype = archetype;
        RestartRun();
    }

    public static void RestartRun()
    {
        CurrentRoom = 1;
        PersistentStats = null;
        DarkEnergyCarryOver = 0;
        ChunkNames.ResetUsed();
        ResetCurrentRunScore();
    }

    public static void RecordDamageDone(int amount)
    {
        if (_currentRunFinalized || amount <= 0)
            return;

        _currentRunDamageDone += amount;
        _currentRunHighestDamage = System.Math.Max(_currentRunHighestDamage, amount);
    }

    public static void RecordKill(bool isBoss)
    {
        if (_currentRunFinalized)
            return;

        if (isBoss)
            _currentRunBossesDefeated++;
        else
            _currentRunMonstersKilled++;
    }

    public static void FinalizeCurrentRun(RunOutcome outcome, PlayerStats stats)
    {
        EnsureRunHistoryLoaded();

        if (_currentRunFinalized)
            return;

        _currentRunFinalized = true;
        _runHistory.Insert(0, new RunScoreEntry
        {
            RunNumber = GetNextRunNumber(),
            Outcome = outcome,
            Archetype = SelectedArchetype,
            RoomReached = CurrentRoom,
            TotalRooms = TotalRooms,
            MonstersKilled = _currentRunMonstersKilled,
            BossesDefeated = _currentRunBossesDefeated,
            DamageDone = _currentRunDamageDone,
            HighestDamage = _currentRunHighestDamage,
            Stats = BuildStatLines(stats)
        });
        SaveRunHistory();
    }

    private static void ResetCurrentRunScore()
    {
        _currentRunMonstersKilled = 0;
        _currentRunBossesDefeated = 0;
        _currentRunDamageDone = 0;
        _currentRunHighestDamage = 0;
        _currentRunFinalized = false;
    }

    private static List<RunStatLine> BuildStatLines(PlayerStats stats)
    {
        var lines = new List<RunStatLine>();
        if (stats == null)
            return lines;

        lines.Add(new RunStatLine
        {
            Name = "Current HP",
            Value = $"{stats.CurrentHp}/{stats.MaxHp}"
        });

        foreach (var target in StatTargetInfo.All)
        {
            lines.Add(new RunStatLine
            {
                Name = StatTargetInfo.DisplayName(target),
                Value = StatTargetInfo.FormatStatValue(target, stats.GetEffectiveStatValue(target))
            });
        }

        return lines;
    }

    private static void EnsureRunHistoryLoaded()
    {
        if (_runHistoryLoaded)
            return;

        _runHistoryLoaded = true;
        LoadRunHistory();
    }

    private static void LoadRunHistory()
    {
        string path = ProjectSettings.GlobalizePath(RunHistorySavePath);
        if (!File.Exists(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            var loadedEntries = JsonSerializer.Deserialize<List<RunScoreEntry>>(json, RunHistoryJsonOptions);

            _runHistory.Clear();
            if (loadedEntries == null)
                return;

            for (int i = 0; i < loadedEntries.Count; i++)
                _runHistory.Add(SanitizeRunScoreEntry(loadedEntries[i], i + 1));
        }
        catch (System.Exception exception)
        {
            _runHistory.Clear();
            GD.PushWarning($"Failed to load run history from {RunHistorySavePath}: {exception.Message}");
        }
    }

    private static void SaveRunHistory()
    {
        string path = ProjectSettings.GlobalizePath(RunHistorySavePath);

        try
        {
            string directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            string json = JsonSerializer.Serialize(_runHistory, RunHistoryJsonOptions);
            File.WriteAllText(path, json);
        }
        catch (System.Exception exception)
        {
            GD.PushWarning($"Failed to save run history to {RunHistorySavePath}: {exception.Message}");
        }
    }

    private static int GetNextRunNumber()
    {
        return _runHistory.Count == 0 ? 1 : _runHistory[0].RunNumber + 1;
    }

    private static RunScoreEntry SanitizeRunScoreEntry(RunScoreEntry entry, int fallbackRunNumber)
    {
        if (entry == null)
        {
            return new RunScoreEntry
            {
                RunNumber = fallbackRunNumber,
                Outcome = RunOutcome.Defeat,
                TotalRooms = TotalRooms,
                Stats = new List<RunStatLine>()
            };
        }

        entry.RunNumber = entry.RunNumber > 0 ? entry.RunNumber : fallbackRunNumber;
        entry.TotalRooms = entry.TotalRooms > 0 ? entry.TotalRooms : TotalRooms;
        entry.Stats ??= new List<RunStatLine>();

        for (int i = 0; i < entry.Stats.Count; i++)
        {
            entry.Stats[i] ??= new RunStatLine
            {
                Name = $"Stat {i + 1}",
                Value = "0"
            };

            entry.Stats[i].Name ??= $"Stat {i + 1}";
            entry.Stats[i].Value ??= "0";
        }

        return entry;
    }

    private static JsonSerializerOptions CreateRunHistoryJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
