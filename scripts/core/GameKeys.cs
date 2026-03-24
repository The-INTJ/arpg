using Godot;

namespace ARPG;

public static class GameKeys
{
	public const string Attack = "attack";
	public const string Ability = "ability";
	public const string Jump = "jump";
	public const string Pause = "pause";
	private static readonly string[] KeyboardItemSlotActions =
	{
		"item_slot_1",
		"item_slot_2",
		"item_slot_3",
		"item_slot_4",
	};

	public static int KeyboardItemSlotCount => KeyboardItemSlotActions.Length;

	public static bool HasKeyboardItemSlot(int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < KeyboardItemSlotActions.Length;
	}

	public static string KeyboardItemSlotAction(int slotIndex)
	{
		return KeyboardItemSlotActions[slotIndex];
	}

	public static string ItemSlotLabel(int slotIndex)
	{
		if (HasKeyboardItemSlot(slotIndex))
			return DisplayName(KeyboardItemSlotAction(slotIndex));

		return $"Slot {slotIndex + 1}";
	}

	public static string ItemSlotUseHint(int slotIndex)
	{
		if (HasKeyboardItemSlot(slotIndex))
			return DisplayName(KeyboardItemSlotAction(slotIndex));

		return $"slot {slotIndex + 1} (click)";
	}

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
