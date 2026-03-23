namespace ARPG;

public enum AbilityType
{
    None,
    Cleave,    // Fighter: hits twice
    Snipe,     // Archer: 3x damage
    Fireball   // Mage: 2.5x damage
}

public class Ability
{
    public AbilityType Type { get; }
    public string Name { get; }
    public int Cooldown { get; }
    public int TurnsRemaining { get; set; }

    public bool IsReady => TurnsRemaining <= 0;

    private Ability(AbilityType type, string name, int cooldown)
    {
        Type = type;
        Name = name;
        Cooldown = cooldown;
    }

    public void Use() => TurnsRemaining = Cooldown;

    public void TickCooldown()
    {
        if (TurnsRemaining > 0) TurnsRemaining--;
    }

    /// <summary>
    /// Returns the total damage multiplier for this ability.
    /// Cleave = 2 hits at 1x = 2x total. Snipe = 3x. Fireball = 2.5x.
    /// </summary>
    public float DamageMultiplier => Type switch
    {
        AbilityType.Cleave => 2.0f,
        AbilityType.Snipe => 3.0f,
        AbilityType.Fireball => 2.5f,
        _ => 1.0f
    };

    public static Ability ForArchetype(Archetype archetype) => archetype switch
    {
        Archetype.Fighter => new Ability(AbilityType.Cleave, "Cleave", 2),
        Archetype.Archer => new Ability(AbilityType.Snipe, "Snipe", 3),
        Archetype.Mage => new Ability(AbilityType.Fireball, "Fireball", 3),
        _ => null
    };

    public static Ability ForWeapon(Weapon weapon)
    {
        if (weapon == null) return null;
        return weapon.Ability switch
        {
            AbilityType.Cleave => new Ability(AbilityType.Cleave, "Cleave", 2),
            AbilityType.Snipe => new Ability(AbilityType.Snipe, "Snipe", 3),
            AbilityType.Fireball => new Ability(AbilityType.Fireball, "Fireball", 3),
            _ => null
        };
    }
}
