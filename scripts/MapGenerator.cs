using Godot;

namespace ARPG;

public partial class MapGenerator : Node3D
{
    // Hardcoded layouts: each is a list of wall positions (x, z) and sizes (w, d)
    private static readonly Vector4[][] Layouts =
    {
        // Layout 0: L-shaped walls
        new[]
        {
            new Vector4(-4, -3, 6, 0.3f),
            new Vector4(-4, -3, 0.3f, 4),
            new Vector4(3, 2, 4, 0.3f),
        },
        // Layout 1: Central pillar + side walls
        new[]
        {
            new Vector4(0, 0, 1.5f, 1.5f),
            new Vector4(-6, -2, 0.3f, 6),
            new Vector4(6, 1, 0.3f, 5),
        },
        // Layout 2: Corridors
        new[]
        {
            new Vector4(-2, -4, 0.3f, 5),
            new Vector4(2, 0, 0.3f, 5),
            new Vector4(-5, 3, 4, 0.3f),
            new Vector4(4, -2, 3, 0.3f),
        },
        // Layout 3: Scattered pillars
        new[]
        {
            new Vector4(-3, -4, 1, 1),
            new Vector4(4, -2, 1, 1),
            new Vector4(-5, 3, 1, 1),
            new Vector4(2, 5, 1, 1),
            new Vector4(0, -1, 1, 1),
        },
    };

    // Valid spawn positions per layout (open areas)
    private static readonly Vector3[][] SpawnSets =
    {
        new[] { new Vector3(5, 0.5f, -5), new Vector3(-6, 0.5f, 3), new Vector3(3, 0.5f, 5), new Vector3(-2, 0.5f, -6), new Vector3(6, 0.5f, 1) },
        new[] { new Vector3(-3, 0.5f, -5), new Vector3(4, 0.5f, -4), new Vector3(-4, 0.5f, 4), new Vector3(3, 0.5f, 5), new Vector3(5, 0.5f, -1) },
        new[] { new Vector3(-5, 0.5f, -2), new Vector3(5, 0.5f, 3), new Vector3(0, 0.5f, -6), new Vector3(-4, 0.5f, 5), new Vector3(5, 0.5f, -5) },
        new[] { new Vector3(6, 0.5f, -5), new Vector3(-6, 0.5f, -3), new Vector3(5, 0.5f, 4), new Vector3(-4, 0.5f, 6), new Vector3(-1, 0.5f, 3) },
    };

    public Vector3[] Generate()
    {
        int idx = (int)(GD.Randi() % Layouts.Length);
        var layout = Layouts[idx];
        var spawns = SpawnSets[idx];

        // Place walls
        foreach (var wall in layout)
        {
            var body = new StaticBody3D();
            AddChild(body);
            body.Position = new Vector3(wall.X, 1, wall.Y);

            var mesh = new MeshInstance3D();
            var box = new BoxMesh();
            box.Size = new Vector3(wall.Z, 2, wall.W);
            var wallMat = new StandardMaterial3D();
            wallMat.AlbedoColor = new Color(0.4f, 0.35f, 0.3f);
            box.Material = wallMat;
            mesh.Mesh = box;
            body.AddChild(mesh);

            var shape = new CollisionShape3D();
            var boxShape = new BoxShape3D();
            boxShape.Size = new Vector3(wall.Z, 2, wall.W);
            shape.Shape = boxShape;
            body.AddChild(shape);
        }

        // Shuffle spawns and return 4-5 positions for enemies
        var shuffled = (Vector3[])spawns.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = (int)(GD.Randi() % (i + 1));
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int enemyCount = 4 + (int)(GD.Randi() % 2); // 4 or 5
        var result = new Vector3[enemyCount];
        for (int i = 0; i < enemyCount && i < shuffled.Length; i++)
            result[i] = shuffled[i];

        return result;
    }
}
