using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class MapGenerator : Node3D
{
    public const float ChunkWidth = 110.0f;
    public const float ChunkDepth = 112.0f;
    private const float ChunkThickness = 18.0f;
    public const float PlayWidth = 104.0f;
    public const float PlayDepth = 106.0f;
    public const float GroundTop = 0.0f;
    private const float MidTop = 1.15f;
    private const float HighTop = 2.25f;
    private const float FloorThickness = 1.0f;
    private const float RampThickness = 0.6f;

    private static StandardMaterial3D _groundMaterial;
    private static StandardMaterial3D _midGroundMaterial;
    private static StandardMaterial3D _highGroundMaterial;
    private static StandardMaterial3D _caveGroundMaterial;
    private static StandardMaterial3D _rampMaterial;
    private static StandardMaterial3D _rockMaterial;
    private static StandardMaterial3D _caveRockMaterial;
    private static StandardMaterial3D _caveRoofMaterial;
    private readonly List<SurfaceRect> _spawnSurfaces = new();

    public GeneratedMapResult Generate()
    {
        ClearGeneratedGeometry();
        BuildChunkShell();

        int layoutIndex = (int)(GD.Randi() % 3);
        return layoutIndex switch
        {
            0 => BuildRidgeLayout(caveSide: -1),
            1 => BuildRidgeLayout(caveSide: 1),
            _ => BuildMesaLayout(caveSide: GD.Randi() % 2 == 0 ? -1 : 1),
        };
    }

    private void BuildChunkShell()
    {
        var shell = new Node3D();
        shell.Name = "ChunkShell";
        AddChild(shell);
        ChunkBuilder.BuildChunk(shell, ChunkWidth, ChunkDepth, ChunkThickness);

        // Keep the main floor slightly larger than the edge-fall bounds so the player
        // gets snapped back while still above solid ground instead of dropping into the void.
        PlacePlatform(0, 0, PlayWidth, PlayDepth, GroundTop, SurfaceKind.Ground);
    }

    private GeneratedMapResult BuildRidgeLayout(int caveSide)
    {
        int ridgeSide = -caveSide;

        PlacePlatform(ridgeSide * 19.0f, -4.0f, 20.0f, 78.0f, MidTop, SurfaceKind.Mid);
        PlaceRamp(ridgeSide * 13.0f, 24.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);
        PlaceRamp(ridgeSide * 13.0f, -28.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);

        PlacePlatform(ridgeSide * 31.0f, -16.0f, 14.0f, 20.0f, HighTop, SurfaceKind.High);
        PlaceRamp(ridgeSide * 25.0f, -16.0f, 10.0f, 12.0f, MidTop, HighTop, alongX: true, ascendPositive: ridgeSide > 0);

        PlacePlatform(-ridgeSide * 10.0f, 26.0f, 16.0f, 18.0f, MidTop, SurfaceKind.Mid);
        PlaceRamp(-ridgeSide * 4.0f, 26.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: -ridgeSide > 0);

        var caveResult = PlaceCavePocket(caveSide, 10.0f);

        PlaceTree(new Vector3(ridgeSide * 34.0f, 0, 30.0f));
        PlaceTree(new Vector3(ridgeSide * 7.0f, 0, 40.0f));
        PlaceTree(new Vector3(-ridgeSide * 18.0f, 0, -28.0f));
        PlaceTree(new Vector3(caveSide * 12.0f, 0, -34.0f));
        PlaceTree(new Vector3(-caveSide * 24.0f, 0, 12.0f));

        return new GeneratedMapResult(
            new[]
            {
                SpawnPoint(-14.0f, GroundTop, 30.0f),
                SpawnPoint(16.0f, GroundTop, 18.0f),
                SpawnPoint(ridgeSide * 19.0f, MidTop, 18.0f),
                SpawnPoint(ridgeSide * 19.0f, MidTop, -22.0f),
                SpawnPoint(ridgeSide * 31.0f, HighTop, -16.0f),
                SpawnPoint(caveSide * 10.0f, GroundTop, -24.0f),
                SpawnPoint(-caveSide * 8.0f, GroundTop, 4.0f),
                SpawnPoint(-ridgeSide * 10.0f, MidTop, 26.0f),
            },
            caveResult.ChestPosition,
            caveResult.FallbackItemPosition);
    }

    private GeneratedMapResult BuildMesaLayout(int caveSide)
    {
        int highSide = -caveSide;

        PlacePlatform(0, -6.0f, 34.0f, 32.0f, MidTop, SurfaceKind.Mid);
        PlaceRamp(-14.0f, -6.0f, 12.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: true);
        PlaceRamp(14.0f, -6.0f, 12.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: false);

        PlacePlatform(highSide * 16.0f, -20.0f, 16.0f, 18.0f, HighTop, SurfaceKind.High);
        PlaceRamp(highSide * 10.0f, -20.0f, 10.0f, 12.0f, MidTop, HighTop, alongX: true, ascendPositive: highSide > 0);

        PlacePlatform(caveSide * -24.0f, 22.0f, 14.0f, 20.0f, MidTop, SurfaceKind.Mid);
        PlaceRamp(caveSide * -18.0f, 22.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: -caveSide > 0);

        var caveResult = PlaceCavePocket(caveSide, 16.0f);

        PlaceTree(new Vector3(-28.0f, 0, 34.0f));
        PlaceTree(new Vector3(30.0f, 0, 28.0f));
        PlaceTree(new Vector3(highSide * 32.0f, 0, -30.0f));
        PlaceTree(new Vector3(-highSide * 10.0f, 0, -36.0f));
        PlaceTree(new Vector3(caveSide * -30.0f, 0, 6.0f));

        return new GeneratedMapResult(
            new[]
            {
                SpawnPoint(-22.0f, GroundTop, 30.0f),
                SpawnPoint(22.0f, GroundTop, 22.0f),
                SpawnPoint(0, MidTop, -2.0f),
                SpawnPoint(-10.0f, MidTop, -10.0f),
                SpawnPoint(10.0f, MidTop, -4.0f),
                SpawnPoint(highSide * 16.0f, HighTop, -20.0f),
                SpawnPoint(caveSide * -24.0f, MidTop, 22.0f),
                SpawnPoint(caveSide * 8.0f, GroundTop, -28.0f),
            },
            caveResult.ChestPosition,
            caveResult.FallbackItemPosition);
    }

    private CavePocketResult PlaceCavePocket(int side, float centerZ)
    {
        float caveFloorX = side * 34.0f;
        float shelfX = side * 44.0f;

        PlacePlatform(caveFloorX, centerZ, 18.0f, 24.0f, GroundTop, SurfaceKind.Cave);
        PlacePlatform(shelfX, centerZ, 10.0f, 12.0f, MidTop, SurfaceKind.Mid);
        PlaceRamp(side * 39.0f, centerZ, 8.0f, 10.0f, GroundTop, MidTop, alongX: true, ascendPositive: side > 0);

        // Rock masses shape the cave alcove while keeping the new open-chunk layout.
        PlaceRockMass(side * 47.5f, centerZ, 5.0f, 28.0f, 3.8f, caveRock: true);
        PlaceRockMass(side * 40.0f, centerZ - 11.0f, 16.0f, 4.0f, 3.0f, caveRock: true);
        PlaceRockMass(side * 40.0f, centerZ + 11.0f, 16.0f, 4.0f, 3.0f, caveRock: true);
        PlaceCeiling(side * 40.5f, centerZ, 18.0f, 26.0f, 2.55f, 0.7f);
        PlaceLamp(new Vector3(side * 40.0f, 2.0f, centerZ), new Color(1.0f, 0.82f, 0.65f), 7.5f, 1.7f);

        return new CavePocketResult(
            new Vector3(shelfX, MidTop, centerZ),
            new Vector3(caveFloorX - side * 2.0f, GroundTop, centerZ));
    }

    private void ClearGeneratedGeometry()
    {
        _spawnSurfaces.Clear();
        foreach (Node child in GetChildren())
            child.QueueFree();
    }

    private void PlacePlatform(float x, float z, float width, float depth, float topHeight, SurfaceKind surfaceKind)
    {
        var body = new StaticBody3D();
        body.Position = new Vector3(x, topHeight - FloorThickness / 2.0f, z);
        AddChild(body);

        var mesh = new MeshInstance3D();
        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, FloorThickness, depth),
            Material = GetSurfaceMaterial(surfaceKind),
        };
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(width, FloorThickness, depth) };
        body.AddChild(shape);

        _spawnSurfaces.Add(new SurfaceRect(x, z, width, depth, topHeight));
    }

    private void PlaceRamp(
        float x,
        float z,
        float width,
        float horizontalRun,
        float lowTop,
        float highTop,
        bool alongX,
        bool ascendPositive)
    {
        float heightDelta = Mathf.Abs(highTop - lowTop);
        float angle = Mathf.Atan2(heightDelta, horizontalRun);
        float slopedLength = Mathf.Sqrt(horizontalRun * horizontalRun + heightDelta * heightDelta);
        float averageTop = (lowTop + highTop) * 0.5f;
        float centerY = averageTop - RampThickness * 0.5f * Mathf.Cos(angle);

        var size = alongX
            ? new Vector3(slopedLength, RampThickness, width)
            : new Vector3(width, RampThickness, slopedLength);

        var body = new StaticBody3D();
        body.Position = new Vector3(x, centerY, z);
        body.Rotation = alongX
            ? new Vector3(0, 0, ascendPositive ? angle : -angle)
            : new Vector3(ascendPositive ? -angle : angle, 0, 0);
        AddChild(body);

        var mesh = new MeshInstance3D();
        mesh.Mesh = new BoxMesh
        {
            Size = size,
            Material = GetSurfaceMaterial(SurfaceKind.Ramp),
        };
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = size };
        body.AddChild(shape);
    }

    private void PlaceRockMass(float x, float z, float width, float depth, float height, bool caveRock)
    {
        var body = new StaticBody3D();
        body.Position = new Vector3(x, height / 2.0f, z);
        AddChild(body);

        var mesh = new MeshInstance3D();
        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, height, depth),
            Material = caveRock ? GetCaveRockMaterial() : GetRockMaterial(),
        };
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(width, height, depth) };
        body.AddChild(shape);
    }

    private void PlaceCeiling(float x, float z, float width, float depth, float centerY, float thickness)
    {
        var mesh = new MeshInstance3D();
        mesh.Position = new Vector3(x, centerY, z);
        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, thickness, depth),
            Material = GetCaveRoofMaterial(),
        };
        AddChild(mesh);
    }

    private void PlaceLamp(Vector3 position, Color color, float range, float energy)
    {
        var light = new OmniLight3D();
        light.Position = position;
        light.LightColor = color;
        light.OmniRange = range;
        light.LightEnergy = energy;
        light.ShadowEnabled = false;
        AddChild(light);
    }

    private void PlaceTree(Vector3 position)
    {
        var tree = new StaticBody3D();
        tree.Position = position;
        AddChild(tree);

        float trunkHeight = (float)GD.RandRange(1.7, 2.8);
        float trunkRadius = (float)GD.RandRange(0.16, 0.26);
        var trunkMesh = new MeshInstance3D();
        trunkMesh.Mesh = new CylinderMesh
        {
            TopRadius = trunkRadius * 0.72f,
            BottomRadius = trunkRadius,
            Height = trunkHeight,
            Material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.40f, 0.28f, 0.15f),
                Roughness = 0.9f,
            },
        };
        trunkMesh.Position = new Vector3(0, trunkHeight / 2.0f, 0);
        tree.AddChild(trunkMesh);

        bool isPine = GD.Randi() % 2 == 0;
        var canopyMesh = new MeshInstance3D();
        var canopyMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.14f, (float)GD.RandRange(0.30, 0.52), 0.12f),
            Roughness = 0.85f,
        };

        if (isPine)
        {
            float coneHeight = (float)GD.RandRange(1.6, 2.6);
            float coneRadius = (float)GD.RandRange(0.85, 1.3);
            canopyMesh.Mesh = new CylinderMesh
            {
                TopRadius = 0.0f,
                BottomRadius = coneRadius,
                Height = coneHeight,
                Material = canopyMaterial,
            };
            canopyMesh.Position = new Vector3(0, trunkHeight + coneHeight / 2.0f - 0.2f, 0);
        }
        else
        {
            float sphereRadius = (float)GD.RandRange(0.9, 1.4);
            canopyMesh.Mesh = new SphereMesh
            {
                Radius = sphereRadius,
                Height = sphereRadius * 2.0f,
                Material = canopyMaterial,
            };
            canopyMesh.Position = new Vector3(0, trunkHeight + sphereRadius * 0.55f, 0);
        }

        tree.AddChild(canopyMesh);

        var shape = new CollisionShape3D();
        shape.Shape = new CylinderShape3D { Radius = trunkRadius + 0.1f, Height = trunkHeight };
        shape.Position = new Vector3(0, trunkHeight / 2.0f, 0);
        tree.AddChild(shape);
    }

    private Vector3 SpawnPoint(float x, float preferredSurfaceTop, float z)
    {
        float resolvedTop = ResolveSurfaceTop(x, z, preferredSurfaceTop);
        return new Vector3(x, resolvedTop + 0.5f, z);
    }

    private float ResolveSurfaceTop(float x, float z, float fallbackTop)
    {
        float bestTop = fallbackTop;
        bool found = false;

        for (int i = 0; i < _spawnSurfaces.Count; i++)
        {
            if (!_spawnSurfaces[i].Contains(x, z))
                continue;

            if (!found || _spawnSurfaces[i].TopHeight > bestTop)
            {
                bestTop = _spawnSurfaces[i].TopHeight;
                found = true;
            }
        }

        return found ? bestTop : fallbackTop;
    }

    private static StandardMaterial3D GetSurfaceMaterial(SurfaceKind surfaceKind)
    {
        return surfaceKind switch
        {
            SurfaceKind.Ground => _groundMaterial ??= CreateGroundMaterial(Palette.Floor, 0.028f, 0.11f),
            SurfaceKind.Mid => _midGroundMaterial ??= CreateGroundMaterial(Palette.FloorMid, 0.032f, 0.12f),
            SurfaceKind.High => _highGroundMaterial ??= CreateGroundMaterial(Palette.FloorHigh, 0.036f, 0.13f),
            SurfaceKind.Cave => _caveGroundMaterial ??= CreateGroundMaterial(Palette.CaveFloor, 0.05f, 0.18f),
            _ => _rampMaterial ??= CreateGroundMaterial(Palette.Ramp, 0.035f, 0.12f),
        };
    }

    private static StandardMaterial3D GetRockMaterial()
    {
        return _rockMaterial ??= CreateStoneMaterial(Palette.ChunkEdge);
    }

    private static StandardMaterial3D GetCaveRockMaterial()
    {
        return _caveRockMaterial ??= CreateStoneMaterial(Palette.CaveWall);
    }

    private static StandardMaterial3D GetCaveRoofMaterial()
    {
        return _caveRoofMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.CaveShadow,
            Roughness = 0.98f,
        };
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

    public static StandardMaterial3D CreateGroundMaterial()
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

        return GetSurfaceMaterial(SurfaceKind.Ground);
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

    private enum SurfaceKind
    {
        Ground,
        Mid,
        High,
        Cave,
        Ramp,
    }

    private readonly record struct CavePocketResult(Vector3 ChestPosition, Vector3 FallbackItemPosition);

    private readonly record struct SurfaceRect(float CenterX, float CenterZ, float Width, float Depth, float TopHeight)
    {
        public bool Contains(float x, float z)
        {
            return Mathf.Abs(x - CenterX) <= Width * 0.5f && Mathf.Abs(z - CenterZ) <= Depth * 0.5f;
        }
    }
}
