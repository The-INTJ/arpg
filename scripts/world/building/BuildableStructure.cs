using Godot;

namespace ARPG;

public enum StructureKind
{
	Platform,
	Ramp,
}

/// <summary>
/// Template describing a player-buildable structure: dimensions, cost, and type.
/// </summary>
public class BuildableStructure
{
	public StructureKind Kind { get; }
	public string DisplayName { get; }
	public Vector3 Size { get; }
	public float HeightGain { get; }
	public int EnergyCost { get; }

	private BuildableStructure(StructureKind kind, string displayName, Vector3 size, float heightGain, int energyCost)
	{
		Kind = kind;
		DisplayName = displayName;
		Size = size;
		HeightGain = heightGain;
		EnergyCost = energyCost;
	}

	public static BuildableStructure SmallPlatform() =>
		new(StructureKind.Platform, "Small Platform", new Vector3(4f, 0.4f, 4f), 0f, 2);

	public static BuildableStructure ShortRamp() =>
		new(StructureKind.Ramp, "Short Ramp", new Vector3(4f, 0.6f, 6f), 1.15f, 3);

	public static BuildableStructure[] AllTemplates() => new[]
	{
		SmallPlatform(),
		ShortRamp(),
	};
}
