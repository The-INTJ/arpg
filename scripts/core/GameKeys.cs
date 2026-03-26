using Godot;

namespace ARPG;

public static class GameKeys
{
	public const string MoveForward = "move_forward";
	public const string MoveBack = "move_back";
	public const string MoveLeft = "move_left";
	public const string MoveRight = "move_right";
	public const string Attack = "attack";
	public const string Ability = "ability";
	public const string Sprint = "sprint";
	public const string Jump = "jump";
	public const string Pause = "pause";
	public const string BuildMode = "build_mode";
	public const string RotateBuild = "rotate_build";
	public const string DevAscend = "dev_ascend";
	public const string DevDescend = "dev_descend";
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
