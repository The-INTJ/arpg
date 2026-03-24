using Godot;

namespace ARPG;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<ColorRect>("Background").Color = new Color(0.10f, 0.07f, 0.05f);

		var titleLabel = GetNode<Label>("CenterContainer/Card/Margin/VBox/TitleLabel");
		titleLabel.AddThemeColorOverride("font_color", Palette.TextLight);

		var subtitleLabel = GetNode<Label>("CenterContainer/Card/Margin/VBox/SubtitleLabel");
		subtitleLabel.AddThemeColorOverride("font_color", new Color(Palette.TextLight, 0.82f));

		var playButton = GetNode<Button>("CenterContainer/Card/Margin/VBox/Buttons/PlayButton");
		Palette.StyleButton(playButton, 30);
		playButton.Pressed += OnPlayPressed;

		var quitButton = GetNode<Button>("CenterContainer/Card/Margin/VBox/Buttons/QuitButton");
		Palette.StyleButton(quitButton, 26);
		quitButton.Pressed += OnQuitPressed;
	}

	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToFile(Scenes.ArchetypeSelect);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
