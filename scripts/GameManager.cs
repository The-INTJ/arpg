using Godot;

namespace ARPG;

public partial class GameManager : Node3D
{
    private PlayerController _player;
    private ExitDoor _exitDoor;
    private Button _attackButton;
    private Label _hpLabel;
    private Label _statsLabel;
    private Label _killLabel;
    private Label _statusLabel;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private Camera3D _camera;

    // Enemy HP bar shown during combat
    private ProgressBar _enemyHpBar;
    private Label _enemyHpLabel;

    private int _killCount;
    private const int KillsToWin = 3;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _exitDoor = GetNode<ExitDoor>("World/ExitDoor");
        _attackButton = GetNode<Button>("CanvasLayer/AttackButton");
        _hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        _statsLabel = GetNode<Label>("CanvasLayer/HUD/StatsLabel");
        _killLabel = GetNode<Label>("CanvasLayer/HUD/KillLabel");
        _statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");
        _camera = _player.GetNode<Camera3D>("CameraRig/Camera3D");

        _turnManager = new TurnManager();
        AddChild(_turnManager);

        _combatManager = new CombatManager();
        AddChild(_combatManager);
        _combatManager.Init(_player, _turnManager, _camera);
        _combatManager.CombatEnded += OnCombatEnded;

        _attackButton.Pressed += OnAttackPressed;
        _attackButton.Visible = false;
        Palette.StyleButton(_attackButton, 18);

        BuildEnemyHpBar();

        // Scale HUD font size relative to viewport height (~3% of height)
        int fontSize = Mathf.Max(18, (int)(GetViewport().GetVisibleRect().Size.Y * 0.03f));
        foreach (var label in new[] { _hpLabel, _statsLabel, _killLabel, _statusLabel })
        {
            label.AddThemeColorOverride("font_color", Palette.TextLight);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        }

