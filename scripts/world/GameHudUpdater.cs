using System.Collections.Generic;
using Godot;

namespace ARPG;

/// <summary>
/// Per-frame HUD state updates: labels, item bar, enemy HP display, and button positioning.
/// </summary>
public partial class GameHudUpdater : Node
{
    private PlayerController _player;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;
    private PlayerActionHandler _actionHandler;
    private Camera3D _camera;
    private CanvasLayer _canvas;

    private Label _hpLabel;
    private Label _statsLabel;
    private Label _darkEnergyLabel;
    private ProgressBar _darkEnergyBar;
    private Label _statusLabel;
    private Button _attackButton;
    private Button _abilityButton;
    private ProgressBar _enemyHpBar;
    private Label _enemyHpLabel;
    private Label _enemyEffectInfoLabel;
    private VBoxContainer _enemyHpDisplay;

    private HBoxContainer _itemBarHBox;
    private readonly List<GameHudBuilder.ItemSlotEntry> _itemSlots = new();

    public string StatusText
    {
        get => _statusLabel.Text;
        set => _statusLabel.Text = value;
    }

    public Button AttackButton => _attackButton;
    public Button AbilityButton => _abilityButton;

    public void Init(
        PlayerController player,
        TurnManager turnManager,
        CombatManager combatManager,
        AggroSystem aggroSystem,
        PlayerActionHandler actionHandler,
        Camera3D camera,
        CanvasLayer canvas,
        Label hpLabel,
        Label statsLabel,
        Label darkEnergyLabel,
        ProgressBar darkEnergyBar,
        Label statusLabel,
        Button attackButton,
        Button abilityButton,
        GameHudBuilder.EnemyHpDisplay enemyHp,
        HBoxContainer itemBarHBox)
    {
        _player = player;
        _turnManager = turnManager;
        _combatManager = combatManager;
        _aggroSystem = aggroSystem;
        _actionHandler = actionHandler;
        _camera = camera;
        _canvas = canvas;
        _hpLabel = hpLabel;
        _statsLabel = statsLabel;
        _darkEnergyLabel = darkEnergyLabel;
        _darkEnergyBar = darkEnergyBar;
        _statusLabel = statusLabel;
        _attackButton = attackButton;
        _abilityButton = abilityButton;
        _enemyHpBar = enemyHp.Bar;
        _enemyHpLabel = enemyHp.HpLabel;
        _enemyEffectInfoLabel = enemyHp.EffectInfoLabel;
        _enemyHpDisplay = enemyHp.Container;
        _itemBarHBox = itemBarHBox;
    }

    public void UpdateAll(DarkEnergy darkEnergy)
    {
        UpdateHud(darkEnergy);
        SyncItemSlotCount();
        UpdateItemBar();

        if (_turnManager.State == TurnState.Defeat)
        {
            HideAllCombatUI();
            return;
        }

        UpdateRealtimeCombatUI();
    }

    public void HideAllCombatUI()
    {
        _attackButton.Visible = false;
        _abilityButton.Visible = false;
        _enemyHpDisplay.Visible = false;
    }

    private void UpdateHud(DarkEnergy darkEnergy)
    {
        var s = _player.Stats;
        _hpLabel.Text = $"HP: {s.CurrentHp} / {s.MaxHp}";
        string inventoryText = $"{s.Inventory.OccupiedSlotCount}/{s.Inventory.Capacity}";
        _statsLabel.Text = $"ATK: {s.AttackDamage}  |  SPD: {s.MoveSpeed:0.#}  |  Range: {s.AttackRange:0.##}  |  Bag: {inventoryText}";
        _darkEnergyLabel.Text = $"Dark Energy: {darkEnergy.Current}/{darkEnergy.Threshold}";
        _darkEnergyBar.Value = darkEnergy.FillPercent;
    }

    /// <summary>
    /// Adds or removes ItemSlot scene instances to match the player's inventory capacity.
    /// </summary>
    private void SyncItemSlotCount()
    {
        int desired = _player.Stats.Inventory.Capacity;
        if (desired == _itemSlots.Count)
            return;

        while (_itemSlots.Count < desired)
        {
            var entry = GameHudBuilder.CreateItemSlot();
            entry.Panel.GuiInput += @event => OnItemSlotGuiInput(entry.Panel, @event);
            _itemBarHBox.AddChild(entry.Panel);
            _itemSlots.Add(entry);
        }

        while (_itemSlots.Count > desired)
        {
            int last = _itemSlots.Count - 1;
            var entry = _itemSlots[last];
            _itemBarHBox.RemoveChild(entry.Panel);
            entry.Panel.QueueFree();
            _itemSlots.RemoveAt(last);
        }
    }

