namespace ARPG;

public readonly record struct PlayerBaseStats(
    int MaxHp,
    int AttackDamage,
    float MoveSpeed,
    float AttackReach,
    float AttackSize,
    float JumpHeight,
    int JumpCount,
    int InventorySlots = 2,
    int ItemUsesPerTurn = 1)
{
    public static PlayerBaseStats Default => new(
        MaxHp: 30,
        AttackDamage: 4,
        MoveSpeed: 5.5f,
        AttackReach: 1.25f,
        AttackSize: 1.0f,
        JumpHeight: 1.9f,
        JumpCount: 1,
        InventorySlots: 2,
        ItemUsesPerTurn: 1);
}
