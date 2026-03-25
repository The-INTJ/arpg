namespace ARPG;

public static partial class DeveloperEffectGroups
{
    public const string Movement = "movement";
    public const string Encounter = "encounter";
    public const string Boundary = "boundary";
    public const string Progression = "progression";

    public static string DisplayName(string groupId)
    {
        return groupId switch
        {
            Movement => "Movement",
            Encounter => "Encounter",
            Boundary => "Boundary",
            Progression => "Progression",
            _ => groupId,
        };
    }

    public static int SortOrder(string groupId)
    {
        return groupId switch
        {
            Movement => 0,
            Encounter => 1,
            Boundary => 2,
            Progression => 3,
            _ => 100,
        };
    }
}
