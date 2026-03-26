using Godot;

namespace ARPG;

/// <summary>
/// Translucent ghost mesh showing where a structure will be placed.
/// Tints green when valid, red when invalid. No collision — purely visual.
/// </summary>
public partial class BuildPreview : Node3D
{
	private MeshInstance3D _mesh;
	private StandardMaterial3D _material;
	private bool _valid;

	public void Configure(BuildableStructure template)
	{
		_mesh?.QueueFree();

		_material = new StandardMaterial3D
		{
			AlbedoColor = new Color(Palette.BridgePurple, 0.35f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			EmissionEnabled = true,
			Emission = Palette.DarkEnergyGlow,
			EmissionEnergyMultiplier = 1.5f,
			Roughness = 0.3f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};

		_mesh = new MeshInstance3D();
		_mesh.Mesh = new BoxMesh { Size = template.Size };
		_mesh.MaterialOverride = _material;
		_mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		AddChild(_mesh);

		SetValid(false);
	}

	public void SetValid(bool valid)
	{
		if (_material == null)
			return;

		_valid = valid;
		if (valid)
		{
			_material.AlbedoColor = new Color(0.2f, 0.8f, 0.3f, 0.35f);
			_material.Emission = new Color(0.2f, 0.9f, 0.3f);
		}
		else
		{
			_material.AlbedoColor = new Color(0.9f, 0.2f, 0.2f, 0.35f);
			_material.Emission = new Color(0.9f, 0.2f, 0.2f);
		}
	}
}
