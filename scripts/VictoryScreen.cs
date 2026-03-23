using Godot;

namespace ARPG;

public partial class VictoryScreen : Control
{
    public override void _Ready()
    {
        GetNode<ColorRect>("Background").Color = Palette.BgDark;

        var title = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 72);

        var playAgainBtn = GetNode<Button>("CenterContainer/VBoxContainer/PlayAgainButton");
        Palette.StyleButton(playAgainBtn, 24);
        playAgainBtn.Pressed += OnPlayAgainPressed;

        var quitBtn = GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");
        Palette.StyleButton(quitBtn, 24);
        quitBtn.Pressed += OnQuitPressed;
    }

    private void OnPlayAgainPressed()
    {
        GameState.RestartRun();
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
