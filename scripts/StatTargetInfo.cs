namespace ARPG;

public static partial class StatTargetInfo
{
    public static readonly StatTarget[] All =
    {
        StatTarget.MaxHp,
        StatTarget.AttackDamage,
        StatTarget.MoveSpeed,
        StatTarget.AttackRange
    };

    public static string DisplayName(StatTarget target) => target switch
    {
        StatTarget.MaxHp => "HP",
        StatTarget.AttackDamage => "ATK",
        StatTarget.MoveSpeed => "SPD",
        StatTarget.AttackRange => "Range",
        _ => target.ToString()
    };
}
