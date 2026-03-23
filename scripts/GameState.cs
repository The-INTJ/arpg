namespace ARPG;

/// <summary>
/// Static state passed between scenes. Holds choices made before gameplay starts
/// and persistent data that survives room transitions.
/// </summary>
public static class GameState
{
    public static Archetype SelectedArchetype { get; set; } = Archetype.Fighter;

    /// <summary>Current room number (1-based). 3 rooms total; room 3 has the boss.</summary>
    public static int CurrentRoom { get; set; } = 1;
    public const int TotalRooms = 3;
    public const int BossRoom = 3;

    /// <summary>
    /// Persistent player stats that carry over between rooms.
    /// Null until first room starts — PlayerController creates and stores it.
    /// </summary>
    public static PlayerStats PersistentStats { get; set; }

    /// <summary>Call when starting a new run from archetype select.</summary>
    public static void StartNewRun(Archetype archetype)
    {
        SelectedArchetype = archetype;
        CurrentRoom = 1;
        PersistentStats = null;
    }
}
