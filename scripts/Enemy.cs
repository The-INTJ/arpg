using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    public int MaxHp = 10;
    public int Hp = 10;
    public int AttackDamage = 2;

    public float HpPercent => MaxHp > 0 ? (float)Hp / MaxHp : 0f;

    public void TakeDamage(int amount)
    {
        Hp = Mathf.Max(0, Hp - amount);
    }

    public bool IsDead => Hp <= 0;

    public void Die()
    {
        QueueFree();
    }

    public void AttackPlayer(PlayerController player)
    {
        player.Hp -= AttackDamage;
    }
}
