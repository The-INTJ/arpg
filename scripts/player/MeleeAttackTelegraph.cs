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

    public void Play(
        AttackVolumeDefinition volume,
        Vector3 direction,
        float attackReach,
        float attackSize,
        Color color,
        float durationSeconds)
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

        Transform = volume.BuildTransform(Vector3.Zero, direction, attackReach, attackSize);
        Position += new Vector3(0, 0.04f, 0);

        _meshInstance.Mesh = BuildVolumeMesh(volume, attackReach, attackSize);
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

    private static Mesh BuildVolumeMesh(AttackVolumeDefinition volume, float attackReach, float attackSize)
    {
        Vector3 size = volume.ResolveSize(attackReach, attackSize);
        return volume.Shape switch
        {
            AttackVolumeShape.Box => BuildQuadMesh(size.X, size.Z),
            AttackVolumeShape.Sphere => BuildCircleMesh(Mathf.Max(size.X, size.Z) * 0.5f, 18),
            AttackVolumeShape.Capsule => BuildCircleMesh(Mathf.Max(size.X, size.Z) * 0.5f, 18),
            AttackVolumeShape.Cylinder => BuildCircleMesh(Mathf.Max(size.X, size.Z) * 0.5f, 18),
            _ => BuildQuadMesh(size.X, size.Z),
        };
    }

    private static ArrayMesh BuildQuadMesh(float width, float depth)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 normal = Vector3.Up;
        Vector3 a = new(-width * 0.5f, 0, -depth * 0.5f);
        Vector3 b = new(width * 0.5f, 0, -depth * 0.5f);
        Vector3 c = new(width * 0.5f, 0, depth * 0.5f);
        Vector3 d = new(-width * 0.5f, 0, depth * 0.5f);

        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(a);
        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(b);
        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(c);

        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(a);
        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(c);
        surfaceTool.SetNormal(normal);
        surfaceTool.AddVertex(d);
        return surfaceTool.Commit();
    }

    private static ArrayMesh BuildCircleMesh(float radius, int segments)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 center = Vector3.Zero;
        Vector3 normal = Vector3.Up;

        for (int i = 0; i < segments; i++)
        {
            float angle0 = Mathf.Tau * i / segments;
            float angle1 = Mathf.Tau * (i + 1) / segments;
            Vector3 edge0 = new(Mathf.Sin(angle0) * radius, 0, Mathf.Cos(angle0) * radius);
            Vector3 edge1 = new(Mathf.Sin(angle1) * radius, 0, Mathf.Cos(angle1) * radius);

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
