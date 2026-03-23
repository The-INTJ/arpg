using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    public int MaxHp = 10;
    public int Hp = 10;
    public int AttackDamage = 2;
    public float SightRange = 7.0f;
    public bool IsBoss { get; private set; }

    public bool HasAggro { get; set; }

    public float HpPercent => MaxHp > 0 ? (float)Hp / MaxHp : 0f;
    public bool IsDead => Hp <= 0;

    /// <summary>
    /// Scale this enemy's stats for the given room number (1-based).
    /// Room 1 = baseline, Room 2 = tougher, Room 3 = hardest.
    /// </summary>
    public void ScaleForRoom(int room)
    {
        float hpMult = 1.0f + (room - 1) * 0.5f;   // 1.0, 1.5, 2.0
        float atkMult = 1.0f + (room - 1) * 0.35f;  // 1.0, 1.35, 1.7
        MaxHp = (int)(MaxHp * hpMult);
        Hp = MaxHp;
        AttackDamage = (int)(AttackDamage * atkMult);
    }

    /// <summary>
    /// Promote this enemy to a boss. Significantly more HP and damage, larger sprite.
    /// </summary>
    public void MakeBoss()
    {
        IsBoss = true;
        MaxHp = (int)(MaxHp * 2.5f);
        Hp = MaxHp;
        AttackDamage = (int)(AttackDamage * 1.8f);
        SightRange = 12.0f;
    }

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
        label.Text = IsBoss ? "!!!" : "!";
        label.FontSize = IsBoss ? 80 : 64;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.NoDepthTest = true;
        label.FixedSize = true;
        label.PixelSize = 0.01f;
        label.Modulate = new Color(1.0f, 0.3f, 0.2f);
        label.OutlineSize = 12;
        label.OutlineModulate = new Color(0, 0, 0);
        label.Position = new Vector3(0, IsBoss ? 2.2f : 1.6f, 0);
        label.Name = "AggroIndicator";
        AddChild(label);

        var tween = CreateTween();
        tween.TweenInterval(0.6f);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() => label.QueueFree()));
    }
}
