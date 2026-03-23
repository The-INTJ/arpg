using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    public int MaxHp = 10;
    public int Hp = 10;
    public int AttackDamage = 2;
    public float SightRange = 7.0f;

    public bool HasAggro { get; set; }

    public float HpPercent => MaxHp > 0 ? (float)Hp / MaxHp : 0f;
    public bool IsDead => Hp <= 0;

    public void TakeDamage(int amount)
    {
        Hp = Mathf.Max(0, Hp - amount);
    }

    public void Die()
    {
        QueueFree();
    }

    public void AttackPlayer(PlayerController player)
    {
        player.Hp -= AttackDamage;
    }

    /// <summary>
    /// Shows a "!" indicator above this enemy. Called by GameManager on aggro.
    /// </summary>
    public void ShowAggroIndicator()
    {
        if (HasAggro) return;
        HasAggro = true;

        var label = new Label3D();
        label.Text = "!";
        label.FontSize = 64;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.NoDepthTest = true;
        label.FixedSize = true;
        label.PixelSize = 0.01f;
        label.Modulate = new Color(1.0f, 0.3f, 0.2f);
        label.OutlineSize = 12;
        label.OutlineModulate = new Color(0, 0, 0);
        label.Position = new Vector3(0, 1.6f, 0);
        label.Name = "AggroIndicator";
        AddChild(label);

        // Fade out after a moment
        var tween = CreateTween();
        tween.TweenInterval(0.6f);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
}
