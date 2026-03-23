namespace ARPG;

/// <summary>
/// A weapon with a name, an ability, and exactly two modifier slots.
/// The weapon's slot modifiers participate in stat computation alongside other modifiers.
/// </summary>
public class Weapon
{
    public string Name { get; }
    public AbilityType Ability { get; }
    public Modifier[] Slots { get; } = new Modifier[2];

    public Weapon(string name, AbilityType ability)
    {
        Name = name;
        Ability = ability;
    }

    public static Weapon ForArchetype(Archetype archetype) => archetype switch
    {
        Archetype.Fighter => Create("Iron Sword", AbilityType.Cleave,
            new Modifier(ModifierOp.FlatAdd, StatTarget.AttackDamage, 2),
            new Modifier(ModifierOp.FlatAdd, StatTarget.MaxHp, 3)),

        Archetype.Archer => Create("Longbow", AbilityType.Snipe,
            new Modifier(ModifierOp.FlatAdd, StatTarget.AttackRange, 0.5f),
            new Modifier(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1)),

        Archetype.Mage => Create("Oak Staff", AbilityType.Fireball,
            new Modifier(ModifierOp.FlatAdd, StatTarget.AttackDamage, 1),
            new Modifier(ModifierOp.PercentAdd, StatTarget.AttackDamage, 5)),

        _ => Create("Fists", AbilityType.None, null, null)
    };

    private static Weapon Create(string name, AbilityType ability, Modifier slot0, Modifier slot1)
    {
        var w = new Weapon(name, ability);
        w.Slots[0] = slot0;
        w.Slots[1] = slot1;
        return w;
    }
}
