using Godot;

namespace ARPG;

public partial class GameOverScreen : Control
{
    public override void _Ready()
    {
        GetNode<ColorRect>("Background").Color = Palette.BgDark;

        var title = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 72);

        var retryButton = GetNode<Button>("CenterContainer/VBoxContainer/RetryButton");
        Palette.StyleButton(retryButton, 24);
        retryButton.Pressed += OnRetryPressed;

        var menuButton = GetNode<Button>("CenterContainer/VBoxContainer/MainMenuButton");
        Palette.StyleButton(menuButton, 24);
        menuButton.Pressed += OnMainMenuPressed;
    }

    private void OnRetryPressed()
    {
        GameState.RestartRun();
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }

    private void OnMainMenuPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
