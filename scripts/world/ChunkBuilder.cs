using Godot;

namespace ARPG;

/// <summary>
/// Builds the floating island geometry — side cliffs hanging below the floor,
/// a bottom cap, and edge trim to sell the "chunk hovering over void" look.
/// </summary>
public static class ChunkBuilder
{
	private const float DefaultThickness = 15f;

	/// <summary>
	/// Adds floating island geometry to the given parent node.
	/// Width/depth match the playable floor area. Thickness is how deep the chunk hangs below Y=0.
	/// </summary>
	public static void BuildChunk(Node3D parent, float width, float depth, float thickness = DefaultThickness)
	{
		float halfW = width / 2f;
		float halfD = depth / 2f;
		float midY = -thickness / 2f;

		// North face
		AddSideFace(parent, new Vector3(0, midY, -halfD), new Vector3(width, thickness, 0.5f));
		// South face
		AddSideFace(parent, new Vector3(0, midY, halfD), new Vector3(width, thickness, 0.5f));
		// East face
		AddSideFace(parent, new Vector3(halfW, midY, 0), new Vector3(0.5f, thickness, depth));
		// West face
		AddSideFace(parent, new Vector3(-halfW, midY, 0), new Vector3(0.5f, thickness, depth));

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

	private static void AddSideFace(Node3D parent, Vector3 position, Vector3 size)
	{
		var mesh = new MeshInstance3D();
		mesh.Name = "ChunkSide";
		mesh.Position = position;
		mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		var box = new BoxMesh { Size = size };
		box.Material = CreateCliffMaterial();
		mesh.Mesh = box;

		parent.AddChild(mesh);
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

	/// <summary>
	/// Procedural cliff rock material with triplanar noise texture.
	/// Falls back to procedural noise if no custom texture exists at res://assets/textures/chunk_cliff.png.
	/// </summary>
	public static StandardMaterial3D CreateCliffMaterial()
	{
		var mat = new StandardMaterial3D();
		mat.AlbedoColor = Palette.ChunkEdge;
		mat.Roughness = 0.9f;
		mat.Uv1Triplanar = true;
		mat.Uv1TriplanarSharpness = 1.0f;
		mat.Uv1Scale = new Vector3(0.4f, 0.4f, 0.4f);

		var custom = TextureLoader.TryLoad("res://assets/textures/chunk_cliff.png");
		if (custom != null)
		{
			mat.AlbedoTexture = custom;
			return mat;
		}

		var noise = new FastNoiseLite();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
		noise.Frequency = 0.06f;
		noise.FractalOctaves = 4;

		var noiseTex = new NoiseTexture2D();
		noiseTex.Noise = noise;
		noiseTex.Width = 64;
		noiseTex.Height = 64;

		var gradient = new Gradient();
		gradient.SetColor(0, Palette.ChunkEdge.Darkened(0.35f));
		gradient.SetColor(1, Palette.ChunkEdge.Lightened(0.1f));
		noiseTex.ColorRamp = gradient;

		mat.AlbedoTexture = noiseTex;
		return mat;
	}
}
