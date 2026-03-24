using Godot;

namespace ARPG;

public partial class GameOverScreen : Control
{
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNode<ColorRect>("Background").Color = new Color(0.11f, 0.06f, 0.05f);

        var title = GetNode<Label>("MarginContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 74);

        var retryButton = GetNode<Button>("MarginContainer/VBoxContainer/Buttons/RetryButton");
        Palette.StyleButton(retryButton, 24);
        retryButton.Pressed += OnRetryPressed;

        var menuButton = GetNode<Button>("MarginContainer/VBoxContainer/Buttons/MainMenuButton");
        Palette.StyleButton(menuButton, 24);
        menuButton.Pressed += OnMainMenuPressed;
    }

    private void OnRetryPressed()
    {
        GameState.RestartRun();
        GetTree().ChangeSceneToFile(Scenes.Game);
    }

    private void OnMainMenuPressed()
    {
        GetTree().ChangeSceneToFile(Scenes.MainMenu);
    }
}
