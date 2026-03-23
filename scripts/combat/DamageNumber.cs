using Godot;

namespace ARPG;

public partial class DamageNumber : Node3D
{
    private Label3D _label;

    public override void _Ready()
    {
        _label = new Label3D();
        _label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _label.NoDepthTest = true;
        _label.FixedSize = true;
        _label.PixelSize = 0.006f;
        _label.FontSize = 24;
        _label.OutlineSize = 6;
        AddChild(_label);
    }

    public void Setup(int amount, bool isPlayerDamage)
    {
        SetupText(
            amount.ToString(),
            isPlayerDamage
                ? Palette.DamagePlayer
                : Palette.DamageEnemy);
    }

    public void SetupText(string text, Color color)
    {
        _label.Text = text;
        _label.Modulate = color;
        _label.OutlineModulate = Palette.OutlineBlack;

        Animate();
    }

    private void Animate()
    {
        var tween = CreateTween();
        tween.SetParallel(true);

        // Float upward
        var endPos = GlobalPosition + Vector3.Up * 1.0f;
        tween.TweenProperty(this, "global_position", endPos, 0.8f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // Fade out
        tween.TweenProperty(_label, "modulate:a", 0.0f, 0.8f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }
}
