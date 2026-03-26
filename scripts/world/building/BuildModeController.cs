using System;
using Godot;

namespace ARPG;

/// <summary>
/// Manages build mode lifecycle: toggling on/off, cycling structures, rotating,
/// confirming placement, and deducting energy. Only active during exploration.
/// </summary>
public partial class BuildModeController : Node
{
	private const int MaxStructuresPerZone = 5;

	[Signal] public delegate void StructureBuiltEventHandler(int energyCost);
	[Signal] public delegate void BuildModeToggledEventHandler(bool active);

	private PlayerController _player;
	private Camera3D _camera;
	private Func<Aabb> _getZoneBounds;
	private Func<DarkEnergy> _getZoneEnergy;
	private Func<bool> _isBridgeBuilt;
	private Func<bool> _isExploring;
	private Node3D _structuresRoot;

	private BuildableStructure[] _templates;
	private int _selectedIndex;
	private StructurePlacer _placer;
	private int _structuresPlacedInZone;
	private bool _active;

	public bool IsActive => _active;
	public BuildableStructure SelectedTemplate => _templates?[_selectedIndex];

	public void Init(
		PlayerController player,
		Camera3D camera,
		Func<Aabb> getZoneBounds,
		Func<DarkEnergy> getZoneEnergy,
		Func<bool> isBridgeBuilt,
		Func<bool> isExploring,
		Node3D structuresRoot)
	{
		_player = player;
		_camera = camera;
		_getZoneBounds = getZoneBounds;
		_getZoneEnergy = getZoneEnergy;
		_isBridgeBuilt = isBridgeBuilt;
		_isExploring = isExploring;
		_structuresRoot = structuresRoot;
		_templates = BuildableStructure.AllTemplates();
		_selectedIndex = 0;
	}

	public void ResetZoneCount()
	{
		_structuresPlacedInZone = 0;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed(GameKeys.BuildMode))
		{
			if (_active)
				ExitBuildMode();
			else if (_isExploring())
				EnterBuildMode();
			return;
		}

		if (!_active)
			return;

		if (@event.IsActionPressed(GameKeys.RotateBuild))
		{
			_placer?.RotatePreview();
		}
		else if (@event.IsActionPressed(GameKeys.Attack))
		{
			TryConfirmPlacement();
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventMouseButton mouse && mouse.Pressed)
		{
			if (mouse.ButtonIndex == MouseButton.WheelUp)
			{
				CycleTemplate(-1);
				GetViewport().SetInputAsHandled();
			}
			else if (mouse.ButtonIndex == MouseButton.WheelDown)
			{
				CycleTemplate(1);
				GetViewport().SetInputAsHandled();
			}
		}
		else if (@event.IsActionPressed(GameKeys.Pause))
		{
			ExitBuildMode();
			GetViewport().SetInputAsHandled();
		}
	}

	private void EnterBuildMode()
	{
		if (_templates == null || _templates.Length == 0)
			return;

		_active = true;
		_selectedIndex = 0;

		_placer = new StructurePlacer();
		AddChild(_placer);
		_placer.Configure(_camera, _player, _getZoneBounds(), _templates[_selectedIndex]);

		EmitSignal(SignalName.BuildModeToggled, true);
	}

	private void ExitBuildMode()
	{
		_active = false;

		if (_placer != null)
		{
			_placer.Cleanup();
			_placer.QueueFree();
			_placer = null;
		}

		EmitSignal(SignalName.BuildModeToggled, false);
	}

	private void CycleTemplate(int direction)
	{
		if (_templates == null || _templates.Length <= 1)
			return;

		_selectedIndex = (_selectedIndex + direction + _templates.Length) % _templates.Length;
		_placer?.UpdateTemplate(_templates[_selectedIndex]);
		EmitSignal(SignalName.BuildModeToggled, true);
	}

	private void TryConfirmPlacement()
	{
		if (_placer == null || !_placer.IsPlacementValid)
			return;

		var template = _templates[_selectedIndex];
		var energy = _getZoneEnergy();
		bool bridgeBuilt = _isBridgeBuilt();

		if (energy.Spendable(bridgeBuilt) < template.EnergyCost)
			return;

		if (_structuresPlacedInZone >= MaxStructuresPerZone)
			return;

		var structure = _placer.TryConfirm();
		if (structure == null)
			return;

		_structuresRoot.AddChild(structure);
		energy.TrySpend(template.EnergyCost, bridgeBuilt);
		_structuresPlacedInZone++;

		EmitSignal(SignalName.StructureBuilt, template.EnergyCost);
	}

	public void ForceExit()
	{
		if (_active)
			ExitBuildMode();
	}
}
