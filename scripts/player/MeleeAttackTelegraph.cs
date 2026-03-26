using Godot;

namespace ARPG;

public partial class MeleeAttackTelegraph : Node3D
{
    private MeshInstance3D _meshInstance;

    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.Name = "Mesh";
        AddChild(_meshInstance);
    }

    public void Play(Vector3 direction, float range, float arcDegrees, Color color, float durationSeconds)
    {
        if (_meshInstance == null)
        {
            _meshInstance = GetNodeOrNull<MeshInstance3D>("Mesh");
            if (_meshInstance == null)
            {
                _meshInstance = new MeshInstance3D();
                _meshInstance.Name = "Mesh";
                AddChild(_meshInstance);
            }
        }

        Vector3 flatDirection = direction;
        flatDirection.Y = 0.0f;
        if (flatDirection.LengthSquared() <= 0.001f)
            flatDirection = Vector3.Forward;
        else
            flatDirection = flatDirection.Normalized();

        Rotation = new Vector3(0, Mathf.Atan2(flatDirection.X, flatDirection.Z), 0);
        Position = new Vector3(0, 0.04f, 0);

        _meshInstance.Mesh = BuildSectorMesh(range, arcDegrees, 14);
        _meshInstance.MaterialOverride = BuildMaterial(color);
        _meshInstance.Transparency = 0.0f;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_meshInstance, "transparency", 1.0f, durationSeconds)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(_meshInstance, "scale", new Vector3(1.06f, 1.0f, 1.06f), durationSeconds)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private static ArrayMesh BuildSectorMesh(float range, float arcDegrees, int segments)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        float halfArcRadians = Mathf.DegToRad(arcDegrees * 0.5f);
        Vector3 center = Vector3.Zero;
        Vector3 normal = Vector3.Up;

        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float angle0 = Mathf.Lerp(-halfArcRadians, halfArcRadians, t0);
            float angle1 = Mathf.Lerp(-halfArcRadians, halfArcRadians, t1);

            Vector3 edge0 = new(Mathf.Sin(angle0) * range, 0, Mathf.Cos(angle0) * range);
            Vector3 edge1 = new(Mathf.Sin(angle1) * range, 0, Mathf.Cos(angle1) * range);

            surfaceTool.SetNormal(normal);
            surfaceTool.AddVertex(center);
            surfaceTool.SetNormal(normal);
            surfaceTool.AddVertex(edge0);
            surfaceTool.SetNormal(normal);
            surfaceTool.AddVertex(edge1);
        }

        return surfaceTool.Commit();
    }

    private static StandardMaterial3D BuildMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(color, 0.28f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 0.9f,
            Roughness = 1.0f,
        };
    }
}
