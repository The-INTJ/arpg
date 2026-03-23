namespace ARPG;

public enum Archetype
{
    Fighter,
    Archer,
    Mage
}

public static class ArchetypeData
{
    public static void ApplyTo(Archetype archetype, PlayerStats stats)
    {
        switch (archetype)
        {
            case Archetype.Fighter:
                stats.SetBaseStats(maxHp: 20, attackDamage: 5, moveSpeed: 4.5f, attackRange: 3.0f);
                break;
            case Archetype.Archer:
                stats.SetBaseStats(maxHp: 12, attackDamage: 3, moveSpeed: 5.5f, attackRange: 6.0f);
                break;
            case Archetype.Mage:
                stats.SetBaseStats(maxHp: 10, attackDamage: 6, moveSpeed: 4.0f, attackRange: 5.0f);
                break;
        }
        stats.ResetHp();
    }

    public static string DisplayName(Archetype a) => a.ToString();

    public static string Description(Archetype a) => a switch
    {
        Archetype.Fighter => "Tough and reliable.\nHP: 20  ATK: 5  SPD: 4.5  Range: 3\nAbility: Cleave (double hit)",
        Archetype.Archer => "Fast with long reach.\nHP: 12  ATK: 3  SPD: 5.5  Range: 6\nAbility: Snipe (3x damage)",
        Archetype.Mage => "Fragile but devastating.\nHP: 10  ATK: 6  SPD: 4.0  Range: 5\nAbility: Fireball (2.5x damage)",
        _ => ""
    };
}
