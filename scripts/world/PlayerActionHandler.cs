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

    [Signal]
    public delegate void StatusMessageEventHandler(string text);

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

        switch (item.Kind)
        {
            case ItemKind.HealingTonic:
                UseHealingItem(slotIndex, item);
                break;
            case ItemKind.EmberBomb:
                UseEmberBomb(slotIndex, item);
                break;
        }
    }

    private void UseHealingItem(int slotIndex, InventoryItem item)
    {
        if (_player.Hp >= _player.Stats.MaxHp)
        {
            EmitSignal(SignalName.StatusMessage, "Already at full HP.");
            return;
        }

        int oldHp = _player.Hp;
        _player.Hp = Mathf.Min(_player.Hp + item.Power, _player.Stats.MaxHp);
        int healed = _player.Hp - oldHp;
        _player.Stats.Inventory.RemoveAt(slotIndex);
        EmitSignal(SignalName.StatusMessage, $"Used {item.Name}: +{healed} HP");

        if (_turnManager.IsPlayerTurn)
            _combatManager.PlayerUseUtilityItem();
    }

    private void UseEmberBomb(int slotIndex, InventoryItem item)
    {
        if (!_turnManager.IsPlayerTurn || _combatManager.Target == null || !IsInstanceValid(_combatManager.Target))
        {
            EmitSignal(SignalName.StatusMessage, $"{item.Name} needs a combat target.");
            return;
        }

        _player.Stats.Inventory.RemoveAt(slotIndex);
        EmitSignal(SignalName.StatusMessage, $"Used {item.Name} for {item.Power} damage!");
        _combatManager.PlayerUseDamageItem(item.Power);
    }
}
