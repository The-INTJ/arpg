using Godot;

namespace ARPG;

public static class GameKeys
{
    public const string Attack = "attack";
    public const string Ability = "ability";
    public const string Jump = "jump";
    public const string Pause = "pause";
    public static readonly string[] ItemSlots =
    {
        "item_slot_1",
        "item_slot_2",
        "item_slot_3",
        "item_slot_4",
        "item_slot_5",
        "item_slot_6",
        "item_slot_7",
    };

    public static string ItemSlot(int slotIndex) => ItemSlots[slotIndex];

    public static string DisplayName(string action)
    {
        foreach (var evt in InputMap.ActionGetEvents(action))
        {
            if (evt is InputEventKey key)
            {
                var k = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
                return k.ToString();
            }
        }

        return "?";
    }
}
