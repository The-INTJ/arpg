using Godot;

namespace ARPG;

/// <summary>
/// Detects enemies in range and manages the aggro delay timer before combat starts.
/// </summary>
public partial class AggroSystem : Node
{
    private PlayerController _player;
    private Enemy _aggroEnemy;
    private float _aggroTimer;
    private const float AggroDelay = 0.6f;

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

    public void Tick(float delta)
    {
        if (_aggroEnemy != null)
        {
            if (!IsInstanceValid(_aggroEnemy))
            {
                _aggroEnemy = null;
                return;
            }

            _aggroTimer -= delta;
            if (_aggroTimer <= 0)
            {
                var enemy = _aggroEnemy;
                _aggroEnemy = null;
                EmitSignal(SignalName.AggroTriggered, enemy);
            }
            return;
        }

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && !enemy.HasAggro)
            {
                float dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist <= enemy.SightRange)
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
                float dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist <= range && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }
}