        // Floor color
        var floorMesh = GetNode<MeshInstance3D>("World/Floor/FloorMesh");
        floorMesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = Palette.Floor };

        // Generate map and spawn enemies
        var mapGen = GetNode<MapGenerator>("World/MapGenerator");
        var spawnPositions = mapGen.Generate();
        var enemiesContainer = GetNode<Node3D>("World/Enemies");
        foreach (var pos in spawnPositions)
            SpawnEnemy(enemiesContainer, pos);

        UpdateHud();
    }

    private void BuildEnemyHpBar()
    {
        var canvas = GetNode<CanvasLayer>("CanvasLayer");

        var container = new VBoxContainer();
        container.Name = "EnemyHpDisplay";
        container.Visible = false;
        canvas.AddChild(container);

        _enemyHpLabel = new Label();
        _enemyHpLabel.Text = "Enemy";
        _enemyHpLabel.AddThemeColorOverride("font_color", Palette.TextLight);
        _enemyHpLabel.AddThemeFontSizeOverride("font_size", 16);
        _enemyHpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        container.AddChild(_enemyHpLabel);

        _enemyHpBar = new ProgressBar();
        _enemyHpBar.MinValue = 0;
        _enemyHpBar.MaxValue = 1;
        _enemyHpBar.CustomMinimumSize = new Vector2(120, 14);
        _enemyHpBar.ShowPercentage = false;

        var barStyle = new StyleBoxFlat();
        barStyle.BgColor = new Color(0.7f, 0.15f, 0.15f);
        barStyle.SetCornerRadiusAll(4);
        _enemyHpBar.AddThemeStyleboxOverride("fill", barStyle);

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.15f, 0.12f);
        bgStyle.SetCornerRadiusAll(4);
        _enemyHpBar.AddThemeStyleboxOverride("background", bgStyle);

        container.AddChild(_enemyHpBar);
    }

    private void SpawnEnemy(Node3D container, Vector3 position)
    {
        var enemy = new Enemy();
        enemy.Position = position;
        enemy.AddToGroup("enemies");

        // Sprite instead of primitive meshes
        var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateEnemyTexture());
        sprite.Position = new Vector3(0, 0.5f, 0);
        enemy.AddChild(sprite);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(0.6f, 0.9f, 0.6f) };
        shape.Position = new Vector3(0, 0.45f, 0);
        enemy.AddChild(shape);

        container.AddChild(enemy);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameKeys.Attack))
            OnAttackPressed();
    }

    public override void _Process(double delta)
    {
        UpdateHud();

        if (_turnManager.IsExploring)
        {
            _player.TickRegen((float)delta);
            UpdateExploreUI();
        }
        else if (_turnManager.InCombat)
        {
            UpdateCombatUI();
        }
    }

    private void UpdateExploreUI()
    {
        float attackRange = _player.Stats.AttackRange;
        var nearest = FindNearestEnemy(attackRange);
        bool canAttack = nearest != null;

        if (canAttack)
        {
            var worldPos = nearest.GlobalPosition + Vector3.Up * 1.5f;
            if (!_camera.IsPositionBehind(worldPos))
            {
                var screenPos = _camera.UnprojectPosition(worldPos);
                _attackButton.Text = $"Attack ({GameKeys.DisplayName(GameKeys.Attack)})";
                _attackButton.Visible = true;
                _attackButton.Disabled = false;
                _attackButton.Position = screenPos - _attackButton.Size / 2;
            }
            else
            {
                _attackButton.Visible = false;
            }
        }
        else
        {
            _attackButton.Visible = false;
        }

        // Hide enemy HP bar while exploring
        GetNode<VBoxContainer>("CanvasLayer/EnemyHpDisplay").Visible = false;
    }

    private void UpdateCombatUI()
    {
        // Show attack button at screen center-bottom during combat
        if (_turnManager.IsPlayerTurn)
        {
            var viewport = GetViewport().GetVisibleRect().Size;
            _attackButton.Text = $"Attack ({GameKeys.DisplayName(GameKeys.Attack)})";
            _attackButton.Visible = true;
            _attackButton.Disabled = false;
            _attackButton.Position = new Vector2(
                viewport.X / 2 - _attackButton.Size.X / 2,
                viewport.Y * 0.75f
            );
        }
        else
        {
            _attackButton.Visible = false;
        }

        // Show enemy HP bar above the combat target
        var target = _combatManager.Target;
        var display = GetNode<VBoxContainer>("CanvasLayer/EnemyHpDisplay");
        if (target != null && IsInstanceValid(target))
        {
            display.Visible = true;
            _enemyHpBar.Value = target.HpPercent;
            _enemyHpLabel.Text = $"Enemy  {target.Hp}/{target.MaxHp}";

            var worldPos = target.GlobalPosition + Vector3.Up * 1.8f;
            if (!_camera.IsPositionBehind(worldPos))
            {
                var screenPos = _camera.UnprojectPosition(worldPos);
                display.Position = screenPos - display.Size / 2;
            }
        }
        else
        {
            display.Visible = false;
        }
    }

    private void UpdateHud()
    {
        var s = _player.Stats;
        _hpLabel.Text = $"HP: {s.CurrentHp} / {s.MaxHp}";
        _statsLabel.Text = $"ATK: {s.AttackDamage}  |  SPD: {s.MoveSpeed}  |  Range: {s.AttackRange}";
        _killLabel.Text = $"Kills: {_killCount}/{KillsToWin}";
    }

    private Enemy FindNearestEnemy(float range)
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

    private void OnAttackPressed()
    {
        if (_turnManager.State == TurnState.Defeat) return;

        if (_turnManager.IsExploring)
        {
            // Initiate combat with nearest enemy
            var enemy = FindNearestEnemy(_player.Stats.AttackRange);
            if (enemy == null) return;

            _statusLabel.Text = "Combat!";
            _combatManager.EnterCombat(enemy);
        }
        else if (_turnManager.IsPlayerTurn)
        {
            // Already in combat — attack
            _combatManager.PlayerAttack();
        }
    }

    private void OnCombatEnded()
    {
        _killCount++;
        _statusLabel.Text = "Enemy defeated!";
        _attackButton.Visible = false;

        if (_killCount >= KillsToWin)
        {
            _statusLabel.Text = "Exit unlocked! Find the exit!";
            _exitDoor.Unlock();
        }

        if (_player.Hp <= 0)
        {
            _turnManager.SetState(TurnState.Defeat);
            _statusLabel.Text = "Defeated!";
            _attackButton.Visible = false;
            _player.SetPhysicsProcess(false);
        }
    }
}
