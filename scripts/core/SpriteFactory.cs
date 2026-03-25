using Godot;
using System.Collections.Generic;

namespace ARPG;

/// <summary>
/// Generates simple pixel-art character textures at runtime.
/// Each texture is a small grid of colored pixels, scaled up with nearest-neighbor filtering.
/// </summary>
public static class SpriteFactory
{
    public const int GoblinVariant = 0;
    public const int SkeletonVariant = 1;
    public const int SlimeVariant = 2;
    public const int DemonVariant = 3;

    // --- Player sprites per archetype ---

    private static readonly string[] FighterPixels =
    {
        "....HH....",
        "...HHHH...",
        "...HFFH...",
        "...HHHH...",
        "....HH....",
        "..AABBAA..",
        "..BBBBBB..",
        "..BABBAB..",
        "..BBBBBB..",
        "..BBBBBB..",
        "...BB.BB..",
        "...LL.LL..",
        "...LL.LL..",
        "..SSS.SSS.",
    };

    private static readonly string[] ArcherPixels =
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

    private static readonly string[] MagePixels =
    {
        "...PPPP...",
        "..PPPPPP..",
        "...HFFH...",
        "...HHHH...",
        "....HH....",
        "..RBBBBR..",
        ".RBBBBBBR.",
        ".RBABBABR.",
        ".RBBBBBBR.",
        "..RBBBBR..",
        "..RBBRBB..",
        "...LL.LL..",
        "...LL.LL..",
        "..SSS.SSS.",
    };

    private static readonly string[] FighterWeaponPixels =
    {
        "..GG..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..MM..",
        "..WW..",
        ".WWWW.",
        "..WW..",
    };

    private static readonly string[] ArcherWeaponPixels =
    {
        "..WW..",
        ".W..W.",
        "W....W",
        "W....R",
        "W...R.",
        ".W.R..",
        "..R...",
        ".W.R..",
        "W...R.",
        "W....R",
        "W....W",
        ".W..W.",
        "..WW..",
    };

    private static readonly string[] MageWeaponPixels =
    {
        "..CC..",
        "..CC..",
        "..GG..",
        "..WW..",
        "..WW..",
        "..WW..",
        "..WW..",
        "..WW..",
        "..WW..",
        "..WW..",
        "..WW..",
        ".WWWW.",
        "..WW..",
    };

    private static readonly string[][] FlameVariants =
    {
        new[]
        {
            "...M...",
            "...C...",
            "..CCC..",
            "..CCC..",
            ".CCCCC.",
            ".CCCCC.",
            ".MMMMM.",
            "..MMM..",
            "...M...",
        },
        new[]
        {
            "...C...",
            "..CCC..",
            "..CCC..",
            ".CCCCC.",
            ".CMMMC.",
            "..MMM..",
            "..MMM..",
            "...M...",
            "...M...",
        },
        new[]
        {
            "...C...",
            "...C...",
            "..CCC..",
            ".CCCCC.",
            ".CCMMC.",
            ".MMMMM.",
            "..MMM..",
            "..MMM..",
            "...M...",
        },
    };

    // --- Enemy variants ---

    // Goblin (original)
    private static readonly string[] GoblinPixels =
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

    // Skeleton
    private static readonly string[] SkeletonPixels =
    {
        "..WWWWWW..",
        ".WWWWWWWW.",
        ".WKWWWKWW.",
        ".WWKKKWWW.",
        ".WWWWWWWW.",
        "..WWWWWW..",
        "...WWWW...",
        "..WWWWWW..",
        ".WWWWWWWW.",
        "..WWWWWW..",
        "...WW.WW..",
        "...WW.WW..",
        "..GGG.GGG.",
    };

    // Slime
    private static readonly string[] SlimePixels =
    {
        "..........",
        "..........",
        "...GGGG...",
        "..GGGGGG..",
        ".GGMGGMGG.",
        ".GGGGGGGG.",
        ".GGGGGGGG.",
        "GGGGGGGGGG",
        "GGGGGGGGGG",
        "GGGGGGGGGG",
        ".GGGGGGGG.",
        "..GGGGGG..",
        "..........",
    };

    // Demon
    private static readonly string[] DemonPixels =
    {
        ".RR....RR.",
        ".RRR..RRR.",
        "..RRRRRR..",
        ".RRMRRRMR.",
        ".RRRRRRRR.",
        "..RRRRRR..",
        "..RRRRRR..",
        ".RRRRRRRR.",
        ".RRRRRRRR.",
        "..RR..RR..",
        "..RR..RR..",
        ".DDD..DDD.",
        "..........",
    };

