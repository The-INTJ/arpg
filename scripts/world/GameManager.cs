using Godot;

namespace ARPG;

public partial class GameManager : Node3D
{
    private PlayerController _player;
    private ExitDoor _exitDoor;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;
    private GameHudUpdater _hudUpdater;
    private PlayerActionHandler _actionHandler;
    private ModifyStatsSimple _modifyScreen;
    private PauseScreen _pauseScreen;

    private int _killCount;
    private int _totalEnemies;
    private bool _isEndingRun;

    private int _room => GameState.CurrentRoom;
    private bool _isBossRoom => _room == GameState.BossRoom;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _exitDoor = GetNode<ExitDoor>("World/ExitDoor");
        var camera = _player.GetNode<Camera3D>("CameraRig/Camera3D");
        var canvas = GetNode<CanvasLayer>("CanvasLayer");
        var monsterEffectProfile = MonsterEffectRoomProfiles.ForRoom(_room);

        // Turn and combat systems
        _turnManager = new TurnManager();
        AddChild(_turnManager);
        _turnManager.TurnChanged += OnTurnChanged;

        _combatManager = new CombatManager();
        AddChild(_combatManager);
        _combatManager.Init(_player, _turnManager, camera);
        _combatManager.CombatEnded += OnCombatEnded;
        _combatManager.CombatFeedback += text => _hudUpdater.StatusText = text;

        // Aggro system
        _aggroSystem = new AggroSystem();
        AddChild(_aggroSystem);
        _aggroSystem.Init(_player);
        _aggroSystem.AggroTriggered += enemy =>
        {
            _hudUpdater.StatusText = "Combat!";
            _combatManager.EnterCombat(enemy);
        };
        _aggroSystem.AggroSpotted += (enemy, message) =>
        {
            _hudUpdater.StatusText = message;
        };

        // Action handler
        _actionHandler = new PlayerActionHandler();
        AddChild(_actionHandler);
        _actionHandler.Init(_player, _turnManager, _combatManager, _aggroSystem);
        _actionHandler.StatusMessage += text => _hudUpdater.StatusText = text;

        // HUD elements from scene
        var attackButton = GetNode<Button>("CanvasLayer/AttackButton");
        attackButton.Pressed += _actionHandler.OnAttackPressed;
        attackButton.Visible = false;
        Palette.StyleButton(attackButton, 18);

        var abilityButton = GetNode<Button>("CanvasLayer/AbilityButton");
        Palette.StyleButton(abilityButton, 18);
        abilityButton.Pressed += _actionHandler.OnAbilityPressed;

        var enemyHp = new GameHudBuilder.EnemyHpDisplay(
            GetNode<ProgressBar>("CanvasLayer/EnemyHpDisplay/HpBar"),
            GetNode<Label>("CanvasLayer/EnemyHpDisplay/HpLabel"),
            GetNode<Label>("CanvasLayer/EnemyHpDisplay/EffectInfoLabel"),
            GetNode<VBoxContainer>("CanvasLayer/EnemyHpDisplay"));

        var roomLabel = GetNode<Label>("CanvasLayer/RoomLabel");
        roomLabel.Text = $"Room {_room}/{GameState.TotalRooms}";
        var ruleLabel = GetNode<Label>("CanvasLayer/RuleLabel");
        ruleLabel.Text = $"{monsterEffectProfile.DisplayName}\n{monsterEffectProfile.Description}";

        var hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        var statsLabel = GetNode<Label>("CanvasLayer/HUD/StatsLabel");
        var killLabel = GetNode<Label>("CanvasLayer/HUD/KillLabel");
        var statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");
        GameHudBuilder.StyleHudLabels(
            new[] { hpLabel, statsLabel, killLabel, statusLabel },
            GetViewport().GetVisibleRect().Size.Y);

        var itemBarHBox = GetNode<HBoxContainer>("CanvasLayer/ItemBarCenter/ItemBarHBox");
        var (itemSlots, itemLabels, itemStyles) = GameHudBuilder.BindItemBar(itemBarHBox);

