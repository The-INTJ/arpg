using Godot;

namespace ARPG;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBoxContainer/PlayButton").Pressed += OnPlayPressed;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }
}