    // --- Boss sprite (demonic warlord) ---
    private static readonly string[] BossPixels =
    {
        ".CC......CC.",
        ".CCC....CCC.",
        "..CCCCCCCC..",
        ".CCCCCCCCCC.",
        ".CCMCCCMCCC.",
        ".CCCCCCCCCC.",
        "..CCC..CCC..",
        ".CCCCCCCCCC.",
        "CCCCCCCCCCCC",
        "CCCCCCCCCCCC",
        ".CCCCCCCCCC.",
        "..CCC..CCC..",
        "..CCC..CCC..",
        ".DDDD..DDDD.",
    };

    // --- Item and chest sprites ---

    private static readonly string[] HealingBottlePixels =
    {
        "...CC...",
        "...WW...",
        "..WGGW..",
        "..WGGW..",
        "..WGGW..",
        "..WGGW..",
        "...WW...",
        "...SS...",
    };

    private static readonly string[] DeepBottlePixels =
    {
        "...CC...",
        "..WWWW..",
        "..WLLW..",
        ".WLLLLW.",
        ".WLLLLW.",
        "..WLLW..",
        "...WW...",
        "...SS...",
    };

    private static readonly string[] EmberBombPixels =
    {
        "...FF...",
        "..FYYF..",
        ".FYYYYF.",
        ".FYYYYF.",
        ".FYYYYF.",
        "..FYYF..",
        "...FF...",
        "..CC....",
    };

    private static readonly string[] StarfireBombPixels =
    {
        "...GG...",
        "..GYYG..",
        ".GYYYYG.",
        ".GYYYYG.",
        ".GYYYYG.",
        "..GYYG..",
        "...GG...",
        "..CC....",
    };

    private static readonly string[] ShieldPixels =
    {
        "...GG...",
        "..GBBG..",
        "..BBBB..",
        "..BBBB..",
        "..BBBB..",
        "...BB...",
        "...BB...",
        "...SS...",
    };

    private static readonly string[] DraughtPixels =
    {
        "...CC...",
        "..WWWW..",
        "..WRRW..",
        ".WRRRRW.",
        ".WRRRRW.",
        "..WRRW..",
        "...WW...",
        "...SS...",
    };

    private static readonly string[] GiantSealPixels =
    {
        "..GGGG..",
        ".GYYYYG.",
        "GYYCCYYG",
        "GYCYYCYG",
        "GYYCCYYG",
        ".GYYYYG.",
        "..GGGG..",
        "...SS...",
    };

    private static readonly string[] ChestClosedPixels =
    {
        ".GGGGGG.",
        "GBBBBBBG",
        "GBWWWWBG",
        "GBWWWWBG",
        "GBBBBBBG",
        "GBGGGGBG",
        "GB....BG",
        ".GGGGGG.",
    };

    private static readonly string[] ChestOpenedPixels =
    {
        ".GGGGGG.",
        "GBBBBBBG",
        "GBG..GBG",
        "G......G",
        "GBWWWWBG",
        "GBWWWWBG",
        "GB....BG",
        ".GGGGGG.",
    };

    // --- Color maps ---

    private static Color GetFighterColor(char c) => c switch
    {
        'H' => new Color(0.55f, 0.40f, 0.25f), // hair
        'F' => new Color(0.85f, 0.70f, 0.55f), // face
        'B' => new Color(0.35f, 0.35f, 0.45f), // steel armor
        'A' => new Color(0.75f, 0.60f, 0.20f), // gold trim
        'L' => new Color(0.25f, 0.25f, 0.30f), // legs
        'S' => new Color(0.35f, 0.22f, 0.12f), // shoes
        _ => Colors.Transparent,
    };

    private static Color GetArcherColor(char c) => c switch
    {
        'H' => new Color(0.65f, 0.45f, 0.20f), // auburn hair
        'F' => new Color(0.85f, 0.70f, 0.55f), // face
        'B' => new Color(0.28f, 0.45f, 0.22f), // forest green tunic
        'A' => new Color(0.55f, 0.40f, 0.18f), // leather belt
        'L' => new Color(0.30f, 0.25f, 0.15f), // brown pants
        'S' => new Color(0.30f, 0.20f, 0.10f), // boots
        _ => Colors.Transparent,
    };

