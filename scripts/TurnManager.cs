using Godot;

namespace ARPG;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    Busy,
    Victory,
    Defeat
}

public partial class TurnManager : Node
{
    public TurnState State { get; private set; } = TurnState.PlayerTurn;

    [Signal]
    public delegate void TurnChangedEventHandler(int newState);

    public void SetState(TurnState state)
    {
        State = state;
        EmitSignal(SignalName.TurnChanged, (int)state);
    }

    public bool IsPlayerTurn => State == TurnState.PlayerTurn;
}
