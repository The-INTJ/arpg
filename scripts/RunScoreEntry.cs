using System.Collections.Generic;

namespace ARPG;

public enum RunOutcome
{
    Victory,
    Defeat
}

public partial class RunScoreEntry
{
    public int RunNumber { get; set; }
    public RunOutcome Outcome { get; set; }
    public Archetype Archetype { get; set; }
    public int RoomReached { get; set; }
    public int TotalRooms { get; set; }
    public int MonstersKilled { get; set; }
    public int BossesDefeated { get; set; }
    public int DamageDone { get; set; }
    public int HighestDamage { get; set; }
    public List<RunStatLine> Stats { get; set; } = new();

    public string OutcomeLabel => Outcome == RunOutcome.Victory ? "Victory" : "Defeat";
}

public partial class RunStatLine
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
