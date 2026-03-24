using Godot;

namespace ARPG;

/// <summary>
/// Replaces ExitDoor. When dark energy threshold is met, shows a floating arrow.
/// Player presses E nearby to build a purple energy bridge to the next chunk.
/// Walking across the bridge advances to the next room.
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

	private const float InteractRadius = 5f;
	private const int BridgeSegments = 10;
	private const float SegmentLength = 2.5f;
	private const float BridgeWidth = 3f;

	public override void _Ready()
	{
		BuildArrowIndicator();
		BuildInteractZone();
		BuildPromptLabel();
	}

	/// <summary>Called by GameManager when dark energy threshold is met.</summary>
	public void SetEnergyReady()
	{
		if (_state != BridgeState.Inactive) return;
		_state = BridgeState.Ready;
		_arrowIndicator.Visible = true;
	}

	/// <summary>Attempt to build the bridge. Returns true if building started.</summary>
	public bool TryBuild()
	{
		if (_state != BridgeState.Ready || !_playerInZone) return false;
		_state = BridgeState.Building;
		_arrowIndicator.Visible = false;
		_promptLabel.Visible = false;
		SpawnBridge();
		return true;
	}

	public bool IsReadyAndPlayerNear => _state == BridgeState.Ready && _playerInZone;

	public override void _Process(double delta)
	{
		if (_state == BridgeState.Ready)
		{
			_arrowBobTime += (float)delta;
			_arrowIndicator.Position = new Vector3(0, 3.5f + Mathf.Sin(_arrowBobTime * 3f) * 0.3f, 0);
			_promptLabel.Visible = _playerInZone;
		}
	}

	private void SpawnBridge()
	{
		var bridge = new Node3D();
		bridge.Name = "EnergyBridge";
		AddChild(bridge);

		var mat = CreateBridgeMaterial();

		for (int i = 0; i < BridgeSegments; i++)
		{
			int segIndex = i;
			float delay = i * 0.1f;

			var segment = new StaticBody3D();
			segment.Position = new Vector3(0, 0, -(i * SegmentLength) - SegmentLength / 2f);

			var meshInst = new MeshInstance3D();
			meshInst.Mesh = new BoxMesh { Size = new Vector3(BridgeWidth, 0.2f, SegmentLength) };
			meshInst.MaterialOverride = mat;
			meshInst.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			segment.AddChild(meshInst);

			var collision = new CollisionShape3D();
			collision.Shape = new BoxShape3D { Size = new Vector3(BridgeWidth, 0.2f, SegmentLength) };
			segment.AddChild(collision);

			// Start invisible, tween in
			meshInst.Transparency = 1.0f;
			bridge.AddChild(segment);

			var tween = CreateTween();
			tween.TweenInterval(delay);
			tween.TweenProperty(meshInst, "transparency", 0.0f, 0.15f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);

			// On last segment, add trigger and mark built
			if (segIndex == BridgeSegments - 1)
			{
				tween.TweenCallback(Callable.From(() =>
				{
					AddBridgeEndTrigger(bridge, -(BridgeSegments * SegmentLength));
					_state = BridgeState.Built;
				}));
			}
		}
	}

	private void AddBridgeEndTrigger(Node3D bridge, float zPos)
	{
		var trigger = new Area3D();
		trigger.Position = new Vector3(0, 0.5f, zPos);

		var shape = new CollisionShape3D();
		shape.Shape = new BoxShape3D { Size = new Vector3(BridgeWidth, 2f, 2f) };
		trigger.AddChild(shape);

		trigger.BodyEntered += OnBridgeEndEntered;
		bridge.AddChild(trigger);
	}

	private void OnBridgeEndEntered(Node3D body)
	{
		if (body is not PlayerController player) return;

		if (GameState.CurrentRoom >= GameState.TotalRooms)
		{
			GameState.FinalizeCurrentRun(RunOutcome.Victory, player.Stats);
			GetTree().ChangeSceneToFile(Scenes.VictoryScreen);
			return;
		}

		AudioManager.Instance?.PlayLevelUp();
		GameState.CurrentRoom++;
		GetTree().ChangeSceneToFile(Scenes.Game);
	}

	private void BuildArrowIndicator()
	{
		_arrowIndicator = new Node3D();
		_arrowIndicator.Name = "ArrowIndicator";
		_arrowIndicator.Position = new Vector3(0, 3.5f, 0);
		_arrowIndicator.Visible = false;

		// Downward-pointing cone
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
		// Rotate so the point faces down
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
			if (body is PlayerController) _playerInZone = true;
		};
		_interactZone.BodyExited += body =>
		{
			if (body is PlayerController) _playerInZone = false;
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
