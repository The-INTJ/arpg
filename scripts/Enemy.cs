using Godot;

namespace ARPG;

public partial class Enemy : StaticBody3D
{
    public int Hp = 5;
    public int AttackDamage = 2;

    public void TakeDamage(int amount)
    {
        Hp -= amount;
        if (Hp <= 0)
            Die();
    }

    private void Die()
    {
        QueueFree();
    }

    public void AttackPlayer(PlayerController player)
    {
        player.Hp -= AttackDamage;
    }
}
