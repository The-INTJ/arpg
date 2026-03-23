using Godot;

namespace ARPG;

/// <summary>
/// Generates simple pixel-art character textures at runtime.
/// Each texture is a small grid of colored pixels, scaled up with nearest-neighbor filtering.
/// </summary>
public static class SpriteFactory
{
    // Pixel maps: '.' = transparent, letters = color keys
    // Player: simple adventurer silhouette
    private static readonly string[] PlayerPixels =
    {
        "....HH....",
        "...HHHH...",
        "...HFFH...",
        "...HHHH...",
        "....HH....",
        "...BBBB...",
        "..BBBBBB..",
        "..BABBAB..",
        "..BBBBBB..",
        "..BBBBBB..",
        "...BB.BB..",
        "...LL.LL..",
        "...LL.LL..",
        "..SSS.SSS.",
    };

    // Enemy: squat goblin/imp
    private static readonly string[] EnemyPixels =
    {
        "..EE..EE..",
        "..EEEEEE..",
        ".EEEEEEEE.",
        ".EEMEEEME.",
        ".EEEEEEEE.",
        ".EE.EE.EE.",
        "..EEEEEE..",
        "..EEEEEE..",
        ".EEEEEEE..",
        ".EEEEEEEE.",
        "..EE..EE..",
        "..EE..EE..",
        ".DDD..DDD.",
    };

    private static Color GetPlayerColor(char c) => c switch
    {
        'H' => new Color(0.55f, 0.40f, 0.25f), // hair/skin
        'F' => new Color(0.85f, 0.70f, 0.55f), // face
        'B' => Palette.PlayerBody,               // body/armor
        'A' => Palette.Accent,                    // accent/belt
        'L' => new Color(0.25f, 0.35f, 0.25f),  // legs
        'S' => new Color(0.35f, 0.22f, 0.12f),  // shoes
        _ => Colors.Transparent,
    };

    private static Color GetEnemyColor(char c) => c switch
    {
        'E' => Palette.EnemyBody,                 // body
        'M' => new Color(0.95f, 0.85f, 0.2f),   // eyes
        'D' => new Color(0.40f, 0.20f, 0.10f),  // feet
        _ => Colors.Transparent,
    };

    // Boss: bigger, scarier version
    private static readonly string[] BossPixels =
    {
        "..EEE..EEE..",
        "..EEEEEEEE..",
        ".EEEEEEEEEE.",
        ".EEEEMMEEE..",
        ".EEEEEEEEEE.",
        ".EEE.EE.EEE.",
        "..EEEEEEEE..",
        ".EEEEEEEEEE.",
        ".EEEEEEEEEE.",
        ".EEEEEEEEEE.",
        "..EEEEEEEE..",
        "..EEE..EEE..",
        "..EEE..EEE..",
        ".DDDD.DDDD..",
    };

    public static ImageTexture CreatePlayerTexture() => BuildTexture(PlayerPixels, GetPlayerColor);
    public static ImageTexture CreateEnemyTexture() => BuildTexture(EnemyPixels, GetEnemyColor);
    public static ImageTexture CreateBossTexture() => BuildTexture(BossPixels, GetEnemyColor);

    private static ImageTexture BuildTexture(string[] pixels, System.Func<char, Color> colorMap)
    {
        int height = pixels.Length;
        int width = 0;
        foreach (var row in pixels)
            if (row.Length > width) width = row.Length;

        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < pixels[y].Length; x++)
            {
                image.SetPixel(x, y, colorMap(pixels[y][x]));
            }
        }

        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }

    /// <summary>
    /// Creates a Sprite3D billboard node with the given texture.
    /// </summary>
    public static Sprite3D CreateSprite(ImageTexture texture, float pixelSize = 0.1f)
    {
        var sprite = new Sprite3D();
        sprite.Texture = texture;
        sprite.PixelSize = pixelSize;
        sprite.Billboard = BaseMaterial3D.BillboardModeEnum.FixedY;
        sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
        sprite.Shaded = false;
        return sprite;
    }
}
