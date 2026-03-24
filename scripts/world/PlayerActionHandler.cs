using Godot;

namespace ARPG;

/// <summary>
/// Processes player input actions (attack, ability, item use) and translates
/// them into CombatManager/system calls.
/// </summary>
public partial class PlayerActionHandler : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;
    private int _remainingCombatItemUses;

    [Signal]
    public delegate void StatusMessageEventHandler(string text);

    public int RemainingCombatItemUses => _turnManager != null && _turnManager.IsPlayerTurn ? _remainingCombatItemUses : 0;
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
        _turnManager.TurnChanged += OnTurnChanged;
        ResetCombatItemUses();
    }

    public void OnAttackPressed()
    {
        if (_turnManager.State == TurnState.Defeat) return;

        if (_turnManager.IsExploring)
        {
            var enemy = _aggroSystem.FindNearestEnemy(_player.Stats.AttackRange);
            if (enemy == null) return;

            _aggroSystem.ClearAggro();
            EmitSignal(SignalName.StatusMessage, "Combat!");
            _combatManager.EnterCombat(enemy);
        }
        else if (_turnManager.IsPlayerTurn)
        {
            _combatManager.PlayerAttack();
        }
    }

    public void OnAbilityPressed()
    {
        if (_turnManager.State == TurnState.Defeat) return;

        if (_turnManager.IsPlayerTurn)
        {
            _combatManager.PlayerAbility();
        }
    }

    public void OnItemSlotPressed(int slotIndex)
    {
        if (_turnManager.State == TurnState.Defeat || GetTree().Paused)
            return;

        var inventory = _player.Stats.Inventory;
        var item = inventory.GetItem(slotIndex);
        if (item == null)
            return;

        if (_turnManager.InCombat && !_turnManager.IsPlayerTurn)
        {
            EmitSignal(SignalName.StatusMessage, "Can't use items right now.");
            return;
        }

        if (_turnManager.IsPlayerTurn && _remainingCombatItemUses <= 0)
        {
            EmitSignal(SignalName.StatusMessage, "No item uses left this turn.");
            return;
        }

        if (item.RequiresCombatTarget && (!_turnManager.IsPlayerTurn || _combatManager.Target == null || !IsInstanceValid(_combatManager.Target)))
        {
            EmitSignal(SignalName.StatusMessage, $"{item.Name} needs a combat target.");
            return;
        }

        UseItem(slotIndex, item);
    }

    private void UseItem(int slotIndex, InventoryItem item)
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

        bool isCombatItemUse = _turnManager.IsPlayerTurn;
        bool endTurnAfterUse = true;
        if (isCombatItemUse)
            endTurnAfterUse = ConsumeCombatItemUse();

        _player.Stats.Inventory.RemoveAt(slotIndex);
        string turnText = isCombatItemUse && !endTurnAfterUse
            ? $" {_remainingCombatItemUses} item uses left."
            : string.Empty;
        EmitSignal(SignalName.StatusMessage, $"Used {item.Name}: {summary}.{turnText}");

        if (isCombatItemUse)
        {
            if (item.DirectDamage > 0)
                _combatManager.PlayerUseDamageItem(item.DirectDamage, endTurnAfterUse);
            else
                _combatManager.PlayerUseUtilityItem(endTurnAfterUse);
        }
    }

    private void OnTurnChanged(int newState)
    {
        TurnState state = (TurnState)newState;
        if (state == TurnState.PlayerTurn || state == TurnState.Exploring)
            ResetCombatItemUses();
        else if (state == TurnState.Defeat)
            _remainingCombatItemUses = 0;
    }

    private void ResetCombatItemUses()
    {
        _remainingCombatItemUses = CombatItemUsesPerTurn;
    }

    private bool ConsumeCombatItemUse()
    {
        _remainingCombatItemUses = Mathf.Max(0, _remainingCombatItemUses - 1);
        return _remainingCombatItemUses <= 0;
    }

    private static string AppendSummary(string summary, string addition)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return addition;

        return $"{summary}, {addition}";
    }
}
