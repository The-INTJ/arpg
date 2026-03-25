namespace ARPG;

public readonly partial record struct EnemySpawnPlan(bool IsBoss, bool IsElite, InventoryItem ItemDrop, int? ForcedVariant = null)
{
    public static EnemySpawnPlan Normal(int? forcedVariant = null) => new(false, false, null, forcedVariant);
    public static EnemySpawnPlan Elite(InventoryItem itemDrop, int? forcedVariant = null) => new(false, true, itemDrop, forcedVariant);
    public static EnemySpawnPlan Boss(InventoryItem itemDrop) => new(true, false, itemDrop);
}
