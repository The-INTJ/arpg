using Godot;

namespace ARPG;

/// <summary>
/// Detects enemies in range and manages the aggro delay timer before combat starts.
/// </summary>
public partial class AggroSystem : Node, IDeveloperEffectProvider
{
    public const string EncounterWatchEffectId = "encounter_watch";
    public const string EncounterCommitEffectId = "encounter_commit";

    private PlayerController _player;
    private DeveloperToolsManager _developerTools;
    private Enemy _aggroEnemy;
    private float _aggroTimer;
    private const float AggroDelay = 0.6f;
    private const float MaxVerticalDelta = 2.4f;

    [Signal]
    public delegate void AggroTriggeredEventHandler(Enemy enemy);

    [Signal]
    public delegate void AggroSpottedEventHandler(Enemy enemy, string message);

    public void Init(PlayerController player)
    {
        _player = player;
    }

    public void ClearAggro()
    {
        _aggroEnemy = null;
    }

    public void RegisterDeveloperEffects(DeveloperToolsManager developerTools)
    {
        if (_developerTools != null)
            return;

        _developerTools = developerTools;
        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                EncounterWatchEffectId,
                "Encounter Watch",
                "Scan nearby enemies during exploration and mark them as spotting the player.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro",
                Order: 10));
        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                EncounterCommitEffectId,
                "Encounter Commit",
                "Let a spotted enemy finish its timer and start combat.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro",
                Order: 20));
    }

    public void Tick(float delta)
    {
        if (_aggroEnemy != null)
        {
            if (!IsInstanceValid(_aggroEnemy))
            {
                _aggroEnemy = null;
                return;
            }

            if (!IsEffectEnabled(EncounterCommitEffectId))
                return;

            _aggroTimer -= delta;
            if (_aggroTimer <= 0)
            {
                var enemy = _aggroEnemy;
                _aggroEnemy = null;
                EmitSignal(SignalName.AggroTriggered, enemy);
            }
            return;
        }

        if (!IsEffectEnabled(EncounterWatchEffectId))
            return;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && !enemy.HasAggro)
            {
                if (CanEngage(enemy, enemy.SightRange))
                {
                    enemy.ShowAggroIndicator();
                    _aggroEnemy = enemy;
                    _aggroTimer = AggroDelay;
                    string message = enemy.IsBoss ? "Boss spotted!" : "Spotted!";
                    EmitSignal(SignalName.AggroSpotted, enemy, message);
                    return;
                }
            }
        }
    }

    public Enemy FindNearestEnemy(float range)
    {
        Enemy nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
            {
                float dist = GetHorizontalDistance(enemy);
                if (CanEngage(enemy, range) && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }

    private bool CanEngage(Enemy enemy, float horizontalRange)
    {
        if (enemy == null || !IsInstanceValid(enemy))
            return false;

        if (Mathf.Abs(enemy.GlobalPosition.Y - _player.GlobalPosition.Y) > MaxVerticalDelta)
            return false;

        if (GetHorizontalDistance(enemy) > horizontalRange)
            return false;

        return HasLineOfSight(enemy);
    }

    private float GetHorizontalDistance(Enemy enemy)
    {
        Vector3 delta = enemy.GlobalPosition - _player.GlobalPosition;
        return new Vector2(delta.X, delta.Z).Length();
    }

    private bool HasLineOfSight(Enemy enemy)
    {
        var spaceState = _player.GetWorld3D().DirectSpaceState;
        Vector3 origin = _player.GlobalPosition + new Vector3(0, 0.65f, 0);
        Vector3 target = enemy.GlobalPosition + new Vector3(0, 0.35f, 0);

        var query = PhysicsRayQueryParameters3D.Create(origin, target);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid(), enemy.GetRid() };

        var result = spaceState.IntersectRay(query);
        return result.Count == 0;
    }

    private bool IsEffectEnabled(string effectId)
    {
        return _developerTools?.IsEffectEnabled(this, effectId) ?? true;
    }
}
