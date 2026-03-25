using Godot;

namespace ARPG;

/// <summary>
/// Builds the open-void chunk silhouette.
/// The playable chunk keeps a bottom cap and edge trim, but no vertical side walls.
/// </summary>
public static class ChunkBuilder
{
	private const float DefaultThickness = 15f;
	private static PackedScene _islandMainSliceScene;

	/// <summary>
	/// Adds floating island geometry to the given parent node.
	/// Width/depth match the playable floor area. Thickness is how deep the chunk hangs below Y=0.
	/// </summary>
	public static void BuildChunk(Node3D parent, float width, float depth, float thickness = DefaultThickness)
	{
		if (TryPlaceIslandSlice(parent, width, depth, thickness))
			return;

		float halfW = width / 2f;
		float halfD = depth / 2f;

		// Bottom cap
		AddBottomCap(parent, width * 0.9f, depth * 0.9f, -thickness);

		// Edge trim — thin raised lip along the top perimeter
		float trimH = 0.15f;
		float trimW = 0.3f;
		AddTrimStrip(parent, new Vector3(0, trimH / 2f, -halfD), new Vector3(width + trimW, trimH, trimW));
		AddTrimStrip(parent, new Vector3(0, trimH / 2f, halfD), new Vector3(width + trimW, trimH, trimW));
		AddTrimStrip(parent, new Vector3(-halfW, trimH / 2f, 0), new Vector3(trimW, trimH, depth + trimW));
		AddTrimStrip(parent, new Vector3(halfW, trimH / 2f, 0), new Vector3(trimW, trimH, depth + trimW));
	}

	private static bool TryPlaceIslandSlice(Node3D parent, float width, float depth, float thickness)
	{
		var scene = LoadIslandMainSlice();
		if (scene == null)
			return false;

		var slice = scene.Instantiate<IslandMainSlice>();
		slice.Name = "IslandMainSlice";
		parent.AddChild(slice);
		return true;
	}

	private static PackedScene LoadIslandMainSlice()
	{
		return _islandMainSliceScene ??= GD.Load<PackedScene>(Scenes.IslandMainSlice);
	}

	private static void AddBottomCap(Node3D parent, float width, float depth, float yPos)
	{
		var mesh = new MeshInstance3D();
		mesh.Name = "ChunkBottom";
		mesh.Position = new Vector3(0, yPos, 0);
		mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		var plane = new PlaneMesh { Size = new Vector2(width, depth) };
		var mat = new StandardMaterial3D
		{
			AlbedoColor = Palette.AbyssVoid.Lightened(0.05f),
			Roughness = 1.0f,
		};
		plane.Material = mat;
		// Flip the plane so it faces downward
		mesh.RotationDegrees = new Vector3(180, 0, 0);
		mesh.Mesh = plane;

		parent.AddChild(mesh);
	}

	private static void AddTrimStrip(Node3D parent, Vector3 position, Vector3 size)
	{
		var mesh = new MeshInstance3D();
		mesh.Name = "ChunkEdgeTrim";
		mesh.Position = position;
		mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		var box = new BoxMesh { Size = size };
		var mat = new StandardMaterial3D
		{
			AlbedoColor = Palette.ChunkEdge.Lightened(0.1f),
			Roughness = 0.7f,
		};
		box.Material = mat;
		mesh.Mesh = box;

		parent.AddChild(mesh);
	}

}
