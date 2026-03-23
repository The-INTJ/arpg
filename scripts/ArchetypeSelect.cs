using Godot;

namespace ARPG;

public partial class ArchetypeSelect : Control
{
    private Label _descriptionLabel;

    public override void _Ready()
    {
        GetNode<ColorRect>("Background").Color = Palette.BgDark;

        var title = GetNode<Label>("CenterContainer/VBoxContainer/TitleLabel");
        title.AddThemeColorOverride("font_color", Palette.Accent);
        title.AddThemeFontSizeOverride("font_size", 48);

        _descriptionLabel = GetNode<Label>("CenterContainer/VBoxContainer/DescriptionLabel");
        _descriptionLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        _descriptionLabel.AddThemeFontSizeOverride("font_size", 18);
        _descriptionLabel.Text = "Choose your path...";

        var buttonBox = GetNode<HBoxContainer>("CenterContainer/VBoxContainer/ButtonBox");

        foreach (Archetype archetype in new[] { Archetype.Fighter, Archetype.Archer, Archetype.Mage })
        {
            var btn = new Button();
            btn.Text = ArchetypeData.DisplayName(archetype);
            btn.CustomMinimumSize = new Vector2(200, 60);
            Palette.StyleButton(btn, 24);
            buttonBox.AddChild(btn);

            var a = archetype; // capture for lambda
            btn.MouseEntered += () => OnHover(a);
            btn.Pressed += () => OnSelect(a);
        }
    }

    private void OnHover(Archetype archetype)
    {
        _descriptionLabel.Text = ArchetypeData.Description(archetype);
    }

    private void OnSelect(Archetype archetype)
    {
        GameState.SelectedArchetype = archetype;
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }
}
