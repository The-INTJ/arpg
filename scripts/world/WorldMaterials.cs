using Godot;

namespace ARPG;

public static partial class WorldMaterials
{
    private static StandardMaterial3D _groundMaterial;
    private static StandardMaterial3D _doubleSidedGroundMaterial;
    private static StandardMaterial3D _midGroundMaterial;
    private static StandardMaterial3D _highGroundMaterial;
    private static StandardMaterial3D _caveGroundMaterial;
    private static StandardMaterial3D _rampMaterial;
    private static StandardMaterial3D _rockMaterial;
    private static StandardMaterial3D _caveRockMaterial;
    private static StandardMaterial3D _caveRoofMaterial;
    private static StandardMaterial3D _chestWoodMaterial;
    private static StandardMaterial3D _chestMetalMaterial;
    private static StandardMaterial3D _tunnelGlowOverlayMaterial;

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

    public static StandardMaterial3D GetChestWoodMaterial()
    {
        if (_chestWoodMaterial != null)
            return _chestWoodMaterial;

        _chestWoodMaterial = new StandardMaterial3D
        {
            AlbedoColor = Palette.ChestWood,
            AlbedoTexture = TextureLoader.TryLoad("res://assets/textures/props/chest_wood.png")
                ?? CreateTintedNoiseTexture(Palette.ChestWood.Darkened(0.24f), Palette.ChestWood.Lightened(0.12f), 0.075f, 128, 48),
            Roughness = 0.94f,
            Metallic = 0.02f,
            Uv1Triplanar = true,
            Uv1TriplanarSharpness = 0.85f,
            Uv1Scale = new Vector3(5.5f, 9.0f, 5.5f),
        };

        return _chestWoodMaterial;
    }

    public static StandardMaterial3D GetChestMetalMaterial()
    {
        if (_chestMetalMaterial != null)
            return _chestMetalMaterial;

        _chestMetalMaterial = new StandardMaterial3D
        {
            AlbedoColor = Palette.ChestMetal,
            AlbedoTexture = TextureLoader.TryLoad("res://assets/textures/props/chest_metal.png")
                ?? CreateTintedNoiseTexture(Palette.ChestMetal.Darkened(0.22f), Palette.TextLight.Lerp(Palette.ChestMetal, 0.40f), 0.14f, 96, 96),
            Roughness = 0.34f,
            Metallic = 0.82f,
            EmissionEnabled = true,
            Emission = Palette.ChestMetal.Darkened(0.10f),
            EmissionEnergyMultiplier = 0.15f,
            Uv1Triplanar = true,
            Uv1TriplanarSharpness = 0.9f,
            Uv1Scale = new Vector3(7.0f, 7.0f, 7.0f),
        };

        return _chestMetalMaterial;
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

    public static StandardMaterial3D GetDoubleSidedPrimaryGroundMaterial()
    {
        if (_doubleSidedGroundMaterial != null)
            return _doubleSidedGroundMaterial;

        _doubleSidedGroundMaterial = (StandardMaterial3D)CreatePrimaryGroundMaterial().Duplicate();
        _doubleSidedGroundMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        return _doubleSidedGroundMaterial;
    }

    public static StandardMaterial3D GetTunnelGlowOverlayMaterial()
    {
        if (_tunnelGlowOverlayMaterial != null)
            return _tunnelGlowOverlayMaterial;

        _tunnelGlowOverlayMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(Palette.DarkEnergyGlow, 0.16f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            Roughness = 0.2f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            EmissionEnabled = true,
            Emission = Palette.DarkEnergyGlow,
            EmissionEnergyMultiplier = 1.1f,
        };

        return _tunnelGlowOverlayMaterial;
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

    private static Texture2D CreateTintedNoiseTexture(Color darkColor, Color lightColor, float frequency, int width, int height)
    {
        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = frequency;
        noise.FractalOctaves = 4;

        var noiseTex = new NoiseTexture2D();
        noiseTex.Noise = noise;
        noiseTex.Width = width;
        noiseTex.Height = height;

        var gradient = new Gradient();
        gradient.SetColor(0, darkColor);
        gradient.SetColor(1, lightColor);
        noiseTex.ColorRamp = gradient;
        return noiseTex;
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
