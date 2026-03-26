namespace ARPG;

public readonly partial record struct EnemySpawnPlan(bool IsBoss, bool IsElite, bool IsRanged, InventoryItem ItemDrop, int? ForcedVariant = null)
{
    public static EnemySpawnPlan Normal(int? forcedVariant = null) => new(false, false, false, null, forcedVariant);
    public static EnemySpawnPlan Ranged(int? forcedVariant = null) => new(false, false, true, null, forcedVariant);
    public static EnemySpawnPlan Elite(InventoryItem itemDrop, int? forcedVariant = null) => new(false, true, false, itemDrop, forcedVariant);
    public static EnemySpawnPlan Boss(InventoryItem itemDrop) => new(true, false, false, itemDrop);
}
