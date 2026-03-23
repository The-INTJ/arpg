using Godot;

namespace ARPG;

public partial class MapGenerator : Node3D
{
    // Scale factor applied to all positions and sizes to fill a 100x100 map
    private const float S = 2.0f;

    private static readonly Vector4[][] Layouts =
    {
        new[] // L-shaped walls
        {
            new Vector4(-10, -7.5f, 15, 0.5f),
            new Vector4(-10, -7.5f, 0.5f, 10),
            new Vector4(7.5f, 5, 10, 0.5f),
        },
        new[] // Central pillar + side walls
        {
            new Vector4(0, 0, 4, 4),
            new Vector4(-15, -5, 0.5f, 15),
            new Vector4(15, 2.5f, 0.5f, 12.5f),
        },
        new[] // Corridors
        {
            new Vector4(-5, -10, 0.5f, 12.5f),
            new Vector4(5, 0, 0.5f, 12.5f),
            new Vector4(-12.5f, 7.5f, 10, 0.5f),
            new Vector4(10, -5, 7.5f, 0.5f),
        },
        new[] // Scattered pillars
        {
            new Vector4(-7.5f, -10, 2.5f, 2.5f),
            new Vector4(10, -5, 2.5f, 2.5f),
            new Vector4(-12.5f, 7.5f, 2.5f, 2.5f),
            new Vector4(5, 12.5f, 2.5f, 2.5f),
            new Vector4(0, -2.5f, 2.5f, 2.5f),
        },
    };

    private static readonly Vector3[][] SpawnSets =
    {
        new[] { new Vector3(12.5f, 0.5f, -12.5f), new Vector3(-15, 0.5f, 7.5f), new Vector3(7.5f, 0.5f, 12.5f), new Vector3(-5, 0.5f, -15), new Vector3(15, 0.5f, 2.5f) },
        new[] { new Vector3(-7.5f, 0.5f, -12.5f), new Vector3(10, 0.5f, -10), new Vector3(-10, 0.5f, 10), new Vector3(7.5f, 0.5f, 12.5f), new Vector3(12.5f, 0.5f, -2.5f) },
        new[] { new Vector3(-12.5f, 0.5f, -5), new Vector3(12.5f, 0.5f, 7.5f), new Vector3(0, 0.5f, -15), new Vector3(-10, 0.5f, 12.5f), new Vector3(12.5f, 0.5f, -12.5f) },
        new[] { new Vector3(15, 0.5f, -12.5f), new Vector3(-15, 0.5f, -7.5f), new Vector3(12.5f, 0.5f, 10), new Vector3(-10, 0.5f, 15), new Vector3(-2.5f, 0.5f, 7.5f) },
    };

    public Vector3[] Generate()
    {
        int idx = (int)(GD.Randi() % Layouts.Length);
        var layout = Layouts[idx];
        var spawns = SpawnSets[idx];

        // Boundary walls (scaled to 200x200 arena)
        PlaceWall(0, -25 * S, 52 * S, 1, 4, Palette.BoundaryWall, true);
        PlaceWall(0, 25 * S, 52 * S, 1, 4, Palette.BoundaryWall, true);
        PlaceWall(-25 * S, 0, 1, 52 * S, 4, Palette.BoundaryWall, true);
        PlaceWall(25 * S, 0, 1, 52 * S, 4, Palette.BoundaryWall, true);

        // Interior walls (scaled positions and sizes)
        foreach (var wall in layout)
            PlaceWall(wall.X * S, wall.Y * S, wall.Z * S, wall.W * S, 3f, Palette.Wall, true);

        // Scatter trees
        PlaceTrees(layout);

        // Shuffle and return spawn positions (scaled)
        var shuffled = (Vector3[])spawns.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = (int)(GD.Randi() % (i + 1));
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Scale X/Z positions, keep Y at ground level
        for (int i = 0; i < shuffled.Length; i++)
            shuffled[i] = new Vector3(shuffled[i].X * S, shuffled[i].Y, shuffled[i].Z * S);

        return shuffled;
    }

