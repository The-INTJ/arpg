using Godot;

namespace ARPG;

public static class GameKeys
{
    public const string Attack = "attack";

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
