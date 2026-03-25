using Godot;

namespace ARPG;

[Tool]
public partial class SlimeVisual : Node3D
{
    private static StandardMaterial3D _bodyMaterial;
    private static StandardMaterial3D _coreMaterial;
    private static StandardMaterial3D _eyeMaterial;

    [Export]
    public float IdleBobAmplitude { get; set; } = 0.035f;

    [Export]
    public float IdlePulseAmount { get; set; } = 0.055f;

    [Export]
    public float IdleSpeed { get; set; } = 2.2f;

    private Node3D _bobRoot;
    private float _time;

    public override void _Ready()
    {
        _bobRoot = GetNodeOrNull<Node3D>("BobRoot");
        ApplyConfiguredState();
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_bobRoot == null)
            return;

        if (Engine.IsEditorHint())
        {
            _bobRoot.Position = Vector3.Zero;
            _bobRoot.Scale = Vector3.One;
            return;
        }

        _time += (float)delta * IdleSpeed;
        float bob = Mathf.Sin(_time) * IdleBobAmplitude;
        float stretch = Mathf.Sin(_time + 0.5f) * IdlePulseAmount;

        _bobRoot.Position = new Vector3(0, bob, 0);
        _bobRoot.Scale = new Vector3(1.0f - stretch * 0.35f, 1.0f + stretch, 1.0f - stretch * 0.35f);
    }

    private void ApplyConfiguredState()
    {
        var bobRoot = _bobRoot ?? GetNodeOrNull<Node3D>("BobRoot");
        if (bobRoot == null)
            return;

        ApplyMaterialRecursive(bobRoot);
    }

    private static void ApplyMaterialRecursive(Node node)
    {
        if (node is MeshInstance3D mesh)
            mesh.MaterialOverride = ResolveMaterial(mesh.Name);

        foreach (Node child in node.GetChildren())
            ApplyMaterialRecursive(child);
    }

    private static Material ResolveMaterial(string meshName)
    {
        if (meshName.Contains("Core"))
            return CreateCoreMaterial();

        if (meshName.Contains("Eye"))
            return CreateEyeMaterial();

        return CreateBodyMaterial();
    }

    private static StandardMaterial3D CreateBodyMaterial()
    {
        _bodyMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.SlimeBody,
            Roughness = 0.28f,
            EmissionEnabled = true,
            Emission = Palette.SlimeBody.Darkened(0.18f),
            EmissionEnergyMultiplier = 0.22f,
        };
        return _bodyMaterial;
    }

    private static StandardMaterial3D CreateCoreMaterial()
    {
        _coreMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.SlimeCore,
            Roughness = 0.18f,
            EmissionEnabled = true,
            Emission = Palette.SlimeCore,
            EmissionEnergyMultiplier = 1.3f,
        };
        return _coreMaterial;
    }

    private static StandardMaterial3D CreateEyeMaterial()
    {
        _eyeMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = Palette.SlimeEye,
            Roughness = 0.42f,
            EmissionEnabled = true,
            Emission = Palette.SlimeEye,
            EmissionEnergyMultiplier = 0.1f,
        };
        return _eyeMaterial;
    }
}
