using System;
using Godot;

namespace ARPG;

public enum ItemKind
{
    HealingTonic,
    DeeprootFlask,
    EmberBomb,
    StarfireBomb,
    SannosShield,
    MarauderDraught,
    GiantSeal
}

public partial class InventoryItem
{
    public ItemKind Kind { get; }
    public int HealAmount { get; }
    public int DirectDamage { get; }
    public int NextAttackBonusDamage { get; }
    public float NextAttackMultiplier { get; }
    public int NegateNextHits { get; }
    public bool RequiresCombatTarget => DirectDamage > 0;

    public InventoryItem(
        ItemKind kind,
        int healAmount = 0,
        int directDamage = 0,
        int nextAttackBonusDamage = 0,
        float nextAttackMultiplier = 1.0f,
        int negateNextHits = 0)
    {
        Kind = kind;
        HealAmount = Math.Max(0, healAmount);
        DirectDamage = Math.Max(0, directDamage);
        NextAttackBonusDamage = Math.Max(0, nextAttackBonusDamage);
        NextAttackMultiplier = Math.Max(1.0f, nextAttackMultiplier);
        NegateNextHits = Math.Max(0, negateNextHits);
    }

    public string Name => Kind switch
    {
        ItemKind.HealingTonic => "Healing Tonic",
        ItemKind.DeeprootFlask => "Deeproot Flask",
        ItemKind.EmberBomb => "Ember Bomb",
        ItemKind.StarfireBomb => "Starfire Bomb",
        ItemKind.SannosShield => "Sannos Shield",
        ItemKind.MarauderDraught => "Marauder Draught",
        ItemKind.GiantSeal => "Giant's Seal",
        _ => "Unknown Item"
    };

    public string Description => Kind switch
    {
        ItemKind.HealingTonic => $"Restore {HealAmount} HP",
        ItemKind.DeeprootFlask => $"Restore {HealAmount} HP",
        ItemKind.EmberBomb => $"Deal {DirectDamage} damage",
        ItemKind.StarfireBomb => $"Deal {DirectDamage} damage",
        ItemKind.SannosShield => "Next hit: 0 damage",
        ItemKind.MarauderDraught => $"Next attack: +{NextAttackBonusDamage} damage",
        ItemKind.GiantSeal => $"Next attack: x{NextAttackMultiplier:0.#} damage",
        _ => string.Empty
    };

    public Color DisplayColor => Kind switch
    {
        ItemKind.HealingTonic => Palette.ItemHeal,
        ItemKind.DeeprootFlask => Palette.ItemHealMajor,
        ItemKind.EmberBomb => Palette.ItemBomb,
        ItemKind.StarfireBomb => Palette.ItemBombMajor,
        ItemKind.SannosShield => Palette.ItemWard,
        ItemKind.MarauderDraught => Palette.ItemPower,
        ItemKind.GiantSeal => Palette.ItemArcane,
        _ => Palette.Accent
    };

    public static InventoryItem CreateForRoom(int room)
    {
        return CreateRandom(room, ItemDropSource.RoomPickup);
    }

    public static InventoryItem CreateEnemyDrop(int room, bool fromBoss = false)
    {
        return CreateRandom(room, fromBoss ? ItemDropSource.BossDrop : ItemDropSource.EliteDrop);
    }

    private static InventoryItem CreateRandom(int room, ItemDropSource source)
    {
        room = Math.Clamp(room, 1, GameState.TotalRooms);
        return Create(RollKind(room, source), room);
    }

    private static InventoryItem Create(ItemKind kind, int room)
    {
        int roomScale = room - 1;

        return kind switch
        {
            ItemKind.HealingTonic => new InventoryItem(kind, healAmount: 10 + roomScale * 4),
            ItemKind.DeeprootFlask => new InventoryItem(kind, healAmount: 16 + roomScale * 5),
            ItemKind.EmberBomb => new InventoryItem(kind, directDamage: 7 + roomScale * 3),
            ItemKind.StarfireBomb => new InventoryItem(kind, directDamage: 12 + roomScale * 4),
            ItemKind.SannosShield => new InventoryItem(kind, negateNextHits: 1),
            ItemKind.MarauderDraught => new InventoryItem(kind, nextAttackBonusDamage: 5 + roomScale * 2),
            ItemKind.GiantSeal => new InventoryItem(kind, nextAttackMultiplier: 1.8f + roomScale * 0.1f),
            _ => new InventoryItem(ItemKind.HealingTonic, healAmount: 10 + roomScale * 4),
        };
    }

