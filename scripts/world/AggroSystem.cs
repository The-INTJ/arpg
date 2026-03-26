using Godot;

namespace ARPG;

/// <summary>
/// Tracks active-zone enemies for simple real-time combat targeting and alerting.
/// </summary>
public partial class AggroSystem : Node, IDeveloperEffectProvider
{
    public const string EncounterWatchEffectId = "encounter_watch";
    public const string EncounterCommitEffectId = "encounter_commit";

    private PlayerController _player;
    private DeveloperToolsManager _developerTools;

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
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
                enemy.HasAggro = false;
        }
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
                "Zone Awareness",
                "Wake enemies and mark nearby threats when the player enters their room.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro",
                Order: 10));
        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                EncounterCommitEffectId,
                "Zone Alerts",
                "Show the alert indicator when a room becomes hostile.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Encounter,
                OwnerLabel: "Aggro",
                Order: 20));
    }

    public void Tick(float delta)
    {
        if (_player == null || !IsEffectEnabled(EncounterWatchEffectId))
            return;

        Enemy firstSpottedEnemy = null;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || enemy.ZoneRoom != GameState.CurrentRoom || enemy.IsDead)
                continue;

            bool playerInTrackingZone = enemy.IsPlayerInTrackingZone();
            if (!playerInTrackingZone)
            {
                enemy.HasAggro = false;
                continue;
            }

            bool justSpotted = !enemy.HasAggro;
            if (!justSpotted)
                continue;

            if (IsEffectEnabled(EncounterCommitEffectId))
                enemy.ShowAggroIndicator();
            else
                enemy.HasAggro = true;

            firstSpottedEnemy ??= enemy;
        }

        if (firstSpottedEnemy == null)
            return;

        EmitSignal(
            SignalName.AggroSpotted,
            firstSpottedEnemy,
            firstSpottedEnemy.IsBoss ? "Boss spotted." : "Spotted.");
    }

    private bool IsEffectEnabled(string effectId)
    {
        return _developerTools?.IsEffectEnabled(this, effectId) ?? true;
    }
}
