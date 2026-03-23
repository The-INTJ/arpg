using Godot;

namespace ARPG;

public partial class ExitDoor : Node3D
{
	private MeshInstance3D _barrier;
	private Area3D _trigger;
	private bool _unlocked;

	public override void _Ready()
	{
		_barrier = GetNode<MeshInstance3D>("Barrier");
		_trigger = GetNode<Area3D>("Trigger");
		_trigger.BodyEntered += OnBodyEntered;
	}

	public void Unlock()
	{
		_unlocked = true;
		_barrier.Visible = false;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_unlocked && body is PlayerController)
		{
			GetTree().ChangeSceneToFile("res://scenes/VictoryScreen.tscn");
		}
	}
}
