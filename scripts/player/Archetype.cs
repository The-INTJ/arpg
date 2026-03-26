namespace ARPG;

public enum Archetype
{
    Fighter,
    Archer,
    Mage
}

public static class ArchetypeData
{
    private static readonly PlayerBaseStats FighterStats = new(
        MaxHp: 40,
        AttackDamage: 5,
        MoveSpeed: 5.0f,
        AttackReach: 1.55f,
        AttackSize: 1.0f,
        JumpHeight: 1.9f,
        JumpCount: 1);

    private static readonly PlayerBaseStats ArcherStats = new(
        MaxHp: 24,
        AttackDamage: 3,
        MoveSpeed: 6.0f,
        AttackReach: 1.75f,
        AttackSize: 1.0f,
        JumpHeight: 1.9f,
        JumpCount: 1);

    private static readonly PlayerBaseStats MageStats = new(
        MaxHp: 20,
        AttackDamage: 6,
        MoveSpeed: 4.5f,
        AttackReach: 1.65f,
        AttackSize: 1.0f,
        JumpHeight: 1.9f,
        JumpCount: 1);

    public static void ApplyTo(Archetype archetype, PlayerStats stats)
    {
        switch (archetype)
        {
            case Archetype.Fighter:
                stats.SetBaseStats(FighterStats);
                break;
            case Archetype.Archer:
                stats.SetBaseStats(ArcherStats);
                break;
            case Archetype.Mage:
                stats.SetBaseStats(MageStats);
                break;
        }
        stats.ResetHp();
    }

    public static string DisplayName(Archetype a) => a.ToString();

    public static string Description(Archetype a) => a switch
    {
        Archetype.Fighter => "Tough and reliable.\nHP: 40  ATK: 5  SPD: 5.0  Reach: 1.55\nAbility: Cleave (double hit)",
        Archetype.Archer => "Fast and evasive.\nHP: 24  ATK: 3  SPD: 6.0  Reach: 1.75\nAbility: Snipe (3x damage)",
        Archetype.Mage => "Fragile but devastating.\nHP: 20  ATK: 6  SPD: 4.5  Reach: 1.65\nAbility: Fireball (2.5x damage)",
        _ => ""
    };
}
