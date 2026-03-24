using Godot;

namespace ARPG;

public partial class ArchetypeSelect : Control
{
    private Label _descriptionLabel;

    public override void _Ready()
    {
        GetNode<ColorRect>("Background").Color = new Color(0.10f, 0.07f, 0.05f);

        var title = GetNode<Label>("MarginContainer/CenterContainer/SelectionCard/CardMargin/VBox/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 60);

        _descriptionLabel = GetNode<Label>("MarginContainer/CenterContainer/SelectionCard/CardMargin/VBox/DescriptionLabel");
        _descriptionLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        _descriptionLabel.AddThemeFontSizeOverride("font_size", 22);
        _descriptionLabel.Text = "Choose your path...";

        BindArchetypeButton("MarginContainer/CenterContainer/SelectionCard/CardMargin/VBox/ButtonBox/FighterButton", Archetype.Fighter);
        BindArchetypeButton("MarginContainer/CenterContainer/SelectionCard/CardMargin/VBox/ButtonBox/ArcherButton", Archetype.Archer);
        BindArchetypeButton("MarginContainer/CenterContainer/SelectionCard/CardMargin/VBox/ButtonBox/MageButton", Archetype.Mage);
        OnHover(Archetype.Fighter);
    }

    private void BindArchetypeButton(string path, Archetype archetype)
    {
        var button = GetNode<Button>(path);
        Palette.StyleButton(button, 24);
        button.MouseEntered += () => OnHover(archetype);
        button.Pressed += () => OnSelect(archetype);
    }

    private void OnHover(Archetype archetype)
    {
        _descriptionLabel.Text = ArchetypeData.Description(archetype);
    }

    private void OnSelect(Archetype archetype)
    {
        GameState.StartNewRun(archetype);
        GetTree().ChangeSceneToFile(Scenes.Game);
    }
}
