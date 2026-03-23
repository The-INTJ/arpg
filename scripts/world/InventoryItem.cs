using Godot;

namespace ARPG;

public enum ItemKind
{
    HealingTonic,
    EmberBomb
}

public partial class InventoryItem
{
    public ItemKind Kind { get; }
    public int Power { get; }

    public InventoryItem(ItemKind kind, int power)
    {
        Kind = kind;
        Power = power;
    }

    public string Name => Kind switch
    {
        ItemKind.HealingTonic => "Healing Tonic",
        ItemKind.EmberBomb => "Ember Bomb",
        _ => "Unknown Item"
    };

    public string Description => Kind switch
    {
        ItemKind.HealingTonic => $"Restore {Power} HP",
        ItemKind.EmberBomb => $"Deal {Power} damage",
        _ => string.Empty
    };

    public Color DisplayColor => Kind switch
    {
        ItemKind.HealingTonic => Palette.PlayerBody,
        ItemKind.EmberBomb => Palette.EnemyGlow,
        _ => Palette.Accent
    };

    public static InventoryItem CreateForRoom(int room)
    {
        if (room <= 1 || GD.Randf() < 0.65f)
            return new InventoryItem(ItemKind.HealingTonic, 6 + room * 2);

        return new InventoryItem(ItemKind.EmberBomb, 4 + room * 2);
    }
}
