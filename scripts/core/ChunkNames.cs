using System.Collections.Generic;
using Godot;

namespace ARPG;

/// <summary>
/// Pool of lore-flavored names for floating chunks.
/// Edit the Names array to change available chunk names.
/// </summary>
public static class ChunkNames
{
	private static readonly string[] Names =
	{
		"The Fractured Shelf",
		"Obsidian Shard",
		"Sunken Terrace",
		"The Hollow Reach",
		"Ashen Plateau",
		"Splintered Rise",
		"The Shattered Veil",
		"Ironstone Fragment",
		"Duskfall Ridge",
		"The Severed Crown",
		"Blightrock Spur",
		"Windscoured Ledge",
		"The Jagged Remnant",
		"Embercrust Shelf",
		"The Forsaken Plate",
	};

	private static readonly List<int> _usedIndices = new();

	/// <summary>
	/// Pick a random chunk name that hasn't been used this run.
	/// Call ResetUsed() at the start of each new run.
	/// </summary>
	public static string RandomName()
	{
		if (_usedIndices.Count >= Names.Length)
			_usedIndices.Clear();

		int idx;
		do
		{
			idx = (int)(GD.Randi() % Names.Length);
		} while (_usedIndices.Contains(idx));

		_usedIndices.Add(idx);
		return Names[idx];
	}

	public static void ResetUsed()
	{
		_usedIndices.Clear();
	}
}