    private static ItemKind RollKind(int room, ItemDropSource source)
    {
        var options = GetLootTable(room, source);
        float totalWeight = 0.0f;
        for (int i = 0; i < options.Length; i++)
            totalWeight += options[i].Weight;

        float roll = GD.Randf() * totalWeight;
        for (int i = 0; i < options.Length; i++)
        {
            roll -= options[i].Weight;
            if (roll <= 0.0f)
                return options[i].Kind;
        }

        return options[^1].Kind;
    }

    private static ItemDropOption[] GetLootTable(int room, ItemDropSource source)
    {
        if (source == ItemDropSource.BossDrop)
        {
            return new[]
            {
                new ItemDropOption(ItemKind.GiantSeal, 0.35f),
                new ItemDropOption(ItemKind.StarfireBomb, 0.30f),
                new ItemDropOption(ItemKind.DeeprootFlask, 0.20f),
                new ItemDropOption(ItemKind.SannosShield, 0.15f),
            };
        }

        if (source == ItemDropSource.EliteDrop)
        {
            return room switch
            {
                1 => new[]
                {
                    new ItemDropOption(ItemKind.SannosShield, 0.24f),
                    new ItemDropOption(ItemKind.MarauderDraught, 0.24f),
                    new ItemDropOption(ItemKind.EmberBomb, 0.20f),
                    new ItemDropOption(ItemKind.DeeprootFlask, 0.16f),
                    new ItemDropOption(ItemKind.GiantSeal, 0.10f),
                    new ItemDropOption(ItemKind.StarfireBomb, 0.06f),
                },
                2 => new[]
                {
                    new ItemDropOption(ItemKind.SannosShield, 0.18f),
                    new ItemDropOption(ItemKind.MarauderDraught, 0.18f),
                    new ItemDropOption(ItemKind.DeeprootFlask, 0.16f),
                    new ItemDropOption(ItemKind.StarfireBomb, 0.16f),
                    new ItemDropOption(ItemKind.GiantSeal, 0.16f),
                    new ItemDropOption(ItemKind.EmberBomb, 0.16f),
                },
                _ => new[]
                {
                    new ItemDropOption(ItemKind.DeeprootFlask, 0.20f),
                    new ItemDropOption(ItemKind.StarfireBomb, 0.22f),
                    new ItemDropOption(ItemKind.GiantSeal, 0.22f),
                    new ItemDropOption(ItemKind.SannosShield, 0.16f),
                    new ItemDropOption(ItemKind.MarauderDraught, 0.12f),
                    new ItemDropOption(ItemKind.EmberBomb, 0.08f),
                },
            };
        }

        return room switch
        {
            1 => new[]
            {
                new ItemDropOption(ItemKind.HealingTonic, 0.24f),
                new ItemDropOption(ItemKind.EmberBomb, 0.20f),
                new ItemDropOption(ItemKind.SannosShield, 0.18f),
                new ItemDropOption(ItemKind.MarauderDraught, 0.18f),
                new ItemDropOption(ItemKind.DeeprootFlask, 0.12f),
                new ItemDropOption(ItemKind.StarfireBomb, 0.05f),
                new ItemDropOption(ItemKind.GiantSeal, 0.03f),
            },
            2 => new[]
            {
                new ItemDropOption(ItemKind.HealingTonic, 0.16f),
                new ItemDropOption(ItemKind.EmberBomb, 0.16f),
                new ItemDropOption(ItemKind.SannosShield, 0.15f),
                new ItemDropOption(ItemKind.MarauderDraught, 0.15f),
                new ItemDropOption(ItemKind.DeeprootFlask, 0.15f),
                new ItemDropOption(ItemKind.StarfireBomb, 0.12f),
                new ItemDropOption(ItemKind.GiantSeal, 0.11f),
            },
            _ => new[]
            {
                new ItemDropOption(ItemKind.HealingTonic, 0.10f),
                new ItemDropOption(ItemKind.EmberBomb, 0.12f),
                new ItemDropOption(ItemKind.SannosShield, 0.14f),
                new ItemDropOption(ItemKind.MarauderDraught, 0.14f),
                new ItemDropOption(ItemKind.DeeprootFlask, 0.16f),
                new ItemDropOption(ItemKind.StarfireBomb, 0.17f),
                new ItemDropOption(ItemKind.GiantSeal, 0.17f),
            },
        };
    }

    private enum ItemDropSource
    {
        RoomPickup,
        EliteDrop,
        BossDrop
    }

    private readonly record struct ItemDropOption(ItemKind Kind, float Weight);
}
