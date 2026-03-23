using Godot;

namespace ARPG;

public partial class VictoryScreen : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/PlayAgainButton").Pressed += OnPlayAgainPressed;
        GetNode<Button>("VBoxContainer/QuitButton").Pressed += OnQuitPressed;
    }

    private void OnPlayAgainPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
