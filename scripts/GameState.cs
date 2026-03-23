namespace ARPG;

/// <summary>
/// Static state passed between scenes. Holds choices made before gameplay starts.
/// </summary>
public static class GameState
{
    public static Archetype SelectedArchetype { get; set; } = Archetype.Fighter;
}
