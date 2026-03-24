using Godot;

namespace ARPG;

public partial class VictoryScreen : Control
{
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNode<ColorRect>("Background").Color = new Color(0.08f, 0.08f, 0.05f);

        var title = GetNode<Label>("MarginContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 74);

        var playAgainBtn = GetNode<Button>("MarginContainer/VBoxContainer/Buttons/PlayAgainButton");
        Palette.StyleButton(playAgainBtn, 24);
        playAgainBtn.Pressed += OnPlayAgainPressed;

        var quitBtn = GetNode<Button>("MarginContainer/VBoxContainer/Buttons/QuitButton");
        Palette.StyleButton(quitBtn, 24);
        quitBtn.Pressed += OnQuitPressed;
    }

    private void OnPlayAgainPressed()
    {
        GameState.RestartRun();
        GetTree().ChangeSceneToFile(Scenes.Game);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
