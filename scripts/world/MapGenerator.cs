using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class MapGenerator : Node3D
{
    private const float MapExtent = 60.0f;
    private const float SafetyFloorTop = -0.2f;
    private const float GroundTop = 0.0f;
    private const float MidTop = 0.9f;
    private const float HighTop = 1.8f;
    private const float FloorThickness = 1.0f;
    private const float RampThickness = 0.6f;

    private static StandardMaterial3D _groundMaterial;
    private static StandardMaterial3D _midGroundMaterial;
    private static StandardMaterial3D _highGroundMaterial;
    private static StandardMaterial3D _caveGroundMaterial;
    private static StandardMaterial3D _rampMaterial;
    private static StandardMaterial3D _wallMaterial;
    private static StandardMaterial3D _boundaryWallMaterial;
    private static StandardMaterial3D _caveWallMaterial;
    private static StandardMaterial3D _caveRoofMaterial;
    private readonly List<SurfaceRect> _spawnSurfaces = new();

    public GeneratedMapResult Generate()
    {
        ClearGeneratedGeometry();

        int layoutIndex = (int)(GD.Randi() % 3);
        return layoutIndex switch
        {
            0 => BuildRidgeLayout(caveSide: -1),
            1 => BuildRidgeLayout(caveSide: 1),
            _ => BuildMesaLayout(),
        };
    }

    private GeneratedMapResult BuildRidgeLayout(int caveSide)
    {
        int ridgeSide = -caveSide;

        PlaceSafetyFloor();
        PlaceBoundaryWalls();

        PlacePlatform(0, -2, 34, 114, GroundTop, SurfaceKind.Ground);
        PlacePlatform(ridgeSide * 24, 0, 18, 90, GroundTop, SurfaceKind.Ground);
        PlacePlatform(caveSide * 26, 0, 16, 74, GroundTop, SurfaceKind.Ground);
        PlacePlatform(ridgeSide * 35, 10, 18, 32, MidTop, SurfaceKind.Mid);
        PlaceRamp(ridgeSide * 25, 10, 14, 12, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);
        PlacePlatform(ridgeSide * 44, -20, 14, 18, HighTop, SurfaceKind.High);
        PlaceRamp(ridgeSide * 38, -8, 10, 14, MidTop, HighTop, alongX: false, ascendPositive: false);
        PlacePlatform(ridgeSide * 16, 30, 12, 18, MidTop, SurfaceKind.Mid);
        PlaceRamp(ridgeSide * 10, 30, 8, 10, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);

        PlaceWall(-4, 6, 6, 2, 2.4f, Palette.Wall, true);
        PlaceWall(6, -12, 2, 12, 3.0f, Palette.Wall, true);
        PlaceWall(ridgeSide * 14, -32, 10, 2, 2.5f, Palette.Wall, true);
        PlaceWall(caveSide * 10, 20, 2, 10, 2.0f, Palette.CaveWall, true);

        var caveResult = PlaceCavePocket(caveSide, 10);

        PlaceTree(new Vector3(ridgeSide * 19, 0, 34));
        PlaceTree(new Vector3(ridgeSide * 26, 0, -36));
        PlaceTree(new Vector3(caveSide * 18, 0, -30));
        PlaceTree(new Vector3(8, 0, 44));
        PlaceTree(new Vector3(-10, 0, -40));

        return new GeneratedMapResult(
            new[]
            {
                SpawnPoint(-8, GroundTop, 22),
                SpawnPoint(10, GroundTop, -8),
                SpawnPoint(ridgeSide * 22, GroundTop, 30),
                SpawnPoint(ridgeSide * 35, MidTop, 10),
                SpawnPoint(ridgeSide * 44, HighTop, -20),
                SpawnPoint(caveSide * 16, GroundTop, -20),
            },
            caveResult.ChestPosition,
            caveResult.FallbackItemPosition);
    }

    private GeneratedMapResult BuildMesaLayout()
    {
        PlaceSafetyFloor();
        PlaceBoundaryWalls();

        PlacePlatform(0, -2, 32, 114, GroundTop, SurfaceKind.Ground);
        PlacePlatform(-24, 8, 18, 84, GroundTop, SurfaceKind.Ground);
        PlacePlatform(24, 4, 18, 86, GroundTop, SurfaceKind.Ground);
        PlacePlatform(0, -6, 20, 32, MidTop, SurfaceKind.Mid);
        PlaceRamp(0, 16, 12, 14, GroundTop, MidTop, alongX: false, ascendPositive: false);
        PlacePlatform(28, 20, 14, 18, HighTop, SurfaceKind.High);
        PlaceRamp(18, 18, 10, 12, MidTop, HighTop, alongX: true, ascendPositive: true);
        PlacePlatform(-30, 26, 14, 18, MidTop, SurfaceKind.Mid);
        PlaceRamp(-18, 26, 10, 12, GroundTop, MidTop, alongX: true, ascendPositive: false);

        PlaceWall(0, -28, 14, 2, 2.5f, Palette.Wall, true);
        PlaceWall(-8, 10, 2, 10, 2.2f, Palette.Wall, true);
        PlaceWall(12, -14, 2, 12, 3.0f, Palette.Wall, true);
        PlaceWall(26, -6, 8, 2, 2.0f, Palette.CaveWall, true);

        var caveResult = PlaceCavePocket(side: 1, centerZ: -2);

        PlaceTree(new Vector3(-22, 0, 42));
        PlaceTree(new Vector3(-26, 0, -30));
        PlaceTree(new Vector3(18, 0, 34));
        PlaceTree(new Vector3(22, 0, -36));
        PlaceTree(new Vector3(-6, 0, 48));

        return new GeneratedMapResult(
            new[]
            {
                SpawnPoint(-12, GroundTop, 24),
                SpawnPoint(10, GroundTop, -8),
                SpawnPoint(0, MidTop, -6),
                SpawnPoint(-30, MidTop, 26),
                SpawnPoint(28, HighTop, 20),
                SpawnPoint(-18, GroundTop, -22),
            },
            caveResult.ChestPosition,
            caveResult.FallbackItemPosition);
    }

    private CavePocketResult PlaceCavePocket(int side, float centerZ)
    {
        float caveCenterX = side * 48.0f;
        float shelfCenterX = side * 53.0f;

        PlacePlatform(caveCenterX, centerZ, 20, 30, GroundTop, SurfaceKind.Cave);
        PlacePlatform(shelfCenterX, centerZ, 8, 10, MidTop, SurfaceKind.Mid);
        PlaceRamp(caveCenterX + side * 2.5f, centerZ, 8, 7, GroundTop, MidTop, alongX: true, ascendPositive: side > 0);

        PlaceWall(caveCenterX + side * 9.0f, centerZ, 2, 30, 3.4f, Palette.CaveWall, true);
        PlaceWall(caveCenterX, centerZ - 14.0f, 20, 2, 3.1f, Palette.CaveWall, true);
        PlaceWall(caveCenterX, centerZ + 14.0f, 20, 2, 3.1f, Palette.CaveWall, true);
        PlaceWall(caveCenterX - side * 9.0f, centerZ - 10.0f, 2, 8, 2.9f, Palette.CaveWall, true);
        PlaceWall(caveCenterX - side * 9.0f, centerZ + 10.0f, 2, 8, 2.9f, Palette.CaveWall, true);

        PlaceCeiling(caveCenterX, centerZ, 22, 32, 2.7f, 0.6f);
        PlaceLamp(new Vector3(caveCenterX + side * 3.0f, 2.15f, centerZ), new Color(1.0f, 0.84f, 0.62f), 7.0f, 1.9f);

        return new CavePocketResult(
            new Vector3(shelfCenterX, MidTop, centerZ),
            new Vector3(caveCenterX - side * 3.0f, GroundTop, centerZ));
    }

    private void PlaceBoundaryWalls()
    {
        PlaceWall(0, -MapExtent, 124, 2, 5.7f, Palette.BoundaryWall, true, SafetyFloorTop);
        PlaceWall(0, MapExtent, 124, 2, 5.7f, Palette.BoundaryWall, true, SafetyFloorTop);
        PlaceWall(-MapExtent, 0, 2, 124, 5.7f, Palette.BoundaryWall, true, SafetyFloorTop);
        PlaceWall(MapExtent, 0, 2, 124, 5.7f, Palette.BoundaryWall, true, SafetyFloorTop);
    }

    private void ClearGeneratedGeometry()
    {
        _spawnSurfaces.Clear();
        foreach (Node child in GetChildren())
            child.QueueFree();
    }

    private void PlaceSafetyFloor()
    {
        PlacePlatform(0, 0, 118, 118, SafetyFloorTop, SurfaceKind.Ground);
    }

    private void PlacePlatform(float x, float z, float width, float depth, float topHeight, SurfaceKind surfaceKind)
    {
        var body = new StaticBody3D();
        body.Position = new Vector3(x, topHeight - FloorThickness / 2.0f, z);
        AddChild(body);

        var mesh = new MeshInstance3D();
        var box = new BoxMesh
        {
            Size = new Vector3(width, FloorThickness, depth),
            Material = GetSurfaceMaterial(surfaceKind),
        };
        mesh.Mesh = box;
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

    private void PlaceWall(float x, float z, float width, float depth, float height, Color color, bool textured, float baseTop = 0.0f)
    {
        var body = new StaticBody3D();
        body.Position = new Vector3(x, baseTop + height / 2.0f, z);
        AddChild(body);

        var mesh = new MeshInstance3D();
        var box = new BoxMesh { Size = new Vector3(width, height, depth) };
        box.Material = textured ? GetWallMaterial(color) : new StandardMaterial3D { AlbedoColor = color };
        mesh.Mesh = box;
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

    private static StandardMaterial3D GetWallMaterial(Color color)
    {
        if (color == Palette.BoundaryWall)
            return _boundaryWallMaterial ??= CreateStoneMaterial(Palette.BoundaryWall);
        if (color == Palette.CaveWall)
            return _caveWallMaterial ??= CreateStoneMaterial(Palette.CaveWall);
        if (color == Palette.Wall)
            return _wallMaterial ??= CreateStoneMaterial(Palette.Wall);

        return CreateStoneMaterial(color);
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
        mat.Roughness = 0.85f;
        mat.Uv1Triplanar = true;
        mat.Uv1TriplanarSharpness = 1.0f;
        mat.Uv1Scale = new Vector3(0.5f, 0.5f, 0.5f);
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
        Ramp
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
