using Godot;

namespace ARPG;

/// <summary>
/// Builds a purple energy bridge from one zone edge to the next once enough dark energy is gathered.
/// Static children (arrow indicator, interact zone, prompt label) are defined in BridgePoint.tscn.
/// Bridge segments are spawned procedurally since their count varies by distance.
/// </summary>
public partial class BridgePoint : Node3D
{
    private Node3D _arrowIndicator;
    private Label3D _promptLabel;
    private Area3D _interactZone;
    private bool _playerInZone;
    private bool _energyRequirementSatisfied;
    private bool _isBuilding;
    private bool _isBuilt;
    private float _arrowBobTime;
    private Vector3 _targetGroundGlobalPosition;

    private const float BridgeWidth = 3f;
    private const float BridgeThickness = 0.2f;
    private const float SegmentLength = 1.25f;
    private const float MaxStepHeight = 0.12f;

    public bool IsBuilt => _isBuilt;
    public bool IsReadyAndPlayerNear => !_isBuilt && !_isBuilding && _energyRequirementSatisfied && _playerInZone;

    public override void _Ready()
    {
        _arrowIndicator = GetNode<Node3D>("ArrowIndicator");
        _interactZone = GetNode<Area3D>("InteractZone");
        _promptLabel = GetNode<Label3D>("PromptLabel");

        // Apply runtime material and colors
        var mesh = _arrowIndicator.GetNode<MeshInstance3D>("Mesh");
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(Palette.DarkEnergyGlow, 0.85f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = Palette.DarkEnergyGlow,
            EmissionEnergyMultiplier = 2.0f,
            Roughness = 0.2f,
        };

        _promptLabel.Text = $"Press {GameKeys.DisplayName(GameKeys.Attack)} to bridge";
        _promptLabel.Modulate = Palette.DarkEnergyGlow;

        _interactZone.BodyEntered += body =>
        {
            if (body is PlayerController)
            {
                _playerInZone = true;
                UpdateReadyVisuals();
            }
        };
        _interactZone.BodyExited += body =>
        {
            if (body is PlayerController)
            {
                _playerInZone = false;
                UpdateReadyVisuals();
            }
        };

        UpdateReadyVisuals();
    }

    public void Configure(Vector3 targetGroundGlobalPosition)
    {
        _targetGroundGlobalPosition = targetGroundGlobalPosition;
    }

    public void SetEnergyRequirementSatisfied(bool satisfied)
    {
        if (_isBuilt)
            return;

        _energyRequirementSatisfied = satisfied;
        UpdateReadyVisuals();
    }

    /// <summary>Attempt to build the bridge. Returns true if building started.</summary>
    public bool TryBuild()
    {
        if (_isBuilt || _isBuilding || !_energyRequirementSatisfied || !_playerInZone)
            return false;

        _isBuilding = true;
        UpdateReadyVisuals();
        SpawnBridge();
        return true;
    }

    public override void _Process(double delta)
    {
        if (_isBuilt || _isBuilding || !_energyRequirementSatisfied)
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
            segment.AddToGroup(WorldGroups.CameraBlockers);
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
                    _isBuilding = false;
                    _isBuilt = true;
                    UpdateReadyVisuals();
                }));
            }
        }
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

    private void UpdateReadyVisuals()
    {
        bool ready = !_isBuilt && !_isBuilding && _energyRequirementSatisfied;
        _arrowIndicator.Visible = ready;
        _promptLabel.Visible = ready && _playerInZone;
    }
}
