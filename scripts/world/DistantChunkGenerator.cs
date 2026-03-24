using Godot;

namespace ARPG;

/// <summary>
/// Generates simplified floating chunk meshes in the background
/// to sell the splintered-planet feel.
/// </summary>
public static class DistantChunkGenerator
{
	private static readonly Vector3[] ChunkPositions =
	{
		new(-120, -15, -80),
		new(100, -25, -120),
		new(60, -10, 100),
	};

	private static readonly float[] ChunkSizes = { 30f, 22f, 35f };

	public static Node3D Generate(int currentRoom)
	{
		var container = new Node3D();
		container.Name = "DistantChunks";

		for (int i = 0; i < ChunkPositions.Length; i++)
		{
			var chunk = BuildDistantChunk(ChunkSizes[i]);
			chunk.Position = ChunkPositions[i];
			container.AddChild(chunk);
		}

		return container;
	}

	private static Node3D BuildDistantChunk(float size)
	{
		var root = new Node3D();
		root.Name = "DistantChunk";

		float thickness = size * 0.4f;
		float halfSize = size / 2f;

		// Top surface
		var topMesh = new MeshInstance3D();
		topMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		var plane = new PlaneMesh { Size = new Vector2(size, size) };
		plane.Material = CreateDistantSurfaceMaterial();
		topMesh.Mesh = plane;
		root.AddChild(topMesh);

		// Side faces (simplified — just 4 box panels)
		float midY = -thickness / 2f;
		AddDistantSide(root, new Vector3(0, midY, -halfSize), new Vector3(size, thickness, 0.3f));
		AddDistantSide(root, new Vector3(0, midY, halfSize), new Vector3(size, thickness, 0.3f));
		AddDistantSide(root, new Vector3(-halfSize, midY, 0), new Vector3(0.3f, thickness, size));
		AddDistantSide(root, new Vector3(halfSize, midY, 0), new Vector3(0.3f, thickness, size));

		// A few tiny trees for scale
		int treeCount = (int)(GD.Randi() % 3) + 2;
		for (int i = 0; i < treeCount; i++)
		{
			float tx = (float)GD.RandRange(-halfSize * 0.6, halfSize * 0.6);
			float tz = (float)GD.RandRange(-halfSize * 0.6, halfSize * 0.6);
			AddTinyTree(root, new Vector3(tx, 0, tz));
		}

		return root;
	}

	private static void AddDistantSide(Node3D parent, Vector3 position, Vector3 boxSize)
	{
		var mesh = new MeshInstance3D();
		mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		mesh.Position = position;

		var box = new BoxMesh { Size = boxSize };
		box.Material = CreateDistantCliffMaterial();
		mesh.Mesh = box;
		parent.AddChild(mesh);
	}

	private static void AddTinyTree(Node3D parent, Vector3 position)
	{
		var tree = new Node3D();
		tree.Position = position;

		float height = (float)GD.RandRange(1.0, 2.5);

		// Trunk
		var trunk = new MeshInstance3D();
		trunk.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		var cyl = new CylinderMesh
		{
			TopRadius = 0.08f,
			BottomRadius = 0.12f,
			Height = height,
		};
		cyl.Material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.35f, 0.24f, 0.12f),
			Roughness = 0.95f,
		};
		trunk.Mesh = cyl;
		trunk.Position = new Vector3(0, height / 2f, 0);
		tree.AddChild(trunk);

		// Canopy cone
		var canopy = new MeshInstance3D();
		canopy.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		float coneH = (float)GD.RandRange(1.0, 2.0);
		var cone = new CylinderMesh
		{
			TopRadius = 0f,
			BottomRadius = (float)GD.RandRange(0.5, 0.9),
			Height = coneH,
		};
		float green = (float)GD.RandRange(0.15, 0.35);
		cone.Material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.1f, green, 0.08f),
			Roughness = 0.9f,
		};
		canopy.Mesh = cone;
		canopy.Position = new Vector3(0, height + coneH / 2f - 0.2f, 0);
		tree.AddChild(canopy);

		parent.AddChild(tree);
	}

	private static StandardMaterial3D CreateDistantSurfaceMaterial()
	{
		return new StandardMaterial3D
		{
			AlbedoColor = Palette.Floor.Darkened(0.25f),
			Roughness = 0.95f,
		};
	}

	private static StandardMaterial3D CreateDistantCliffMaterial()
	{
		return new StandardMaterial3D
		{
			AlbedoColor = Palette.ChunkEdge.Darkened(0.2f),
			Roughness = 0.95f,
		};
	}
}
