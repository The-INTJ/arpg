using Godot;

namespace ARPG;

/// <summary>
/// Raycasts from the camera to position a ghost preview of the selected structure,
/// snapping to height tiers. Validates placement and spawns confirmed structures.
/// </summary>
public partial class StructurePlacer : Node3D
{
	private static readonly float[] HeightTiers = { 0.0f, 1.15f, 2.25f };
	private const float SnapThreshold = 0.8f;
	private const float MaxPlaceDistance = 30f;
	private const float RampAngleDeg = 15f;

	private Camera3D _camera;
	private CollisionObject3D _playerBody;
	private Aabb _zoneBounds;
	private BuildableStructure _template;
	private BuildPreview _preview;
	private float _rotationY;
	private bool _placementValid;

	public bool IsPlacementValid => _placementValid;

	public void Configure(Camera3D camera, CollisionObject3D playerBody, Aabb zoneBounds, BuildableStructure template)
	{
		_camera = camera;
		_playerBody = playerBody;
		_zoneBounds = zoneBounds;
		_template = template;
		_rotationY = 0f;
		_placementValid = false;

		_preview?.QueueFree();
		_preview = new BuildPreview();
		AddChild(_preview);
		_preview.Configure(template);
		_preview.Visible = false;
	}

	public void UpdateTemplate(BuildableStructure template)
	{
		_template = template;
		_preview?.Configure(template);
	}

	public void RotatePreview()
	{
		_rotationY = (_rotationY + Mathf.Pi * 0.5f) % Mathf.Tau;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_camera == null || _preview == null)
			return;

		var hitResult = RaycastFromCamera();
		if (hitResult == null)
		{
			_preview.Visible = false;
			_placementValid = false;
			return;
		}

		Vector3 hitPos = hitResult.Value;
		float snappedY = SnapToHeightTier(hitPos.Y);
		Vector3 placementPos = new(hitPos.X, snappedY + _template.Size.Y * 0.5f, hitPos.Z);

		_preview.GlobalPosition = placementPos;
		_preview.Rotation = new Vector3(0, _rotationY, 0);

		if (_template.Kind == StructureKind.Ramp)
			ApplyRampTilt();

		_placementValid = ValidatePlacement(placementPos);
		_preview.SetValid(_placementValid);
		_preview.Visible = true;
	}

	/// <summary>
	/// Confirm placement and return the spawned StaticBody3D, or null if invalid.
	/// </summary>
	public StaticBody3D TryConfirm()
	{
		if (!_placementValid || _preview == null || !_preview.Visible)
			return null;

		var structure = SpawnStructure(_preview.GlobalPosition, _preview.Rotation);
		return structure;
	}

	public void Cleanup()
	{
		_preview?.QueueFree();
		_preview = null;
	}

	private Vector3? RaycastFromCamera()
	{
		var spaceState = GetWorld3D()?.DirectSpaceState;
		if (spaceState == null)
			return null;

		Vector2 screenCenter = GetViewport().GetVisibleRect().Size * 0.5f;
		Vector3 from = _camera.ProjectRayOrigin(screenCenter);
		Vector3 dir = _camera.ProjectRayNormal(screenCenter);
		Vector3 to = from + dir * MaxPlaceDistance;

		var exclude = new Godot.Collections.Array<Rid>();
		if (_playerBody != null)
			exclude.Add(_playerBody.GetRid());

		var query = PhysicsRayQueryParameters3D.Create(from, to);
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		query.Exclude = exclude;

		var result = spaceState.IntersectRay(query);
		if (result.Count == 0)
			return null;

		return result["position"].AsVector3();
	}

	private static float SnapToHeightTier(float y)
	{
		float best = HeightTiers[0];
		float bestDist = Mathf.Abs(y - best);

		for (int i = 1; i < HeightTiers.Length; i++)
		{
			float dist = Mathf.Abs(y - HeightTiers[i]);
			if (dist < bestDist)
			{
				best = HeightTiers[i];
				bestDist = dist;
			}
		}

		return bestDist <= SnapThreshold ? best : y;
	}

	private void ApplyRampTilt()
	{
		float snappedBase = SnapToHeightTier(_preview.GlobalPosition.Y - _template.Size.Y * 0.5f);
		int tierIndex = -1;
		for (int i = 0; i < HeightTiers.Length; i++)
		{
			if (Mathf.Abs(snappedBase - HeightTiers[i]) < 0.01f)
			{
				tierIndex = i;
				break;
			}
		}

		if (tierIndex >= 0 && tierIndex < HeightTiers.Length - 1)
		{
			float heightGain = HeightTiers[tierIndex + 1] - HeightTiers[tierIndex];
			float run = _template.Size.Z;
			float angle = Mathf.Atan2(heightGain, run);
			Vector3 rot = _preview.Rotation;

			// Tilt along the local forward axis based on rotation
			float sinY = Mathf.Sin(_rotationY);
			float cosY = Mathf.Cos(_rotationY);
			_preview.Rotation = new Vector3(
				-angle * cosY,
				rot.Y,
				angle * sinY);

			// Shift position so bottom rests on surface, top reaches next tier
			_preview.GlobalPosition += new Vector3(0, heightGain * 0.5f, 0);
		}
	}

	private bool ValidatePlacement(Vector3 position)
	{
		// Must be within zone bounds
		if (!_zoneBounds.HasPoint(position))
			return false;

		// Must be within zone bounds (check structure extents too)
		Vector3 halfSize = _template.Size * 0.5f;
		if (!_zoneBounds.HasPoint(position + halfSize) || !_zoneBounds.HasPoint(position - halfSize))
			return false;

		return true;
	}

	private StaticBody3D SpawnStructure(Vector3 globalPos, Vector3 rotation)
	{
		var body = new StaticBody3D();
		body.AddToGroup(WorldGroups.CameraBlockers);
		body.GlobalPosition = globalPos;
		body.Rotation = rotation;

		var mat = new StandardMaterial3D
		{
			AlbedoColor = new Color(Palette.BridgePurple, 0.7f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			EmissionEnabled = true,
			Emission = Palette.DarkEnergyGlow,
			EmissionEnergyMultiplier = 2.0f,
			Roughness = 0.2f,
		};

		var meshInst = new MeshInstance3D();
		meshInst.Mesh = new BoxMesh { Size = _template.Size };
		meshInst.MaterialOverride = mat;
		meshInst.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		meshInst.Transparency = 1.0f;
		body.AddChild(meshInst);

		var collision = new CollisionShape3D();
		collision.Shape = new BoxShape3D { Size = _template.Size };
		body.AddChild(collision);

		// Fade-in animation matching BridgePoint style
		var tween = body.CreateTween();
		tween.TweenProperty(meshInst, "transparency", 0.0f, 0.25f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		return body;
	}
}
