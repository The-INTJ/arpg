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

    private int _killCount;
    private const int KillsToWin = 3;
    private const float AttackRange = 3.0f;
    private const float EnemyTurnDelay = 0.6f;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _exitDoor = GetNode<ExitDoor>("World/ExitDoor");
        _attackButton = GetNode<Button>("CanvasLayer/HUD/AttackButton");
        _hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        _killLabel = GetNode<Label>("CanvasLayer/HUD/KillLabel");
        _statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");

        _turnManager = new TurnManager();
        AddChild(_turnManager);

        _enemyTurnTimer = new Timer();
        _enemyTurnTimer.OneShot = true;
        _enemyTurnTimer.WaitTime = EnemyTurnDelay;
        _enemyTurnTimer.Timeout += OnEnemyTurnExecute;
        AddChild(_enemyTurnTimer);

        _attackButton.Pressed += OnAttackPressed;

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
        // Use `new Enemy()` directly so the C# type is correct
        var enemy = new Enemy();
        enemy.Position = position;
        enemy.AddToGroup("enemies");

        // Body: red sphere on top of dark red box (little golem look)
        var bodyMesh = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(0.8f, 0.8f, 0.8f);
        var bodyMat = new StandardMaterial3D();
        bodyMat.AlbedoColor = new Color(0.7f, 0.1f, 0.1f);
        box.Material = bodyMat;
        bodyMesh.Mesh = box;
        bodyMesh.Position = new Vector3(0, 0, 0);
        enemy.AddChild(bodyMesh);

        // Head: bright red sphere
        var headMesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.35f;
        sphere.Height = 0.7f;
        var headMat = new StandardMaterial3D();
        headMat.AlbedoColor = new Color(1.0f, 0.2f, 0.2f);
        headMat.Emission = new Color(0.4f, 0.0f, 0.0f);
        headMat.EmissionEnabled = true;
        sphere.Material = headMat;
        headMesh.Mesh = sphere;
        headMesh.Position = new Vector3(0, 0.65f, 0);
        enemy.AddChild(headMesh);

        var shape = new CollisionShape3D();
        var boxShape = new BoxShape3D();
        boxShape.Size = new Vector3(0.8f, 1.4f, 0.8f);
        shape.Shape = boxShape;
        shape.Position = new Vector3(0, 0.3f, 0);
        enemy.AddChild(shape);

        container.AddChild(enemy);
    }

    public override void _Process(double delta)
    {
        UpdateHud();
        var canAttack = _turnManager.IsPlayerTurn && FindNearestEnemy() != null;
        _attackButton.Disabled = !canAttack;
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

        // Check if enemy died
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

        // Start enemy turn
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
            _attackButton.Disabled = true;
            _player.SetPhysicsProcess(false);
            return;
        }

        _turnManager.SetState(TurnState.PlayerTurn);
    }
}
