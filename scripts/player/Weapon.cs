using System.Collections.Generic;
using Godot;

namespace ARPG;

/// <summary>
/// A weapon with a name, an ability, and stable per-stat modifier channels.
/// Each channel acts like a persistent slot for that stat while allowing stacking.
/// </summary>
public partial class Weapon
{
    private readonly Dictionary<StatTarget, WeaponStatChannel> _channelsByTarget = new();
    private readonly WeaponStatChannel[] _channels;

    public string Name { get; }
    public AbilityType Ability { get; }
    public AttackDefinition BasicAttack { get; }
    public IReadOnlyList<WeaponStatChannel> Channels => _channels;

    public Weapon(string name, AbilityType ability, AttackDefinition basicAttack)
    {
        Name = name;
        Ability = ability;
        BasicAttack = basicAttack;
        _channels = CreateChannels();

        foreach (var channel in _channels)
            _channelsByTarget[channel.Target] = channel;
    }

    public WeaponStatChannel GetChannel(StatTarget target) => _channelsByTarget[target];

    public void AddStartingModifier(Modifier modifier)
    {
        if (modifier == null)
            return;

        foreach (var appliedEffect in modifier.CreateAppliedEffectsFromFixedTargets())
            GetChannel(appliedEffect.Target).Add(appliedEffect);
    }

    public static Weapon ForArchetype(Archetype archetype) => archetype switch
    {
        Archetype.Fighter => Create("Iron Sword", AbilityType.Cleave, BuildSwordAttack(),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 2),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.MaxHp, 3)),

        Archetype.Archer => Create("Longbow", AbilityType.Snipe, BuildBowStrike(),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackReach, 0.50f),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1)),

        Archetype.Mage => Create("Oak Staff", AbilityType.Fireball, BuildStaffStrike(),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1),
            Modifier.Fixed(ModifierOp.PercentAdd, StatTarget.AttackDamage, 5)),

        _ => Create("Fists", AbilityType.None, BuildSwordAttack())
    };

    private static Weapon Create(string name, AbilityType ability, AttackDefinition basicAttack, params Modifier[] modifiers)
    {
        var weapon = new Weapon(name, ability, basicAttack);
        foreach (var modifier in modifiers)
            weapon.AddStartingModifier(modifier);

        return weapon;
    }

    private static AttackDefinition BuildSwordAttack()
    {
        return AttackDefinition.CreateMelee(
            "sword_basic",
            AttackVolumeDefinition.CreateBox(
                new Vector3(0.9f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.75f)),
            AttackTimeline.Create(0.08f, 0.04f, 0.20f));
    }

    private static AttackDefinition BuildBowStrike()
    {
        return AttackDefinition.CreateMelee(
            "bow_basic",
            AttackVolumeDefinition.CreateBox(
                new Vector3(0.7f, 0.9f, 1.1f),
                new Vector3(0.0f, 0.0f, 0.8f)),
            AttackTimeline.Create(0.10f, 0.04f, 0.18f));
    }

    private static AttackDefinition BuildStaffStrike()
    {
        return AttackDefinition.CreateMelee(
            "staff_basic",
            AttackVolumeDefinition.CreateBox(
                new Vector3(0.8f, 1.0f, 1.05f),
                new Vector3(0.0f, 0.0f, 0.8f)),
            AttackTimeline.Create(0.09f, 0.04f, 0.19f));
    }

    private static WeaponStatChannel[] CreateChannels()
    {
        var channels = new WeaponStatChannel[StatTargetInfo.All.Length];
        for (int i = 0; i < StatTargetInfo.All.Length; i++)
            channels[i] = new WeaponStatChannel(StatTargetInfo.All[i]);

        return channels;
    }
}
