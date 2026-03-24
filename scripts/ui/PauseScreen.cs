using Godot;

namespace ARPG;

public partial class PauseScreen : Control
{
    [Signal]
    public delegate void ViewStatsRequestedEventHandler();

    private Button _resumeButton;
    private Button _statsButton;
    private Button _quitButton;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;

        // Semi-transparent dark overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // Center container
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center);
        vbox.GrowHorizontal = GrowDirection.Both;
        vbox.GrowVertical = GrowDirection.Both;
        vbox.CustomMinimumSize = new Vector2(300, 0);
        vbox.AddThemeConstantOverride("separation", 16);
        AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "PAUSED";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 36);
        vbox.AddChild(title);

        // Resume button
        _resumeButton = new Button();
        _resumeButton.Text = "Resume";
        Palette.StyleButton(_resumeButton, 22);
        _resumeButton.Pressed += OnResume;
        vbox.AddChild(_resumeButton);

        // Stats button
        _statsButton = new Button();
        _statsButton.Text = "View Stats";
        Palette.StyleButton(_statsButton, 22);
        _statsButton.Pressed += OnViewStats;
        vbox.AddChild(_statsButton);

        // Quit to menu
        _quitButton = new Button();
        _quitButton.Text = "Quit to Menu";
        Palette.StyleButton(_quitButton, 22);
        _quitButton.Pressed += OnQuit;
        vbox.AddChild(_quitButton);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameKeys.Pause))
        {
            if (Visible)
                OnResume();
            else
                ShowPause();

            GetViewport().SetInputAsHandled();
        }
    }

    private void ShowPause()
    {
        Visible = true;
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void OnResume()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    private void OnViewStats()
    {
        Visible = false;
        // Keep the tree paused while the stats overlay is open.
        EmitSignal(SignalName.ViewStatsRequested);
    }

    private void OnQuit()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(Scenes.MainMenu);
    }
}
