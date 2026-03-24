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

        _resumeButton = GetNode<Button>("VBox/ResumeButton");
        _statsButton = GetNode<Button>("VBox/StatsButton");
        _quitButton = GetNode<Button>("VBox/QuitButton");

        _resumeButton.Pressed += OnResume;
        _statsButton.Pressed += OnViewStats;
        _quitButton.Pressed += OnQuit;
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
