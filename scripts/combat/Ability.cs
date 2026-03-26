using System;
using Godot;

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
    public AttackDefinition Attack { get; }

    public bool IsReady => CooldownRemainingSeconds <= 0.001f;
    public int TurnsRemaining => (int)Math.Ceiling(CooldownRemainingSeconds);

    public bool IsRanged => Type is AbilityType.Snipe or AbilityType.Fireball;

    public float ProjectileSpeed => Attack.ProjectileSpeed;
    public float ProjectileVisualRadius => Attack.ProjectileVisualRadius;

    private Ability(AbilityType type, string name, float cooldownSeconds, AttackDefinition attack)
    {
        Type = type;
        Name = name;
        CooldownSeconds = cooldownSeconds;
        Attack = attack;
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
        Archetype.Fighter => new Ability(AbilityType.Cleave, "Cleave", 1.5f, BuildDefinition(AbilityType.Cleave)),
        Archetype.Archer => new Ability(AbilityType.Snipe, "Snipe", 2.4f, BuildDefinition(AbilityType.Snipe)),
        Archetype.Mage => new Ability(AbilityType.Fireball, "Fireball", 2.4f, BuildDefinition(AbilityType.Fireball)),
        _ => null
    };

    public static Ability ForWeapon(Weapon weapon)
    {
        if (weapon == null)
            return null;

        return weapon.Ability switch
        {
            AbilityType.Cleave => new Ability(AbilityType.Cleave, "Cleave", 1.5f, BuildDefinition(AbilityType.Cleave)),
            AbilityType.Snipe => new Ability(AbilityType.Snipe, "Snipe", 2.4f, BuildDefinition(AbilityType.Snipe)),
            AbilityType.Fireball => new Ability(AbilityType.Fireball, "Fireball", 2.4f, BuildDefinition(AbilityType.Fireball)),
            _ => null
        };
    }

    private static AttackDefinition BuildDefinition(AbilityType type) => type switch
    {
        AbilityType.Cleave => AttackDefinition.CreateMelee(
            "cleave",
            AttackVolumeDefinition.CreateBox(
                new Vector3(1.6f, 1.0f, 1.15f),
                new Vector3(0.0f, 0.0f, 0.82f)),
            AttackTimeline.Create(0.14f, 0.05f, 0.28f),
            maxTargets: 4,
            damageMultiplier: 2.0f),
        AbilityType.Snipe => AttackDefinition.CreateProjectile(
            "snipe",
            AttackVolumeDefinition.CreateSphere(0.12f, new Vector3(0.0f, 0.0f, 0.0f)),
            AttackTimeline.Create(0.04f, 0.04f, 0.18f),
            damageMultiplier: 3.0f,
            projectileSpeed: 14.0f,
            projectileVisualRadius: 0.04f),
        AbilityType.Fireball => AttackDefinition.CreateProjectile(
            "fireball",
            AttackVolumeDefinition.CreateSphere(0.18f, new Vector3(0.0f, 0.0f, 0.0f)),
            AttackTimeline.Create(0.08f, 0.04f, 0.24f),
            damageMultiplier: 2.5f,
            projectileSpeed: 9.0f,
            projectileVisualRadius: 0.08f),
        _ => AttackDefinition.CreateMelee(
            "none",
            AttackVolumeDefinition.CreateBox(
                new Vector3(0.8f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.7f)),
            AttackTimeline.Create(0.08f, 0.04f, 0.18f)),
    };
}
