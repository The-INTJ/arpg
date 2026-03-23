using Godot;

namespace ARPG;

public enum TurnState
{
    Exploring,
    PlayerTurn,
    EnemyTurn,
    Busy,
    Victory,
    Defeat
}

public partial class TurnManager : Node
{
    public TurnState State { get; private set; } = TurnState.Exploring;

    [Signal]
    public delegate void TurnChangedEventHandler(int newState);

    public void SetState(TurnState state)
    {
        State = state;
        EmitSignal(SignalName.TurnChanged, (int)state);
    }

    public bool IsExploring => State == TurnState.Exploring;
    public bool IsPlayerTurn => State == TurnState.PlayerTurn;
    public bool InCombat => State == TurnState.PlayerTurn || State == TurnState.EnemyTurn || State == TurnState.Busy;
}
