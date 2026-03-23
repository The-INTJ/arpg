using Godot;

namespace ARPG;

public partial class GameManager : Node3D
{
    private PlayerController _player;
    private ExitDoor _exitDoor;
    private Button _attackButton;
    private Label _hpLabel;
    private Label _killLabel;
    private Label _statusLabel;
    private TurnManager _turnManager;
    private Timer _enemyTurnTimer;
    private Camera3D _camera;

    private int _killCount;
    private const int KillsToWin = 3;
    private const float AttackRange = 3.5f;
    private const float EnemyTurnDelay = 0.6f;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _exitDoor = GetNode<ExitDoor>("World/ExitDoor");
        _attackButton = GetNode<Button>("CanvasLayer/AttackButton");
        _hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        _killLabel = GetNode<Label>("CanvasLayer/HUD/KillLabel");
        _statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");
        _camera = _player.GetNode<Camera3D>("CameraRig/Camera3D");

        _turnManager = new TurnManager();
        AddChild(_turnManager);

        _enemyTurnTimer = new Timer();
        _enemyTurnTimer.OneShot = true;
        _enemyTurnTimer.WaitTime = EnemyTurnDelay;
        _enemyTurnTimer.Timeout += OnEnemyTurnExecute;
        AddChild(_enemyTurnTimer);

        _attackButton.Pressed += OnAttackPressed;
        _attackButton.Visible = false;
        Palette.StyleButton(_attackButton, 18);

        // Style HUD labels
        foreach (var label in new[] { _hpLabel, _killLabel, _statusLabel })
        {
            label.AddThemeColorOverride("font_color", Palette.TextLight);
            label.AddThemeFontSizeOverride("font_size", 22);
            label.AddThemeConstantOverride("shadow_offset_x", 2);
            label.AddThemeConstantOverride("shadow_offset_y", 2);
            label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        }

        // Floor color
        var floorMesh = GetNode<MeshInstance3D>("World/Floor/FloorMesh");
        var floorMat = new StandardMaterial3D { AlbedoColor = Palette.Floor };
        floorMesh.MaterialOverride = floorMat;

        // Generate map and spawn enemies
        var mapGen = GetNode<MapGenerator>("World/MapGenerator");
        var spawnPositions = mapGen.Generate();
        var enemiesContainer = GetNode<Node3D>("World/Enemies");
        foreach (var pos in spawnPositions)
            SpawnEnemy(enemiesContainer, pos);

        UpdateHud();
    }

    private void SpawnEnemy(Node3D container, Vector3 position)
    {
        var enemy = new Enemy();
        enemy.Position = position;
        enemy.AddToGroup("enemies");

        var bodyMesh = new MeshInstance3D();
        var box = new BoxMesh { Size = new Vector3(0.6f, 0.6f, 0.6f) };
        box.Material = new StandardMaterial3D { AlbedoColor = Palette.EnemyBody };
        bodyMesh.Mesh = box;
        bodyMesh.Position = new Vector3(0, 0.3f, 0);
        enemy.AddChild(bodyMesh);

        var headMesh = new MeshInstance3D();
        var sphere = new SphereMesh { Radius = 0.25f, Height = 0.5f };
        sphere.Material = new StandardMaterial3D
        {
            AlbedoColor = Palette.EnemyHead,
            EmissionEnabled = true,
            Emission = Palette.EnemyGlow,
        };
        headMesh.Mesh = sphere;
        headMesh.Position = new Vector3(0, 0.75f, 0);
        enemy.AddChild(headMesh);

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(0.6f, 0.9f, 0.6f) };
        shape.Position = new Vector3(0, 0.45f, 0);
        enemy.AddChild(shape);

        container.AddChild(enemy);
    }

    public override void _Process(double delta)
    {
        UpdateHud();
        var nearest = FindNearestEnemy();
        bool canAttack = _turnManager.IsPlayerTurn && nearest != null;

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
    }

    private void UpdateHud()
    {
        _hpLabel.Text = $"HP: {_player.Hp}";
        _killLabel.Text = $"Kills: {_killCount}/{KillsToWin}";
    }

    private Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
            {
                float dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist <= AttackRange && dist < nearestDist)
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
        if (!_turnManager.IsPlayerTurn) return;

        var enemy = FindNearestEnemy();
        if (enemy == null) return;

        enemy.TakeDamage(_player.AttackDamage);
        _statusLabel.Text = $"You hit the enemy for {_player.AttackDamage}!";

        if (!IsInstanceValid(enemy) || enemy.Hp <= 0)
        {
            _killCount++;
            _statusLabel.Text += " Enemy defeated!";
            if (_killCount >= KillsToWin)
            {
                _statusLabel.Text = "Exit unlocked! Find the exit!";
                _exitDoor.Unlock();
            }
        }

        _turnManager.SetState(TurnState.EnemyTurn);
        _enemyTurnTimer.Start();
    }

    private void OnEnemyTurnExecute()
    {
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy)
            {
                float dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist <= AttackRange)
                {
                    enemy.AttackPlayer(_player);
                    _statusLabel.Text = $"Enemy hits you for {enemy.AttackDamage}!";
                }
            }
        }

        if (_player.Hp <= 0)
        {
            _turnManager.SetState(TurnState.Defeat);
            _statusLabel.Text = "Defeated!";
            _attackButton.Visible = false;
            _player.SetPhysicsProcess(false);
            return;
        }

        _turnManager.SetState(TurnState.PlayerTurn);
    }
}
