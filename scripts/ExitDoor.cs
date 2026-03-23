using Godot;

namespace ARPG;

public partial class ExitDoor : Node3D
{
	private MeshInstance3D _barrier;
	private StaticBody3D _blocker;
	private MeshInstance3D _openIndicator;
	private Area3D _trigger;
	private bool _unlocked;

	public override void _Ready()
	{
		_barrier = GetNode<MeshInstance3D>("Barrier");
		_blocker = GetNode<StaticBody3D>("Blocker");
		_openIndicator = GetNode<MeshInstance3D>("OpenIndicator");
		_trigger = GetNode<Area3D>("Trigger");
		_trigger.BodyEntered += OnBodyEntered;
	}

	public void Unlock()
	{
		_unlocked = true;
		_barrier.Visible = false;
		_blocker.CollisionLayer = 0;
		_blocker.CollisionMask = 0;
		_openIndicator.Visible = true;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_unlocked && body is PlayerController)
		{
			if (GameState.CurrentRoom >= GameState.TotalRooms)
			{
				// All rooms cleared — victory!
				GetTree().ChangeSceneToFile("res://scenes/VictoryScreen.tscn");
			}
			else
			{
				// Advance to next room
				GameState.CurrentRoom++;
				GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
			}
		}
	}
}
