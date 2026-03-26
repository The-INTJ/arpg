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

    private static PackedScene _cavePocketSliceScene;
    private static PackedScene _platformRampSliceScene;
    private static PackedScene _pineTreeSliceScene;
    private static PackedScene _roundTreeSliceScene;
    private readonly List<SurfaceRect> _spawnSurfaces = new();

    public ChunkIslandStyle IslandStyle { get; set; } = ChunkIslandStyle.Main;

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
        ChunkBuilder.BuildChunk(shell, ChunkWidth, ChunkDepth, ChunkThickness, IslandStyle);
    }

    private GeneratedMapResult BuildRidgeLayout(int caveSide)
    {
        int ridgeSide = -caveSide;

        PlacePlatform(ridgeSide * 19.0f, -4.0f, 20.0f, 78.0f, MidTop, WorldSurfaceKind.Mid);
        PlaceRamp(ridgeSide * 13.0f, 24.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);
        PlaceRamp(ridgeSide * 13.0f, -28.0f, 10.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: ridgeSide > 0);

        PlaceMidToHighRise(ridgeSide * 25.0f, -16.0f, 14.0f, 20.0f, ridgeSide > 0);

        PlaceGroundToMidRise(-ridgeSide * 4.0f, 26.0f, 16.0f, 18.0f, -ridgeSide > 0);

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

        PlacePlatform(0, -6.0f, 34.0f, 32.0f, MidTop, WorldSurfaceKind.Mid);
        PlaceRamp(-14.0f, -6.0f, 12.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: true);
        PlaceRamp(14.0f, -6.0f, 12.0f, 12.0f, GroundTop, MidTop, alongX: true, ascendPositive: false);

        PlaceMidToHighRise(highSide * 10.0f, -20.0f, 16.0f, 18.0f, highSide > 0);

        PlaceGroundToMidRise(caveSide * -18.0f, 22.0f, 14.0f, 20.0f, -caveSide > 0);

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

        _spawnSurfaces.Add(new SurfaceRect(caveFloorX, centerZ, 18.0f, 24.0f, GroundTop));
        _spawnSurfaces.Add(new SurfaceRect(shelfX, centerZ, 10.0f, 12.0f, MidTop));

        var slice = LoadCavePocketSlice().Instantiate<Node3D>();
        slice.Name = "CavePocketSlice";
        slice.Position = new Vector3(caveFloorX, GroundTop, centerZ);
        if (side < 0)
            slice.Rotation = new Vector3(0, Mathf.Pi, 0);

        AddChild(slice);

        Vector3 defaultChestPosition = new(shelfX, MidTop, centerZ);
        Vector3 defaultFallbackPosition = new(caveFloorX - side * 2.0f, GroundTop, centerZ);

        return new CavePocketResult(
            ResolveSliceAnchorPosition(slice, SceneSliceAnchorKind.CaveChest, defaultChestPosition),
            ResolveSliceAnchorPosition(slice, SceneSliceAnchorKind.FallbackItem, defaultFallbackPosition));
    }

    private void PlaceGroundToMidRise(float rampX, float z, float platformWidth, float platformDepth, bool ascendPositive)
    {
        PlacePlatformRampSlice(
            rampX,
            z,
            platformWidth,
            platformDepth,
            MidTop,
            platformOffsetX: 6.0f,
            rampWidth: 10.0f,
            rampRun: 12.0f,
            rampLowTop: GroundTop,
            rampHighTop: MidTop,
            WorldSurfaceKind.Mid,
            ascendPositive,
            "GroundToMidRise");
    }

    private void PlaceMidToHighRise(float rampX, float z, float platformWidth, float platformDepth, bool ascendPositive)
    {
        PlacePlatformRampSlice(
            rampX,
            z,
            platformWidth,
            platformDepth,
            HighTop,
            platformOffsetX: 6.0f,
            rampWidth: 10.0f,
            rampRun: 12.0f,
            rampLowTop: MidTop,
            rampHighTop: HighTop,
            WorldSurfaceKind.High,
            ascendPositive,
            "MidToHighRise");
    }

    private void PlacePlatformRampSlice(
        float rampX,
        float z,
        float platformWidth,
        float platformDepth,
        float platformTopHeight,
        float platformOffsetX,
        float rampWidth,
        float rampRun,
        float rampLowTop,
        float rampHighTop,
        WorldSurfaceKind platformSurfaceKind,
        bool ascendPositive,
        string sliceName)
    {
        float platformCenterX = rampX + (ascendPositive ? platformOffsetX : -platformOffsetX);
        _spawnSurfaces.Add(new SurfaceRect(platformCenterX, z, platformWidth, platformDepth, platformTopHeight));

        var slice = LoadPlatformRampSlice().Instantiate<PlatformRampSlice>();
        slice.Name = sliceName;
        slice.Configure(
            new Vector2(platformWidth, platformDepth),
            platformTopHeight,
            platformOffsetX,
            rampWidth,
            rampRun,
            rampLowTop,
            rampHighTop,
            platformSurfaceKind);
        slice.Position = new Vector3(rampX, 0, z);
        if (!ascendPositive)
            slice.Rotation = new Vector3(0, Mathf.Pi, 0);

        AddChild(slice);
    }

    private void ClearGeneratedGeometry()
    {
        _spawnSurfaces.Clear();
        foreach (Node child in GetChildren())
            child.QueueFree();
    }

    private void PlacePlatform(float x, float z, float width, float depth, float topHeight, WorldSurfaceKind surfaceKind)
    {
        var body = new StaticBody3D();
        body.AddToGroup(WorldGroups.CameraBlockers);
        body.Position = new Vector3(x, topHeight - FloorThickness / 2.0f, z);
        AddChild(body);

        var mesh = new MeshInstance3D();
        mesh.Mesh = new BoxMesh
        {
            Size = new Vector3(width, FloorThickness, depth),
            Material = WorldMaterials.GetSurfaceMaterial(surfaceKind),
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
        body.AddToGroup(WorldGroups.CameraBlockers);
        body.Position = new Vector3(x, centerY, z);
        body.Rotation = alongX
            ? new Vector3(0, 0, ascendPositive ? angle : -angle)
            : new Vector3(ascendPositive ? -angle : angle, 0, 0);
        AddChild(body);

        var mesh = new MeshInstance3D();
        mesh.Mesh = new BoxMesh
        {
            Size = size,
            Material = WorldMaterials.GetSurfaceMaterial(WorldSurfaceKind.Ramp),
        };
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = size };
        body.AddChild(shape);

        AddRampEdgeRocks(body, size, alongX);
    }

    private void AddRampEdgeRocks(StaticBody3D rampBody, Vector3 rampSize, bool alongX)
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = GD.Randi();
        var rockMat = WorldMaterials.GetRockMaterial();

        float length = alongX ? rampSize.X : rampSize.Z;
        float width = alongX ? rampSize.Z : rampSize.X;
        float halfWidth = width * 0.5f;

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 3; i++)
            {
                float t = rng.RandfRange(0.1f, 0.9f);
                float along = (t - 0.5f) * length;
                float radius = rng.RandfRange(0.2f, 0.45f);
                float edgeOffset = halfWidth + rng.RandfRange(-0.2f, 0.3f);

                var rock = new MeshInstance3D();
                rock.Mesh = new SphereMesh
                {
                    Radius = radius,
                    Height = radius * rng.RandfRange(0.7f, 1.2f),
                };
                rock.MaterialOverride = rockMat;
                rock.Position = alongX
                    ? new Vector3(along, RampThickness * 0.5f + radius * 0.2f, side * edgeOffset)
                    : new Vector3(side * edgeOffset, RampThickness * 0.5f + radius * 0.2f, along);
                rock.Scale = new Vector3(
                    rng.RandfRange(0.7f, 1.3f),
                    rng.RandfRange(0.4f, 0.8f),
                    rng.RandfRange(0.7f, 1.3f));
                rock.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                rampBody.AddChild(rock);
            }
        }
    }

    private void PlaceTree(Vector3 position)
    {
        PackedScene treeScene = GD.Randi() % 2 == 0 ? LoadPineTreeSlice() : LoadRoundTreeSlice();
        var tree = treeScene.Instantiate<StaticBody3D>();
        tree.Position = position;
        tree.Rotation = new Vector3(0, (float)GD.RandRange(0.0, Mathf.Tau), 0);
        tree.Scale = Vector3.One * (float)GD.RandRange(0.88, 1.18);
        AddChild(tree);
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

    private static PackedScene LoadCavePocketSlice()
    {
        return _cavePocketSliceScene ??= GD.Load<PackedScene>(Scenes.CavePocketSlice);
    }

    private static PackedScene LoadPlatformRampSlice()
    {
        return _platformRampSliceScene ??= GD.Load<PackedScene>(Scenes.PlatformRampSlice);
    }

    private static PackedScene LoadPineTreeSlice()
    {
        return _pineTreeSliceScene ??= GD.Load<PackedScene>(Scenes.PineTreeSlice);
    }

    private static PackedScene LoadRoundTreeSlice()
    {
        return _roundTreeSliceScene ??= GD.Load<PackedScene>(Scenes.RoundTreeSlice);
    }

    private Vector3 ResolveSliceAnchorPosition(Node root, SceneSliceAnchorKind kind, Vector3 fallbackPosition)
    {
        foreach (SceneSliceAnchor anchor in EnumerateSceneSliceAnchors(root))
        {
            if (anchor.Kind == kind)
                return ToLocal(anchor.GlobalPosition);
        }

        return fallbackPosition;
    }

    private IEnumerable<SceneSliceAnchor> EnumerateSceneSliceAnchors(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is SceneSliceAnchor anchor)
                yield return anchor;

            foreach (SceneSliceAnchor nestedAnchor in EnumerateSceneSliceAnchors(child))
                yield return nestedAnchor;
        }
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
