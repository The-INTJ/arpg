using System.Collections.Generic;

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
    public IReadOnlyList<WeaponStatChannel> Channels => _channels;

    public Weapon(string name, AbilityType ability)
    {
        Name = name;
        Ability = ability;
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
        Archetype.Fighter => Create("Iron Sword", AbilityType.Cleave,
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 2),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.MaxHp, 3)),

        Archetype.Archer => Create("Longbow", AbilityType.Snipe,
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackRange, 0.5f),
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1)),

        Archetype.Mage => Create("Oak Staff", AbilityType.Fireball,
            Modifier.Fixed(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1),
            Modifier.Fixed(ModifierOp.PercentAdd, StatTarget.AttackDamage, 5)),

        _ => Create("Fists", AbilityType.None)
    };

    private static Weapon Create(string name, AbilityType ability, params Modifier[] modifiers)
    {
        var weapon = new Weapon(name, ability);
        foreach (var modifier in modifiers)
            weapon.AddStartingModifier(modifier);

        return weapon;
    }

    private static WeaponStatChannel[] CreateChannels()
    {
        var channels = new WeaponStatChannel[StatTargetInfo.All.Length];
        for (int i = 0; i < StatTargetInfo.All.Length; i++)
            channels[i] = new WeaponStatChannel(StatTargetInfo.All[i]);

        return channels;
    }
}
