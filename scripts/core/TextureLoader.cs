using Godot;

namespace ARPG;

/// <summary>
/// Attempts to load a texture from a given path. Returns null if the file
/// doesn't exist or is a tiny placeholder (1x1). This lets art be swapped
/// in by simply replacing placeholder PNGs — no code changes needed.
/// </summary>
public static class TextureLoader
{
	/// <summary>
	/// Try loading a texture. Returns null if the resource doesn't exist
	/// or is a 1x1 placeholder (meaning it hasn't been replaced with real art yet).
	/// </summary>
	public static Texture2D TryLoad(string resPath)
	{
		if (!ResourceLoader.Exists(resPath))
			return null;

		var tex = GD.Load<Texture2D>(resPath);
		if (tex == null)
			return null;

		// Treat 1x1 textures as placeholders — fall back to procedural
		if (tex.GetWidth() <= 1 && tex.GetHeight() <= 1)
			return null;

		return tex;
	}
}
