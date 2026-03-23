namespace ARPG;

/// <summary>
/// Holds a player's base stats. Archetype selection and modifiers will layer on top of these.
/// Designed so multiple players can each own an instance.
/// </summary>
public partial class PlayerStats
{
    // Base values — these are the unmodified defaults before archetype or modifier changes
    public int MaxHp { get; set; } = 15;
    public int AttackDamage { get; set; } = 4;
    public float MoveSpeed { get; set; } = 5.0f;
    public float SprintMultiplier { get; set; } = 2.0f;
    public float AttackRange { get; set; } = 3.5f;
    public float HpRegenRate { get; set; } = 0.2f; // 1 HP per 5 seconds

    // Current mutable state
    public int CurrentHp { get; set; }

    public PlayerStats()
    {
        CurrentHp = MaxHp;
    }

    public void ResetHp() => CurrentHp = MaxHp;

    public bool IsAlive => CurrentHp > 0;
}
