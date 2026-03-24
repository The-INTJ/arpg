using Godot;

namespace ARPG;

public static partial class WorldMaterials
{
    private static StandardMaterial3D _groundMaterial;
    private static StandardMaterial3D _midGroundMaterial;
    private static StandardMaterial3D _highGroundMaterial;
    private static StandardMaterial3D _caveGroundMaterial;
    private static StandardMaterial3D _rampMaterial;
    private static StandardMaterial3D _rockMaterial;
    private static StandardMaterial3D _caveRockMaterial;
    private static StandardMaterial3D _caveRoofMaterial;

    public static StandardMaterial3D GetSurfaceMaterial(WorldSurfaceKind surfaceKind)
    {
        return surfaceKind switch
        {
            WorldSurfaceKind.Ground => _groundMaterial ??= CreateGroundMaterial(Palette.Floor, 0.028f, 0.11f),
            WorldSurfaceKind.Mid => _midGroundMaterial ??= CreateGroundMaterial(Palette.FloorMid, 0.032f, 0.12f),
            WorldSurfaceKind.High => _highGroundMaterial ??= CreateGroundMaterial(Palette.FloorHigh, 0.036f, 0.13f),
            WorldSurfaceKind.Cave => _caveGroundMaterial ??= CreateGroundMaterial(Palette.CaveFloor, 0.05f, 0.18f),
            _ => _rampMaterial ??= CreateGroundMaterial(Palette.Ramp, 0.035f, 0.12f),
        };
    }

    public static StandardMaterial3D GetRockMaterial()
    {
        return _rockMaterial ??= CreateStoneMaterial(Palette.ChunkEdge);
    }

    public static StandardMaterial3D GetCaveRockMaterial()
    {
        return _caveRockMaterial ??= CreateStoneMaterial(Palette.CaveWall);
    }

    public static StandardMaterial3D GetCaveRoofMaterial()
    {
        return _caveRoofMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.CaveShadow,
            Roughness = 0.98f,
        };
    }

    public static StandardMaterial3D CreatePrimaryGroundMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Palette.Floor;
        mat.Roughness = 0.95f;
        mat.Uv1Triplanar = true;
        mat.Uv1Scale = new Vector3(0.1f, 0.1f, 0.1f);

        var custom = TextureLoader.TryLoad("res://assets/textures/chunk_top.png");
        if (custom != null)
        {
            mat.AlbedoTexture = custom;
            return mat;
        }

        return GetSurfaceMaterial(WorldSurfaceKind.Ground);
    }

    private static StandardMaterial3D CreateStoneMaterial(Color baseColor)
    {
        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = 0.08f;
        noise.FractalOctaves = 3;

        var noiseTex = new NoiseTexture2D();
        noiseTex.Noise = noise;
        noiseTex.Width = 64;
        noiseTex.Height = 64;
        noiseTex.ColorRamp = CreateStoneGradient(baseColor);

        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = noiseTex;
        mat.AlbedoColor = baseColor;
        mat.Roughness = 0.88f;
        mat.Uv1Triplanar = true;
        mat.Uv1TriplanarSharpness = 1.0f;
        mat.Uv1Scale = new Vector3(0.45f, 0.45f, 0.45f);
        return mat;
    }

    private static Gradient CreateStoneGradient(Color baseColor)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, baseColor.Darkened(0.3f));
        gradient.SetColor(1, baseColor.Lightened(0.15f));
        return gradient;
    }

    private static StandardMaterial3D CreateGroundMaterial(Color baseColor, float noiseFrequency, float uvScale)
    {
        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = noiseFrequency;
        noise.FractalOctaves = 4;

        var noiseTex = new NoiseTexture2D();
        noiseTex.Noise = noise;
        noiseTex.Width = 128;
        noiseTex.Height = 128;

        var gradient = new Gradient();
        gradient.SetColor(0, baseColor.Darkened(0.2f));
        gradient.SetColor(1, baseColor.Lightened(0.1f));
        noiseTex.ColorRamp = gradient;

        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = noiseTex;
        mat.AlbedoColor = baseColor;
        mat.Roughness = 0.96f;
        mat.Uv1Triplanar = true;
        mat.Uv1Scale = new Vector3(uvScale, uvScale, uvScale);
        return mat;
    }
}
