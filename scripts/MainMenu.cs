using Godot;

namespace ARPG;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<ColorRect>("Background").Color = Palette.BgDark;

        var title = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 72);

        var subtitle = GetNode<Label>("CenterContainer/VBoxContainer/SubtitleLabel");
        subtitle.AddThemeColorOverride("font_color", Palette.TextLight);
        subtitle.AddThemeFontSizeOverride("font_size", 24);

        var playBtn = GetNode<Button>("CenterContainer/VBoxContainer/PlayButton");
        Palette.StyleButton(playBtn, 28);
        playBtn.Pressed += OnPlayPressed;
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }
}
