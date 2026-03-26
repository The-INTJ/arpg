namespace ARPG;

public readonly record struct PlayerBaseStats(
    int MaxHp,
    int AttackDamage,
    float MoveSpeed,
    float AttackRange,
    float JumpHeight,
    int JumpCount,
    int InventorySlots = 2,
    int ItemUsesPerTurn = 1)
{
    public static PlayerBaseStats Default => new(
        MaxHp: 30,
        AttackDamage: 4,
        MoveSpeed: 5.5f,
        AttackRange: 1.25f,
        JumpHeight: 1.9f,
        JumpCount: 1,
        InventorySlots: 2,
        ItemUsesPerTurn: 1);
}
