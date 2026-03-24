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
    private Label _killLabel;
    private Label _statusLabel;
    private Button _attackButton;
    private Button _abilityButton;
    private ProgressBar _enemyHpBar;
    private Label _enemyHpLabel;
    private Label _enemyEffectInfoLabel;
    private VBoxContainer _enemyHpDisplay;
    private Control[] _itemSlotControls;
    private TextureRect[] _itemSlotIcons;
    private Label[] _itemSlotLabels;
    private StyleBoxFlat[] _itemSlotStyles;

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
        Label killLabel,
        Label statusLabel,
        Button attackButton,
        Button abilityButton,
        GameHudBuilder.EnemyHpDisplay enemyHp,
        Control[] itemSlotControls,
        TextureRect[] itemSlotIcons,
        Label[] itemSlotLabels,
        StyleBoxFlat[] itemSlotStyles)
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
        _killLabel = killLabel;
        _statusLabel = statusLabel;
        _attackButton = attackButton;
        _abilityButton = abilityButton;
        _enemyHpBar = enemyHp.Bar;
        _enemyHpLabel = enemyHp.HpLabel;
        _enemyEffectInfoLabel = enemyHp.EffectInfoLabel;
        _enemyHpDisplay = enemyHp.Container;
        _itemSlotControls = itemSlotControls;
        _itemSlotIcons = itemSlotIcons;
        _itemSlotLabels = itemSlotLabels;
        _itemSlotStyles = itemSlotStyles;
    }

    public void UpdateAll(int killCount, int totalEnemies)
    {
        UpdateHud(killCount, totalEnemies);
        UpdateItemBar();

        if (_turnManager.IsExploring)
            UpdateExploreUI();
        else if (_turnManager.InCombat)
            UpdateCombatUI();
    }

    public void HideAllCombatUI()
    {
        _attackButton.Visible = false;
        _abilityButton.Visible = false;
        _enemyHpDisplay.Visible = false;
    }

    private void UpdateHud(int killCount, int totalEnemies)
    {
        var s = _player.Stats;
        _hpLabel.Text = $"HP: {s.CurrentHp} / {s.MaxHp}";
        string itemUsesText = _turnManager.IsPlayerTurn
            ? $"{_actionHandler.RemainingCombatItemUses}/{_actionHandler.CombatItemUsesPerTurn}"
            : $"{s.ItemUsesPerTurn}/turn";
        _statsLabel.Text = $"ATK: {s.AttackDamage}  |  SPD: {s.MoveSpeed:0.#}  |  Range: {s.AttackRange:0.#}  |  Items: {itemUsesText}";
        _killLabel.Text = $"Kills: {killCount}/{totalEnemies}";
    }

    private void UpdateItemBar()
    {
        var inventory = _player.Stats.Inventory;
        for (int i = 0; i < _itemSlotControls.Length; i++)
        {
            bool slotUnlocked = i < inventory.Capacity;
            _itemSlotControls[i].Visible = slotUnlocked;
            if (!slotUnlocked)
                continue;

            string keyName = GameKeys.DisplayName(GameKeys.ItemSlot(i));
            var item = inventory.GetItem(i);

            if (item == null)
            {
                _itemSlotIcons[i].Texture = null;
                _itemSlotIcons[i].Visible = false;
                _itemSlotLabels[i].Text = $"{keyName}\n(empty)";
                _itemSlotLabels[i].AddThemeColorOverride("font_color", Palette.TextDisabled);
                _itemSlotStyles[i].BgColor = new Color(Palette.BgDark, 0.9f);
                _itemSlotStyles[i].BorderColor = new Color(Palette.TextDisabled, 0.85f);
                continue;
            }

            _itemSlotIcons[i].Texture = SpriteFactory.CreateItemTexture(item.VisualId);
            _itemSlotIcons[i].Visible = true;
            _itemSlotLabels[i].Text = $"{keyName}  {item.Name}\n{item.Description}";
            _itemSlotLabels[i].AddThemeColorOverride("font_color", Palette.TextLight);
            _itemSlotStyles[i].BgColor = new Color(Palette.ButtonBg, 0.92f);
            _itemSlotStyles[i].BorderColor = item.DisplayColor;
        }
    }

    private void UpdateExploreUI()
    {
        float attackRange = _player.Stats.AttackRange;
        var nearest = _aggroSystem.FindNearestEnemy(attackRange);
        bool canAttack = nearest != null;

        if (canAttack)
        {
            var worldPos = nearest.GlobalPosition + Vector3.Up * 0.8f;
            if (!_camera.IsPositionBehind(worldPos))
            {
                var screenPos = _camera.UnprojectPosition(worldPos);
                _attackButton.Text = $"Attack ({GameKeys.DisplayName(GameKeys.Attack)})";
                _attackButton.Visible = true;
                _attackButton.Disabled = false;
                _attackButton.Position = screenPos - _attackButton.Size / 2;
                UpdateEnemyDisplay(nearest, nearest.GlobalPosition + Vector3.Up * 0.9f);
            }
            else
            {
                _attackButton.Visible = false;
                _enemyHpDisplay.Visible = false;
            }
        }
        else
        {
            _attackButton.Visible = false;
            _enemyHpDisplay.Visible = false;
        }

        _abilityButton.Visible = false;
    }

    private void UpdateCombatUI()
    {
        var viewport = GetViewport().GetVisibleRect().Size;

        if (_turnManager.IsPlayerTurn)
        {
            _attackButton.Text = $"Attack ({GameKeys.DisplayName(GameKeys.Attack)})";
            _attackButton.Visible = true;
            _attackButton.Disabled = false;
            _attackButton.Position = new Vector2(
                viewport.X / 2 - _attackButton.Size.X / 2 - 100,
                viewport.Y * 0.75f
            );

            var ability = _player.Ability;
            if (ability != null)
            {
                _abilityButton.Visible = true;
                if (ability.IsReady)
                {
                    _abilityButton.Text = $"{ability.Name} ({GameKeys.DisplayName(GameKeys.Ability)})";
                    _abilityButton.Disabled = false;
                }
                else
                {
                    _abilityButton.Text = $"{ability.Name} ({ability.TurnsRemaining})";
                    _abilityButton.Disabled = true;
                }
                _abilityButton.Position = new Vector2(
                    viewport.X / 2 - _abilityButton.Size.X / 2 + 100,
                    viewport.Y * 0.75f
                );
            }
        }
        else
        {
            _attackButton.Visible = false;
            _abilityButton.Visible = false;
        }

        var target = _combatManager.Target;
        if (target != null && IsInstanceValid(target))
            UpdateEnemyDisplay(target, target.GlobalPosition + Vector3.Up * 0.9f);
        else
            _enemyHpDisplay.Visible = false;
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
