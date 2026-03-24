using Godot;

namespace ARPG;

public partial class GameManager : Node3D
{
    private PlayerController _player;
    private BridgePoint _bridgePoint;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;
    private GameHudUpdater _hudUpdater;
    private PlayerActionHandler _actionHandler;
    private ModifyStatsSimple _modifyScreen;
    private PauseScreen _pauseScreen;

    private DarkEnergy _darkEnergy;
    private int _totalEnemies;
    private bool _isEndingRun;

    private int _room => GameState.CurrentRoom;
    private bool _isBossRoom => _room == GameState.BossRoom;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _bridgePoint = GetNode<BridgePoint>("World/ExitDoor");
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
        roomLabel.Text = $"{ChunkNames.RandomName()}  ({_room}/{GameState.TotalRooms})";
        var ruleLabel = GetNode<Label>("CanvasLayer/RuleLabel");
        ruleLabel.Text = $"{monsterEffectProfile.DisplayName}\n{monsterEffectProfile.Description}";

        var hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        var statsLabel = GetNode<Label>("CanvasLayer/HUD/StatsLabel");
        var darkEnergyLabel = GetNode<Label>("CanvasLayer/HUD/DarkEnergyLabel");
        var darkEnergyBar = GetNode<ProgressBar>("CanvasLayer/HUD/DarkEnergyBar");
        var statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");
        GameHudBuilder.StyleHudLabels(
            new[] { hpLabel, statsLabel, darkEnergyLabel, statusLabel },
            GetViewport().GetVisibleRect().Size.Y);

        var itemBarHBox = GetNode<HBoxContainer>("CanvasLayer/ItemBarCenter/ItemBarHBox");
        var (itemSlots, itemIcons, itemLabels, itemStyles) = GameHudBuilder.BindItemBar(itemBarHBox);

        // HUD updater
        _hudUpdater = new GameHudUpdater();
        AddChild(_hudUpdater);
        _hudUpdater.Init(_player, _turnManager, _combatManager, _aggroSystem, _actionHandler, camera, canvas,
            hpLabel, statsLabel, killLabel, statusLabel, attackButton, abilityButton,
            enemyHp, itemSlots, itemIcons, itemLabels, itemStyles);
            hpLabel, statsLabel, darkEnergyLabel, darkEnergyBar, statusLabel, attackButton, abilityButton,
            enemyHp, itemSlots, itemLabels, itemStyles);

        _player.EdgeFall += () => _hudUpdater.StatusText = "Too close to the edge! -5 HP";

        // Screens
        _modifyScreen = GetNode<ModifyStatsSimple>("CanvasLayer/ModifyStatsSimple");
        _pauseScreen = GetNode<PauseScreen>("CanvasLayer/PauseScreen");
        _pauseScreen.ViewStatsRequested += () => _modifyScreen.Open(_player.Stats);

        // The old flat floor stays only as an inert scene anchor. Terrain is now generated.
        var floorMesh = GetNode<MeshInstance3D>("World/Floor/FloorMesh");
        floorMesh.Visible = false;
        var floorCollision = GetNode<CollisionShape3D>("World/Floor/FloorCollision");
        floorCollision.Disabled = true;

        SetupAudioManager();

        // Generate map, chunk scenery, and spawn enemies
        var mapGen = GetNode<MapGenerator>("World/MapGenerator");
        var generatedMap = mapGen.Generate();
        var spawnPositions = mapGen.Generate();
        GetNode<Node3D>("World").AddChild(DistantChunkGenerator.Generate(_room));
        var enemiesContainer = GetNode<Node3D>("World/Enemies");
        var encounter = EnemySpawner.BuildEncounter(_room);

        for (int i = 0; i < encounter.Length && i < generatedMap.EnemySpawnPoints.Length; i++)
            EnemySpawner.Spawn(enemiesContainer, generatedMap.EnemySpawnPoints[i], encounter[i], _room, monsterEffectProfile);

        SpawnCaveChest(generatedMap.CaveChestPosition);

        _totalEnemies = encounter.Length;
        _darkEnergy = new DarkEnergy(DarkEnergy.ThresholdForRoom(_room), GameState.DarkEnergyCarryOver);
        GameState.DarkEnergyCarryOver = 0;
        _hudUpdater.StatusText = GetRoomIntroText();

        AudioManager.Instance?.StartExploreMusic();
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
            return "Boss chunk! Harvest dark energy to build the final bridge!";
        return "Harvest dark energy from enemies to bridge to the next chunk";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameKeys.Attack))
        {
            if (_bridgePoint.IsReadyAndPlayerNear && _turnManager.IsExploring)
                _bridgePoint.TryBuild();
            else
                _actionHandler.OnAttackPressed();
        }
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
        _hudUpdater.UpdateAll(_darkEnergy);
        GameState.DarkEnergyCarryOver = _darkEnergy.Excess;

        if (_turnManager.IsExploring)
        {
            _player.TickRegen((float)delta);
            _aggroSystem.Tick((float)delta);
        }
    }

    // --- Combat callbacks ---

    private void OnCombatEnded()
    {
        bool wasBoss = _combatManager.LastKillWasBoss;
        bool wasElite = _combatManager.LastKillWasElite;
        int energy = DarkEnergy.EnergyForKill(wasBoss, wasElite);
        _darkEnergy.Add(energy);
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

        if (_darkEnergy.IsThresholdMet)
        {
            _bridgePoint.SetEnergyReady();
            AudioManager.Instance?.PlayLevelUp();

            string clearText = _room >= GameState.TotalRooms
                ? "Energy overflowing! Find the bridge point!"
                : "Enough dark energy! Find the bridge point to cross!";
            _hudUpdater.StatusText = droppedItem == null
                ? clearText
                : $"{clearText} {droppedItem.Name} dropped nearby.";
        }
        else
        {
            string energyText = $"+{energy} dark energy";
            _hudUpdater.StatusText = droppedItem == null
                ? $"Enemy defeated! {energyText}"
                : $"Enemy defeated! {energyText}. {droppedItem.Name} dropped nearby.";
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

    private void SpawnCaveChest(Vector3 position)
    {
        var chest = GD.Load<PackedScene>(Scenes.CaveChest).Instantiate<CaveChest>();
        chest.Position = position;
        chest.Init(InventoryItem.CreateForRoom(_room));
        chest.Opened += itemName =>
        {
            _hudUpdater.StatusText = $"Opened cave cache: {itemName}.";
            AudioManager.Instance?.PlayPickup();
        };
        GetNode<Node3D>("World").AddChild(chest);
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
