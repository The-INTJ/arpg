namespace ARPG;

public readonly partial record struct EnemySpawnPlan(bool IsBoss, bool IsElite, InventoryItem ItemDrop)
{
    public static EnemySpawnPlan Normal() => new(false, false, null);
    public static EnemySpawnPlan Elite(InventoryItem itemDrop) => new(false, true, itemDrop);
    public static EnemySpawnPlan Boss(InventoryItem itemDrop) => new(true, false, itemDrop);
}
