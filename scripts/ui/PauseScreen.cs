using Godot;

namespace ARPG;

public partial class PauseScreen : Control
{
    [Signal]
    public delegate void ViewStatsRequestedEventHandler();

    private DeveloperToolsManager _developerTools;
    private VBoxContainer _menuVBox;
    private Button _resumeButton;
    private Button _godModeButton;
    private Button _statsButton;
    private Button _quitButton;
    private GodModePanel _godModePanel;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        ProcessMode = ProcessModeEnum.Always;

        _menuVBox = GetNode<VBoxContainer>("VBox");
        _resumeButton = GetNode<Button>("VBox/ResumeButton");
        _godModeButton = GetNode<Button>("VBox/GodModeButton");
        _statsButton = GetNode<Button>("VBox/StatsButton");
        _quitButton = GetNode<Button>("VBox/QuitButton");
        _godModePanel = GetNode<GodModePanel>("GodModePanel");

        _resumeButton.Pressed += OnResume;
        _godModeButton.Pressed += OnGodMode;
        _statsButton.Pressed += OnViewStats;
        _quitButton.Pressed += OnQuit;
        _godModePanel.BackRequested += ShowMenuPage;
    }

    public void Init(DeveloperToolsManager developerTools)
    {
        if (_developerTools == developerTools)
            return;

        if (_developerTools != null)
            _developerTools.GodMode.Changed -= UpdateGodModeButton;

        _developerTools = developerTools;
        _godModePanel.Init(developerTools);
        if (_developerTools != null)
            _developerTools.GodMode.Changed += UpdateGodModeButton;

        UpdateGodModeButton();
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
        if (_developerTools?.GodMode.Enabled == true)
            ShowGodModePage();
        else
            ShowMenuPage();
    }

    private void OnResume()
    {
        Visible = false;
        _godModePanel.Visible = false;
        _menuVBox.Visible = false;
        GetTree().Paused = false;
    }

    private void OnViewStats()
    {
        Visible = false;
        // Keep the tree paused while the stats overlay is open.
        EmitSignal(SignalName.ViewStatsRequested);
    }

    private void OnGodMode()
    {
        if (_developerTools == null)
            return;

        if (!_developerTools.GodMode.Enabled)
            _developerTools.EnableGodMode();

        ShowGodModePage();
    }

    private void OnQuit()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(Scenes.MainMenu);
    }

    private void ShowMenuPage()
    {
        _menuVBox.Visible = true;
        _godModePanel.Visible = false;
        UpdateGodModeButton();
    }

    private void ShowGodModePage()
    {
        _menuVBox.Visible = false;
        _godModePanel.Visible = true;
        _godModePanel.Refresh();
    }

    private void UpdateGodModeButton()
    {
        _godModeButton.Text = _developerTools?.GodMode.Enabled == true
            ? "God Mode Tools"
            : "Enable God Mode";
    }
}