        // HUD updater
        _hudUpdater = new GameHudUpdater();
        AddChild(_hudUpdater);
        _hudUpdater.Init(_player, _turnManager, _combatManager, _aggroSystem, camera, canvas,
            hpLabel, statsLabel, killLabel, statusLabel, attackButton, abilityButton,
            enemyHp, itemSlots, itemLabels, itemStyles);

        // Screens
        _modifyScreen = GetNode<ModifyStatsSimple>("CanvasLayer/ModifyStatsSimple");
        _pauseScreen = GetNode<PauseScreen>("CanvasLayer/PauseScreen");
        _pauseScreen.ViewStatsRequested += () => _modifyScreen.Open(_player.Stats);

        // Textured ground
        var floorMesh = GetNode<MeshInstance3D>("World/Floor/FloorMesh");
        floorMesh.MaterialOverride = MapGenerator.CreateGroundMaterial();

        SetupEnvironment();
        SetupAudioManager();

        // Generate map and spawn enemies
        var mapGen = GetNode<MapGenerator>("World/MapGenerator");
        var spawnPositions = mapGen.Generate();
        var enemiesContainer = GetNode<Node3D>("World/Enemies");
        var encounter = EnemySpawner.BuildEncounter(_room);

        for (int i = 0; i < encounter.Length && i < spawnPositions.Length; i++)
            EnemySpawner.Spawn(enemiesContainer, spawnPositions[i], encounter[i], _room, monsterEffectProfile);

        if (encounter.Length < spawnPositions.Length)
            SpawnMapItem(spawnPositions[encounter.Length]);

        _totalEnemies = encounter.Length;
        _hudUpdater.StatusText = GetRoomIntroText();