    private void PlaceWall(float x, float z, float w, float d, float h, Color color, bool textured)
    {
        var body = new StaticBody3D();
        AddChild(body);
        body.Position = new Vector3(x, h / 2, z);

        var mesh = new MeshInstance3D();
        var box = new BoxMesh { Size = new Vector3(w, h, d) };

        StandardMaterial3D mat;
        if (textured)
        {
            mat = CreateStoneMaterial(color);
            // UV mapping: triplanar for good texture on all faces
            mat.Uv1Triplanar = true;
            mat.Uv1TriplanarSharpness = 1.0f;
            mat.Uv1Scale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        else
        {
            mat = new StandardMaterial3D { AlbedoColor = color };
        }

        box.Material = mat;
        mesh.Mesh = box;
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        var boxShape = new BoxShape3D { Size = new Vector3(w, h, d) };
        shape.Shape = boxShape;
        body.AddChild(shape);
    }

    private void PlaceTrees(Vector4[] wallLayout)
    {
        int treeCount = (int)(GD.Randi() % 8) + 12; // 12-19 trees
        float mapExtent = 22 * S; // Stay inside boundary walls

        for (int i = 0; i < treeCount; i++)
        {
            float x = (float)GD.RandRange(-mapExtent, mapExtent);
            float z = (float)GD.RandRange(-mapExtent, mapExtent);

            // Skip if too close to player start or exit door
            if (new Vector2(x, z - 40).Length() < 6) continue;
            if (new Vector2(x, z + 46).Length() < 6) continue;

            // Skip if overlapping a wall
            bool overlaps = false;
            foreach (var wall in wallLayout)
            {
                float wx = wall.X * S, wz = wall.Y * S;
                float ww = wall.Z * S, wd = wall.W * S;
                if (x > wx - ww / 2 - 2 && x < wx + ww / 2 + 2 &&
                    z > wz - wd / 2 - 2 && z < wz + wd / 2 + 2)
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps) continue;

            PlaceTree(x, z);
        }
    }

    private void PlaceTree(float x, float z)
    {
        var tree = new StaticBody3D();
        tree.Position = new Vector3(x, 0, z);
        AddChild(tree);

        // Trunk
        float trunkHeight = (float)GD.RandRange(1.5, 3.0);
        float trunkRadius = (float)GD.RandRange(0.15, 0.3);
        var trunkMesh = new MeshInstance3D();
        var cylinder = new CylinderMesh
        {
            TopRadius = trunkRadius * 0.7f,
            BottomRadius = trunkRadius,
            Height = trunkHeight,
        };
        var trunkMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.40f, 0.28f, 0.15f),
            Roughness = 0.9f,
        };
        cylinder.Material = trunkMat;
        trunkMesh.Mesh = cylinder;
        trunkMesh.Position = new Vector3(0, trunkHeight / 2, 0);
        tree.AddChild(trunkMesh);

        // Canopy — random between cone (pine) and sphere (deciduous)
        bool isPine = GD.Randi() % 2 == 0;
        var canopyMesh = new MeshInstance3D();

        float green = (float)GD.RandRange(0.25, 0.55);
        var canopyMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.15f, green, 0.12f),
            Roughness = 0.85f,
        };

        if (isPine)
        {
            float coneHeight = (float)GD.RandRange(1.5, 3.0);
            float coneRadius = (float)GD.RandRange(0.8, 1.4);
            var cone = new CylinderMesh
            {
                TopRadius = 0,
                BottomRadius = coneRadius,
                Height = coneHeight,
            };
            cone.Material = canopyMat;
            canopyMesh.Mesh = cone;
            canopyMesh.Position = new Vector3(0, trunkHeight + coneHeight / 2 - 0.2f, 0);
        }
        else
        {
            float sphereRadius = (float)GD.RandRange(0.8, 1.5);
            var sphere = new SphereMesh { Radius = sphereRadius, Height = sphereRadius * 2 };
            sphere.Material = canopyMat;
            canopyMesh.Mesh = sphere;
            canopyMesh.Position = new Vector3(0, trunkHeight + sphereRadius * 0.5f, 0);
        }

        tree.AddChild(canopyMesh);

        // Simple collision (cylinder around trunk)
        var shape = new CollisionShape3D();
        shape.Shape = new CylinderShape3D { Radius = trunkRadius + 0.1f, Height = trunkHeight };
        shape.Position = new Vector3(0, trunkHeight / 2, 0);
        tree.AddChild(shape);
    }

    /// <summary>
    /// Creates a stone-like material with procedural noise texture.
    /// </summary>
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
        return mat;
    }

    /// <summary>
    /// Creates a gradient from dark to light variants of the base color.
    /// </summary>
    private static Gradient CreateStoneGradient(Color baseColor)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, baseColor.Darkened(0.3f));
        gradient.SetColor(1, baseColor.Lightened(0.15f));
        return gradient;
    }

    /// <summary>
    /// Creates a textured ground material with noise.
    /// </summary>
    public static StandardMaterial3D CreateGroundMaterial()
    {
        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = 0.03f;
        noise.FractalOctaves = 4;

        var noiseTex = new NoiseTexture2D();
        noiseTex.Noise = noise;
        noiseTex.Width = 128;
        noiseTex.Height = 128;

        var gradient = new Gradient();
        gradient.SetColor(0, Palette.Floor.Darkened(0.2f));
        gradient.SetColor(1, Palette.Floor.Lightened(0.1f));
        noiseTex.ColorRamp = gradient;

        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = noiseTex;
        mat.AlbedoColor = Palette.Floor;
        mat.Roughness = 0.95f;
        mat.Uv1Triplanar = true;
        mat.Uv1Scale = new Vector3(0.1f, 0.1f, 0.1f);
        return mat;
    }
}