    private void UpdateItemBar()
    {
        var inventory = _player.Stats.Inventory;
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            string keyName = GameKeys.ItemSlotLabel(i);
            var item = inventory.GetItem(i);

            if (item == null)
            {
                slot.Icon.Texture = null;
                slot.Icon.Visible = false;
                slot.Label.Text = $"{keyName}\n(empty)";
                slot.Label.AddThemeColorOverride("font_color", Palette.TextDisabled);
                slot.Style.BgColor = new Color(Palette.BgDark, 0.9f);
                slot.Style.BorderColor = new Color(Palette.TextDisabled, 0.85f);
                GameHudBuilder.RefreshItemSlotSize(slot);
                continue;
            }

            slot.Icon.Texture = SpriteFactory.CreateItemTexture(item.VisualId);
            slot.Icon.Visible = true;
            slot.Label.Text = $"{keyName}  {item.Name}\n{item.Description}";
            slot.Label.AddThemeColorOverride("font_color", Palette.TextLight);
            slot.Style.BgColor = new Color(Palette.ButtonBg, 0.92f);
            slot.Style.BorderColor = item.DisplayColor;
            GameHudBuilder.RefreshItemSlotSize(slot);
        }

        _itemBarHBox.QueueSort();
    }

    private void OnItemSlotGuiInput(PanelContainer panel, InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton)
            return;

        if (mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed)
            return;

        int slotIndex = FindSlotIndex(panel);
        if (slotIndex < 0)
            return;

        _actionHandler.OnItemSlotPressed(slotIndex);
        GetViewport().SetInputAsHandled();
    }

    private int FindSlotIndex(PanelContainer panel)
    {
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            if (_itemSlots[i].Panel == panel)
                return i;
        }

        return -1;
    }

    private void UpdateRealtimeCombatUI()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        _attackButton.Visible = true;
        _attackButton.Disabled = !_combatManager.IsPlayerAttackReady;
        _attackButton.Text = _combatManager.IsPlayerAttackReady
            ? $"Attack ({GameKeys.DisplayName(GameKeys.Attack)})"
            : $"Attack ({_combatManager.PlayerAttackCooldownRemaining:0.0}s)";
        _attackButton.Position = new Vector2(
            viewport.X * 0.5f - _attackButton.Size.X - 14.0f,
            viewport.Y * 0.78f);

        var ability = _player.Ability;
        if (ability != null)
        {
            _abilityButton.Visible = true;
            _abilityButton.Disabled = !ability.IsReady;
            _abilityButton.Text = ability.IsReady
                ? $"{ability.Name} ({GameKeys.DisplayName(GameKeys.Ability)})"
                : $"{ability.Name} ({ability.CooldownRemainingSeconds:0.0}s)";
            _abilityButton.Position = new Vector2(
                viewport.X * 0.5f + 14.0f,
                viewport.Y * 0.78f);
        }
        else
        {
            _abilityButton.Visible = false;
        }

        var target = _combatManager.Target ?? _aggroSystem.FindNearestEnemy(_player.Stats.AttackRange);
        if (target == null)
        {
            _enemyHpDisplay.Visible = false;
            return;
        }

        var worldPos = target.GlobalPosition + Vector3.Up * 0.9f;
        if (_camera.IsPositionBehind(worldPos))
        {
            _enemyHpDisplay.Visible = false;
            return;
        }

        UpdateEnemyDisplay(target, worldPos);
    }

    private void UpdateEnemyDisplay(Enemy enemy, Vector3 worldPos)
    {
        if (enemy == null || !IsInstanceValid(enemy) || _camera.IsPositionBehind(worldPos))
        {
            _enemyHpDisplay.Visible = false;
            return;
        }

        _enemyHpDisplay.Visible = true;
        _enemyHpBar.Value = enemy.HpPercent;
        _enemyHpLabel.Text = $"{enemy.DisplayName}  {enemy.Hp}/{enemy.MaxHp}";
        _enemyEffectInfoLabel.Text = enemy.GetEffectInfoText();

        var screenPos = _camera.UnprojectPosition(worldPos);
        _enemyHpDisplay.Position = screenPos - _enemyHpDisplay.Size / 2;
    }
}