        AudioManager.Instance?.StartExploreMusic();
    }

    private void SetupEnvironment()
    {
        var env = new Godot.Environment();
        env.BackgroundMode = Godot.Environment.BGMode.Sky;

        var sky = new Sky();
        var skyMat = new ProceduralSkyMaterial();
        skyMat.SkyTopColor = new Color(0.35f, 0.55f, 0.82f);
        skyMat.SkyHorizonColor = new Color(0.65f, 0.75f, 0.85f);
        skyMat.GroundBottomColor = new Color(0.35f, 0.30f, 0.22f);
        skyMat.GroundHorizonColor = new Color(0.55f, 0.50f, 0.40f);
        skyMat.SunAngleMax = 30.0f;
        sky.SkyMaterial = skyMat;
        env.Sky = sky;

        env.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        env.AmbientLightEnergy = 0.4f;
        env.AmbientLightColor = new Color(0.8f, 0.85f, 0.95f);

        env.TonemapMode = Godot.Environment.ToneMapper.Filmic;
        env.SsaoEnabled = false;

        env.FogEnabled = true;
        env.FogLightColor = new Color(0.65f, 0.60f, 0.50f);
        env.FogDensity = 0.003f;

        var worldEnv = new WorldEnvironment();
        worldEnv.Environment = env;
        GetNode<Node3D>("World").AddChild(worldEnv);

        var sun = GetNode<DirectionalLight3D>("World/DirectionalLight3D");
        sun.ShadowEnabled = true;
        sun.LightColor = new Color(1.0f, 0.95f, 0.85f);
        sun.LightEnergy = 1.2f;
        sun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel2Splits;
    }

    private void SetupAudioManager()
    {
        if (AudioManager.Instance != null) return;

        var audio = new AudioManager();
        audio.Name = "AudioManager";
        GetTree().Root.AddChild(audio);
    }

    private string GetRoomIntroText()
    {
        if (_isBossRoom)
            return $"Room {_room}/{GameState.TotalRooms} — Boss ahead! Defeat all enemies!";
        return $"Room {_room}/{GameState.TotalRooms} — Clear all enemies to proceed";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameKeys.Attack))
            _actionHandler.OnAttackPressed();
        else if (@event.IsActionPressed(GameKeys.Ability))
            _actionHandler.OnAbilityPressed();

        for (int i = 0; i < _player.Stats.Inventory.Capacity && i < GameKeys.ItemSlots.Length; i++)
        {
            if (!@event.IsActionPressed(GameKeys.ItemSlot(i)))
                continue;

            _actionHandler.OnItemSlotPressed(i);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public override void _Process(double delta)
    {
        _hudUpdater.UpdateAll(_killCount, _totalEnemies);

        if (_turnManager.IsExploring)
        {
            _player.TickRegen((float)delta);
            _aggroSystem.Tick((float)delta);
        }
    }

    // --- Combat callbacks ---

    private void OnCombatEnded()
    {
        _killCount++;
        bool wasBoss = _combatManager.LastKillWasBoss;
        GameState.RecordKill(wasBoss);
        _hudUpdater.AttackButton.Visible = false;
        _hudUpdater.AbilityButton.Visible = false;

        if (wasBoss)
            AudioManager.Instance?.PlayBossDeath();
        else
            AudioManager.Instance?.PlayEnemyDeath();

        AudioManager.Instance?.StopCombatMusic();
        SpawnLoot(_combatManager.LastKillPosition);
        var droppedItem = _combatManager.LastKillItemDrop;
        if (droppedItem != null)
            SpawnInventoryItem(_combatManager.LastKillPosition + new Vector3(0.75f, 0, 0.4f), droppedItem);

        if (_killCount >= _totalEnemies)
        {
            _exitDoor.Unlock();
            AudioManager.Instance?.PlayLevelUp();

            string clearText = _room >= GameState.TotalRooms
                ? "All rooms cleared! Find the exit!"
                : "Room cleared! Find the exit to proceed!";
            _hudUpdater.StatusText = droppedItem == null
                ? clearText
                : $"{clearText} {droppedItem.Name} dropped nearby.";
        }
        else
        {
            int remaining = _totalEnemies - _killCount;
            _hudUpdater.StatusText = droppedItem == null
                ? $"Enemy defeated! {remaining} remaining"
                : $"Enemy defeated! {remaining} remaining. {droppedItem.Name} dropped nearby.";
        }

        if (_player.Hp <= 0)
            _turnManager.SetState(TurnState.Defeat);
    }

    private void OnTurnChanged(int newState)
    {
        if ((TurnState)newState != TurnState.Defeat || _isEndingRun)
            return;

        _isEndingRun = true;
        _hudUpdater.StatusText = "Defeated!";
        _hudUpdater.HideAllCombatUI();
        _player.SetPhysicsProcess(false);
        _pauseScreen.Visible = false;
        _modifyScreen.Visible = false;
        GetTree().Paused = false;

        CallDeferred(nameof(ShowGameOverScreen));
    }

    private void ShowGameOverScreen()
    {
        GameState.FinalizeCurrentRun(RunOutcome.Defeat, _player?.Stats);
        GetTree().ChangeSceneToFile(Scenes.GameOverScreen);
    }

    // --- Items and loot ---

    private void SpawnMapItem(Vector3 position)
    {
        SpawnInventoryItem(position, InventoryItem.CreateForRoom(_room));
    }

    private void SpawnInventoryItem(Vector3 position, InventoryItem item)
    {
        if (item == null)
            return;

        var pickup = GD.Load<PackedScene>(Scenes.ItemPickup).Instantiate<ItemPickup>();
        pickup.Position = position;
        pickup.Init(item);
        pickup.Collected += (itemName, slotIndex) =>
        {
            string keyName = GameKeys.DisplayName(GameKeys.ItemSlot(slotIndex));
            _hudUpdater.StatusText = $"Picked up {itemName} ({keyName})";
            AudioManager.Instance?.PlayPickup();
        };
        pickup.InventoryFull += itemName =>
        {
            _hudUpdater.StatusText = $"Inventory full for {itemName}.";
        };
        GetNode<Node3D>("World").AddChild(pickup);
    }

    private void SpawnLoot(Vector3 position)
    {
        var pickup = GD.Load<PackedScene>(Scenes.LootPickup).Instantiate<LootPickup>();
        pickup.Position = position;
        pickup.Init(ModifierGenerator.Random());
        pickup.EquipRequested += () =>
        {
            _modifyScreen.Open(_player.Stats, pickup.Modifier);
            AudioManager.Instance?.PlayPickup();
        };
        pickup.Stashed += description =>
        {
            _hudUpdater.StatusText = $"Stashed: {description}";
            AudioManager.Instance?.PlayPickup();
        };
        GetNode<Node3D>("World").AddChild(pickup);
    }
}
