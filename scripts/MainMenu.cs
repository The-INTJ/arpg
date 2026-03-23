using Godot;

namespace ARPG;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<ColorRect>("Background").Color = Palette.BgDark;

		var playButton = GetNode<Button>("Background/GridContainer2/GridContainer/PlayButton");
		Palette.StyleButton(playButton, 28);
		playButton.Pressed += OnPlayPressed;

		var quitButton = GetNode<Button>("Background/GridContainer2/GridContainer/QuitButton");
		Palette.StyleButton(quitButton, 28);
		quitButton.Pressed += OnQuitPressed;
	}

	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ArchetypeSelect.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
