using System;

namespace ARPG;

public enum AbilityType
{
    None,
    Cleave,    // Fighter: hits twice
    Snipe,     // Archer: 3x damage
    Fireball   // Mage: 2.5x damage
}

public partial class Ability
{
    public AbilityType Type { get; }
    public string Name { get; }
    public float CooldownSeconds { get; }
    public float CooldownRemainingSeconds { get; private set; }

    public bool IsReady => CooldownRemainingSeconds <= 0.001f;
    public int TurnsRemaining => (int)Math.Ceiling(CooldownRemainingSeconds);

    public bool IsRanged => Type is AbilityType.Snipe or AbilityType.Fireball;

    public float ProjectileSpeed => Type switch
    {
        AbilityType.Snipe => 14.0f,
        AbilityType.Fireball => 9.0f,
        _ => 0.0f,
    };

    public float ProjectileVisualRadius => Type switch
    {
        AbilityType.Fireball => 0.08f,
        AbilityType.Snipe => 0.04f,
        _ => 0.05f,
    };

    private Ability(AbilityType type, string name, float cooldownSeconds)
    {
        Type = type;
        Name = name;
        CooldownSeconds = cooldownSeconds;
    }

    public void Use()
    {
        CooldownRemainingSeconds = CooldownSeconds;
    }

    public void TickCooldown(float deltaSeconds)
    {
        CooldownRemainingSeconds = Math.Max(0.0f, CooldownRemainingSeconds - Math.Max(0.0f, deltaSeconds));
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
        Archetype.Fighter => new Ability(AbilityType.Cleave, "Cleave", 1.5f),
        Archetype.Archer => new Ability(AbilityType.Snipe, "Snipe", 2.4f),
        Archetype.Mage => new Ability(AbilityType.Fireball, "Fireball", 2.4f),
        _ => null
    };

    public static Ability ForWeapon(Weapon weapon)
    {
        if (weapon == null)
            return null;

        return weapon.Ability switch
        {
            AbilityType.Cleave => new Ability(AbilityType.Cleave, "Cleave", 1.5f),
            AbilityType.Snipe => new Ability(AbilityType.Snipe, "Snipe", 2.4f),
            AbilityType.Fireball => new Ability(AbilityType.Fireball, "Fireball", 2.4f),
            _ => null
        };
    }
}
