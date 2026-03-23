using Godot;

namespace ARPG;

public partial class MapGenerator : Node3D
{
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

        // Boundary walls
        PlaceWall(0, -25, 52, 1, 3, Palette.BoundaryWall);
        PlaceWall(0, 25, 52, 1, 3, Palette.BoundaryWall);
        PlaceWall(-25, 0, 1, 52, 3, Palette.BoundaryWall);
        PlaceWall(25, 0, 1, 52, 3, Palette.BoundaryWall);

        // Interior walls
        foreach (var wall in layout)
            PlaceWall(wall.X, wall.Y, wall.Z, wall.W, 2.5f, Palette.Wall);

        // Shuffle and return spawn positions
        var shuffled = (Vector3[])spawns.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = (int)(GD.Randi() % (i + 1));
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int enemyCount = 4 + (int)(GD.Randi() % 2);
        var result = new Vector3[enemyCount];
        for (int i = 0; i < enemyCount && i < shuffled.Length; i++)
            result[i] = shuffled[i];

        return result;
    }

    private void PlaceWall(float x, float z, float w, float d, float h, Color color)
    {
        var body = new StaticBody3D();
        AddChild(body);
        body.Position = new Vector3(x, h / 2, z);

        var mesh = new MeshInstance3D();
        var box = new BoxMesh { Size = new Vector3(w, h, d) };
        var mat = new StandardMaterial3D { AlbedoColor = color };
        box.Material = mat;
        mesh.Mesh = box;
        body.AddChild(mesh);

        var shape = new CollisionShape3D();
        var boxShape = new BoxShape3D { Size = new Vector3(w, h, d) };
        shape.Shape = boxShape;
        body.AddChild(shape);
    }
}
