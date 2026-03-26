using Godot;

namespace ARPG;

/// <summary>
/// Processes player input actions (attack, ability, item use) and translates
/// them into real-time combat/system calls.
/// </summary>
public partial class PlayerActionHandler : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;

    [Signal]
    public delegate void StatusMessageEventHandler(string text);

    public int RemainingCombatItemUses => CombatItemUsesPerTurn;
    public int CombatItemUsesPerTurn => _player?.Stats?.ItemUsesPerTurn ?? 1;

    public void Init(
        PlayerController player,
        TurnManager turnManager,
        CombatManager combatManager,
        AggroSystem aggroSystem)
    {
        _player = player;
        _turnManager = turnManager;
        _combatManager = combatManager;
        _aggroSystem = aggroSystem;
    }

    public void OnAttackPressed()
    {
        if (_turnManager.State == TurnState.Defeat)
            return;

        if (!_combatManager.PlayerAttack() && !_combatManager.IsPlayerAttackReady)
            EmitSignal(SignalName.StatusMessage, "Weapon recovering.");
    }

    public void OnAbilityPressed()
    {
        if (_turnManager.State == TurnState.Defeat)
            return;

        var ability = _player.Ability;
        if (ability == null)
            return;

        if (!ability.IsReady)
        {
            EmitSignal(SignalName.StatusMessage, $"{ability.Name} ready in {ability.CooldownRemainingSeconds:0.0}s.");
            return;
        }

        _combatManager.PlayerAbility();
    }

    public void OnItemSlotPressed(int slotIndex)
    {
        if (_turnManager.State == TurnState.Defeat || GetTree().Paused)
            return;

        var inventory = _player.Stats.Inventory;
        var item = inventory.GetItem(slotIndex);
        if (item == null)
            return;

        var target = item.RequiresCombatTarget
            ? _aggroSystem.FindNearestEnemy(_player.Stats.AttackRange)
            : null;
        if (item.RequiresCombatTarget && target == null)
        {
            EmitSignal(SignalName.StatusMessage, $"{item.Name} needs a melee target.");
            return;
        }

        UseItem(slotIndex, item, target);
    }

    private void UseItem(int slotIndex, InventoryItem item, Enemy target)
    {
        bool used = false;
        string summary = string.Empty;

        if (item.HealAmount > 0 && _player.Hp < _player.Stats.MaxHp)
        {
            int oldHp = _player.Hp;
            _player.Hp = Mathf.Min(_player.Hp + item.HealAmount, _player.Stats.MaxHp);
            int healed = _player.Hp - oldHp;

            if (healed > 0)
            {
                summary = AppendSummary(summary, $"+{healed} HP");
                used = true;
            }
        }

        if (item.NegateNextHits > 0)
        {
            _player.Stats.QueueWard(item.NegateNextHits);
            summary = AppendSummary(summary, "next hit blocked");
            used = true;
        }

        if (item.NextAttackBonusDamage > 0)
        {
            _player.Stats.QueueAttackBonusDamage(item.NextAttackBonusDamage);
            summary = AppendSummary(summary, $"+{item.NextAttackBonusDamage} next attack");
            used = true;
        }

        if (item.NextAttackMultiplier > 1.001f)
        {
            summary = AppendSummary(summary, $"x{item.NextAttackMultiplier:0.#} next attack");
            _player.Stats.QueueAttackMultiplier(item.NextAttackMultiplier);
            used = true;
        }

        if (item.DirectDamage > 0)
        {
            summary = AppendSummary(summary, $"{item.DirectDamage} damage");
            used = true;
        }

        if (!used)
        {
            EmitSignal(SignalName.StatusMessage, item.HealAmount > 0
                ? "Already at full HP."
                : $"{item.Name} has no effect right now.");
            return;
        }

        _player.Stats.Inventory.RemoveAt(slotIndex);
        EmitSignal(SignalName.StatusMessage, $"Used {item.Name}: {summary}.");

        if (item.DirectDamage > 0)
            _combatManager.PlayerUseDamageItem(target, item.DirectDamage);
    }

    private static string AppendSummary(string summary, string addition)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return addition;

        return $"{summary}, {addition}";
    }
}