    private static Color GetMageColor(char c) => c switch
    {
        'H' => new Color(0.40f, 0.35f, 0.50f), // grey-purple hair
        'F' => new Color(0.82f, 0.72f, 0.65f), // pale face
        'P' => new Color(0.30f, 0.18f, 0.50f), // pointed hat
        'B' => new Color(0.25f, 0.15f, 0.45f), // purple robe
        'R' => new Color(0.35f, 0.22f, 0.55f), // robe trim
        'A' => new Color(0.70f, 0.55f, 0.85f), // magic accent
        'L' => new Color(0.20f, 0.15f, 0.30f), // robe bottom
        'S' => new Color(0.28f, 0.18f, 0.12f), // sandals
        _ => Colors.Transparent,
    };

    private static Color GetFighterWeaponColor(char c) => c switch
    {
        'M' => new Color(0.82f, 0.86f, 0.92f),
        'G' => new Color(0.88f, 0.70f, 0.24f),
        'W' => new Color(0.46f, 0.28f, 0.12f),
        _ => Colors.Transparent,
    };

    private static Color GetArcherWeaponColor(char c) => c switch
    {
        'W' => new Color(0.48f, 0.32f, 0.14f),
        'R' => new Color(0.90f, 0.86f, 0.70f),
        _ => Colors.Transparent,
    };

    private static Color GetMageWeaponColor(char c) => c switch
    {
        'W' => new Color(0.43f, 0.29f, 0.17f),
        'G' => new Color(0.86f, 0.72f, 0.38f),
        'C' => new Color(0.64f, 0.82f, 1.0f),
        _ => Colors.Transparent,
    };

    private static Color GetFlameColor(char c, Color baseColor)
    {
        Color outer = baseColor.Darkened(0.28f);
        outer.A = 0.65f;

        Color mid = baseColor.Lightened(0.1f);
        mid.A = 0.82f;

        Color core = baseColor.Lerp(Palette.TextLight, 0.55f);
        core.A = 0.95f;

        return c switch
        {
            'M' => outer,
            'C' => mid,
            'H' => core,
            _ => Colors.Transparent,
        };
    }

    private static Color GetGoblinColor(char c) => c switch
    {
        'E' => Palette.EnemyBody,
        'M' => new Color(0.95f, 0.85f, 0.2f),  // eyes
        'D' => new Color(0.40f, 0.20f, 0.10f),  // feet
        _ => Colors.Transparent,
    };

    private static Color GetSkeletonColor(char c) => c switch
    {
        'W' => new Color(0.85f, 0.82f, 0.75f),  // bone white
        'K' => new Color(0.15f, 0.12f, 0.10f),  // eye sockets
        'G' => new Color(0.55f, 0.50f, 0.45f),  // grey feet
        _ => Colors.Transparent,
    };

    private static Color GetSlimeColor(char c) => c switch
    {
        'G' => Palette.SlimeBody,
        'M' => Palette.SlimeCore,
        _ => Colors.Transparent,
    };

    private static Color GetDemonColor(char c) => c switch
    {
        'R' => new Color(0.70f, 0.15f, 0.15f),  // red body
        'M' => new Color(0.95f, 0.80f, 0.10f),  // yellow eyes
        'D' => new Color(0.30f, 0.10f, 0.10f),  // dark hooves
        _ => Colors.Transparent,
    };

    private static Color GetBossColor(char c) => c switch
    {
        'C' => new Color(0.55f, 0.10f, 0.50f),  // dark purple
        'M' => new Color(1.0f, 0.20f, 0.10f),   // glowing red eyes
        'D' => new Color(0.25f, 0.08f, 0.08f),   // dark hooves
        _ => Colors.Transparent,
    };

    private static Color GetItemColor(char c, ItemVisualId visualId) => c switch
    {
        'C' => new Color(0.90f, 0.86f, 0.72f),
        'W' => new Color(0.95f, 0.95f, 0.95f),
        'G' => visualId switch
        {
            ItemVisualId.HealingBottle => Palette.ItemHeal,
            ItemVisualId.DeepBottle => Palette.ItemHealMajor,
            ItemVisualId.SannosShield => Palette.ItemWard,
            ItemVisualId.GiantSeal => Palette.ChestMetal,
            _ => Palette.Accent
        },
        'L' => Palette.ItemHealMajor,
        'Y' => visualId == ItemVisualId.StarfireBomb ? Palette.ItemBombMajor : Palette.ItemBomb,
        'B' => Palette.ItemWard,
        'R' => Palette.ItemPower,
        'F' => Palette.BgDark,
        'S' => new Color(0, 0, 0, 0.18f),
        _ => Colors.Transparent,
    };

