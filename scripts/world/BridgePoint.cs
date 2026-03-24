using Godot;

namespace ARPG;

/// <summary>
/// Builds a purple energy bridge from one zone edge to the next once enough dark energy is gathered.
/// </summary>
public partial class BridgePoint : Node3D
{
    private enum BridgeState { Inactive, Ready, Building, Built }

    private BridgeState _state = BridgeState.Inactive;
    private Node3D _arrowIndicator;
    private Label3D _promptLabel;
    private Area3D _interactZone;
    private bool _playerInZone;
    private float _arrowBobTime;
    private Vector3 _targetGroundGlobalPosition;

    private const float InteractRadius = 5f;
    private const float BridgeWidth = 3f;
    private const float BridgeThickness = 0.2f;
    private const float SegmentLength = 1.25f;
    private const float MaxStepHeight = 0.12f;

    public bool IsBuilt => _state == BridgeState.Built;
    public bool IsReadyAndPlayerNear => _state == BridgeState.Ready && _playerInZone;

    public override void _Ready()
    {
        BuildArrowIndicator();
        BuildInteractZone();
        BuildPromptLabel();
    }

    public void Configure(Vector3 targetGroundGlobalPosition)
    {
        _targetGroundGlobalPosition = targetGroundGlobalPosition;
    }

    /// <summary>Called by GameManager when dark energy threshold is met.</summary>
    public void SetEnergyReady()
    {
        if (_state != BridgeState.Inactive)
            return;

        _state = BridgeState.Ready;
        _arrowIndicator.Visible = true;
    }

    /// <summary>Attempt to build the bridge. Returns true if building started.</summary>
    public bool TryBuild()
    {
        if (_state != BridgeState.Ready || !_playerInZone)
            return false;

        _state = BridgeState.Building;
        _arrowIndicator.Visible = false;
        _promptLabel.Visible = false;
        SpawnBridge();
        return true;
    }

    public override void _Process(double delta)
    {
        if (_state != BridgeState.Ready)
            return;

        _arrowBobTime += (float)delta;
        _arrowIndicator.Position = new Vector3(0, 3.5f + Mathf.Sin(_arrowBobTime * 3f) * 0.3f, 0);
        _promptLabel.Visible = _playerInZone;
    }

    private void SpawnBridge()
    {
        var bridge = new Node3D();
        bridge.Name = "EnergyBridge";
        AddChild(bridge);

        var mat = CreateBridgeMaterial();
        Vector3 localTarget = _targetGroundGlobalPosition - GlobalPosition;
        float horizontalDistance = Mathf.Max(Mathf.Abs(localTarget.Z), 0.8f);
        float heightDelta = Mathf.Abs(localTarget.Y);
        int segmentCount = Mathf.Max(2, Mathf.CeilToInt(Mathf.Max(horizontalDistance / SegmentLength, heightDelta / MaxStepHeight)));
        float segmentDepth = Mathf.Max(horizontalDistance / segmentCount, 0.45f);

        for (int i = 0; i < segmentCount; i++)
        {
            int segmentIndex = i;
            float delay = i * 0.04f;
            float t0 = i / (float)segmentCount;
            float t1 = (i + 1) / (float)segmentCount;
            float tMid = (t0 + t1) * 0.5f;

            var segment = new StaticBody3D();
            segment.Position = new Vector3(
                Mathf.Lerp(0.0f, localTarget.X, tMid),
                Mathf.Lerp(0.0f, localTarget.Y, tMid) - BridgeThickness * 0.5f,
                Mathf.Lerp(0.0f, localTarget.Z, tMid));

            var meshInst = new MeshInstance3D();
            meshInst.Mesh = new BoxMesh { Size = new Vector3(BridgeWidth, BridgeThickness, segmentDepth) };
            meshInst.MaterialOverride = mat;
            meshInst.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            segment.AddChild(meshInst);

            var collision = new CollisionShape3D();
            collision.Shape = new BoxShape3D { Size = new Vector3(BridgeWidth, BridgeThickness, segmentDepth) };
            segment.AddChild(collision);

            meshInst.Transparency = 1.0f;
            bridge.AddChild(segment);

            var tween = CreateTween();
            tween.TweenInterval(delay);
            tween.TweenProperty(meshInst, "transparency", 0.0f, 0.15f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);

            if (segmentIndex == segmentCount - 1)
            {
                tween.TweenCallback(Callable.From(() =>
                {
                    _state = BridgeState.Built;
                }));
            }
        }
    }

    private void BuildArrowIndicator()
    {
        _arrowIndicator = new Node3D();
        _arrowIndicator.Name = "ArrowIndicator";
        _arrowIndicator.Position = new Vector3(0, 3.5f, 0);
        _arrowIndicator.Visible = false;

        var mesh = new MeshInstance3D();
        mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        var cone = new CylinderMesh
        {
            TopRadius = 0f,
            BottomRadius = 0.5f,
            Height = 1.0f,
            RadialSegments = 12,
        };
        cone.Material = new StandardMaterial3D
        {
            AlbedoColor = new Color(Palette.DarkEnergyGlow, 0.85f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = Palette.DarkEnergyGlow,
            EmissionEnergyMultiplier = 2.0f,
            Roughness = 0.2f,
        };
        mesh.Mesh = cone;
        mesh.RotationDegrees = new Vector3(180, 0, 0);
        _arrowIndicator.AddChild(mesh);

        AddChild(_arrowIndicator);
    }

    private void BuildInteractZone()
    {
        _interactZone = new Area3D();
        _interactZone.Name = "InteractZone";

        var shape = new CollisionShape3D();
        shape.Shape = new SphereShape3D { Radius = InteractRadius };
        _interactZone.AddChild(shape);

        _interactZone.BodyEntered += body =>
        {
            if (body is PlayerController)
                _playerInZone = true;
        };
        _interactZone.BodyExited += body =>
        {
            if (body is PlayerController)
                _playerInZone = false;
        };

        AddChild(_interactZone);
    }

    private void BuildPromptLabel()
    {
        _promptLabel = new Label3D();
        _promptLabel.Name = "PromptLabel";
        _promptLabel.Text = $"Press {GameKeys.DisplayName(GameKeys.Attack)} to bridge";
        _promptLabel.FontSize = 48;
        _promptLabel.Position = new Vector3(0, 2.2f, 0);
        _promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        _promptLabel.Modulate = Palette.DarkEnergyGlow;
        _promptLabel.OutlineSize = 8;
        _promptLabel.Visible = false;
        AddChild(_promptLabel);
    }

    private static StandardMaterial3D CreateBridgeMaterial()
    {
        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(Palette.BridgePurple, 0.7f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = Palette.DarkEnergyGlow,
            EmissionEnergyMultiplier = 2.0f,
            Roughness = 0.2f,
        };

        var custom = TextureLoader.TryLoad("res://assets/textures/bridge_energy.png");
        if (custom != null)
            mat.AlbedoTexture = custom;

        return mat;
    }
}