    private static Color GetChestColor(char c) => c switch
    {
        'G' => Palette.ChestMetal,
        'B' => Palette.ChestWood,
        'W' => Palette.Accent,
        '.' => Colors.Transparent,
        _ => Colors.Transparent,
    };

    // --- Enemy variant arrays for random selection ---

    private static readonly string[][] EnemyVariants = { GoblinPixels, SkeletonPixels, SlimePixels, DemonPixels };
    private static readonly System.Func<char, Color>[] EnemyColorMaps = { GetGoblinColor, GetSkeletonColor, GetSlimeColor, GetDemonColor };
    private static readonly string[] EnemyNames = { "Goblin", "Skeleton", "Slime", "Demon" };
    private static readonly Dictionary<ItemVisualId, ImageTexture> ItemTextureCache = new();
    private static ImageTexture _closedChestTexture;
    private static ImageTexture _openedChestTexture;

    // --- Public API ---

    /// <summary>Returns a random enemy variant index (0-3).</summary>
    public static int RandomEnemyVariant() => (int)(GD.Randi() % (uint)EnemyVariants.Length);

    /// <summary>Gets the display name for an enemy variant.</summary>
    public static string EnemyVariantName(int variant) => EnemyNames[variant % EnemyNames.Length];

    public static ImageTexture CreatePlayerTexture(Archetype archetype)
    {
        return archetype switch
        {
            Archetype.Fighter => BuildTexture(FighterPixels, GetFighterColor),
            Archetype.Archer => BuildTexture(ArcherPixels, GetArcherColor),
            Archetype.Mage => BuildTexture(MagePixels, GetMageColor),
            _ => BuildTexture(FighterPixels, GetFighterColor),
        };
    }

    public static ImageTexture CreateWeaponTexture(Archetype archetype)
    {
        return archetype switch
        {
            Archetype.Fighter => BuildTexture(FighterWeaponPixels, GetFighterWeaponColor),
            Archetype.Archer => BuildTexture(ArcherWeaponPixels, GetArcherWeaponColor),
            Archetype.Mage => BuildTexture(MageWeaponPixels, GetMageWeaponColor),
            _ => BuildTexture(FighterWeaponPixels, GetFighterWeaponColor),
        };
    }

    public static ImageTexture CreateFlameTexture(Color baseColor, int variant = 0)
    {
        int index = Mathf.Abs(variant) % FlameVariants.Length;
        return BuildTexture(FlameVariants[index], c => GetFlameColor(c, baseColor));
    }

    public static ImageTexture CreateEnemyTexture(int variant = 0)
    {
        int idx = variant % EnemyVariants.Length;
        return BuildTexture(EnemyVariants[idx], EnemyColorMaps[idx]);
    }

    public static ImageTexture CreateBossTexture()
    {
        return BuildTexture(BossPixels, GetBossColor);
    }

    public static ImageTexture CreateItemTexture(ItemVisualId visualId)
    {
        if (ItemTextureCache.TryGetValue(visualId, out var cached))
            return cached;

        var pixels = visualId switch
        {
            ItemVisualId.HealingBottle => HealingBottlePixels,
            ItemVisualId.DeepBottle => DeepBottlePixels,
            ItemVisualId.EmberBomb => EmberBombPixels,
            ItemVisualId.StarfireBomb => StarfireBombPixels,
            ItemVisualId.SannosShield => ShieldPixels,
            ItemVisualId.MarauderDraught => DraughtPixels,
            ItemVisualId.GiantSeal => GiantSealPixels,
            _ => HealingBottlePixels,
        };

        var texture = BuildTexture(pixels, c => GetItemColor(c, visualId));
        ItemTextureCache[visualId] = texture;
        return texture;
    }

    public static ImageTexture CreateChestTexture(bool opened = false)
    {
        if (opened)
        {
            _openedChestTexture ??= BuildTexture(ChestOpenedPixels, GetChestColor);
            return _openedChestTexture;
        }

        _closedChestTexture ??= BuildTexture(ChestClosedPixels, GetChestColor);
        return _closedChestTexture;
    }

    // Backward compat: parameterless version defaults to Fighter
    public static ImageTexture CreatePlayerTexture() => CreatePlayerTexture(Archetype.Fighter);

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
