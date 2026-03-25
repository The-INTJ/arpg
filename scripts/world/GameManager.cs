using Godot;

namespace ARPG;

public partial class GameManager : Node3D, IDeveloperEffectProvider
{
    public const string ActiveZoneDetectionEffectId = "active_zone_detection";
    public const string ExploreRegenEffectId = "explore_regen";
    public const string BridgeEnergyRequirementEffectId = "bridge_energy_requirement";

    private static readonly Vector3[] ZoneOrigins =
    {
        new(0, 0, 0),
        new(0, 5.5f, -128f),
        new(0, 1.5f, -256f),
    };

    private static readonly Vector3 PlayerSpawnOffset = new(0, 0.25f, 40f);
    private static readonly Vector3 ZoneExitOffset = new(0, MapGenerator.GroundTop, -(MapGenerator.PlayDepth * 0.5f));
    private static readonly Vector3 ZoneEntryOffset = new(0, MapGenerator.GroundTop, MapGenerator.PlayDepth * 0.5f);
    private static readonly Vector3 ZoneBoundsPadding = new(0, 40f, 0);

    private PlayerController _player;
    private TurnManager _turnManager;
    private CombatManager _combatManager;
    private AggroSystem _aggroSystem;
    private DeveloperToolsManager _developerTools;
    private GameHudUpdater _hudUpdater;
    private PlayerActionHandler _actionHandler;
    private ModifyStatsSimple _modifyScreen;
    private PauseScreen _pauseScreen;
    private Node3D _worldRoot;
    private Node3D _zonesRoot;
    private Node3D _bridgePointsRoot;
    private Label _roomLabel;
    private Label _ruleLabel;

    private DarkEnergy[] _zoneEnergy;
    private RoomMonsterEffectProfile[] _zoneProfiles;
    private string[] _zoneNames;
    private Aabb[] _zoneBounds;
    private BridgePoint[] _zoneBridges;
    private int _activeZoneRoom = 1;
    private bool _isEndingRun;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("World/Player");
        _worldRoot = GetNode<Node3D>("World");
        _zonesRoot = EnsureWorldChild("Zones");
        _bridgePointsRoot = EnsureWorldChild("BridgePoints");
        var camera = _player.GetNode<Camera3D>("CameraRig/Camera3D");
        var canvas = GetNode<CanvasLayer>("CanvasLayer");
        var cameraController = _player.GetNode<CameraController>("CameraRig");

        _developerTools = new DeveloperToolsManager();
        _developerTools.Name = "DeveloperToolsManager";
        AddChild(_developerTools);

        _turnManager = new TurnManager();
        AddChild(_turnManager);
        _turnManager.TurnChanged += OnTurnChanged;

        _combatManager = new CombatManager();
        AddChild(_combatManager);
        _combatManager.Init(_player, _turnManager, camera);
        _combatManager.CombatEnded += OnCombatEnded;
        _combatManager.CombatFeedback += text => _hudUpdater.StatusText = text;

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

        _actionHandler = new PlayerActionHandler();
        AddChild(_actionHandler);
        _actionHandler.Init(_player, _turnManager, _combatManager, _aggroSystem);
        _actionHandler.StatusMessage += text => _hudUpdater.StatusText = text;

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

        _roomLabel = GetNode<Label>("CanvasLayer/RoomLabel");
        _ruleLabel = GetNode<Label>("CanvasLayer/RuleLabel");

        var hpLabel = GetNode<Label>("CanvasLayer/HUD/HpLabel");
        var statsLabel = GetNode<Label>("CanvasLayer/HUD/StatsLabel");
        var darkEnergyLabel = GetNode<Label>("CanvasLayer/HUD/DarkEnergyLabel");
        var darkEnergyBar = GetNode<ProgressBar>("CanvasLayer/HUD/DarkEnergyBar");
        var statusLabel = GetNode<Label>("CanvasLayer/HUD/StatusLabel");
        GameHudBuilder.StyleHudLabels(
            new[] { hpLabel, statsLabel, darkEnergyLabel, statusLabel },
            GetViewport().GetVisibleRect().Size.Y);

        var itemBarHBox = GetNode<HBoxContainer>("CanvasLayer/ItemBarCenter/ItemBarHBox");

        _hudUpdater = new GameHudUpdater();
        AddChild(_hudUpdater);
        _hudUpdater.Init(_player, _turnManager, _combatManager, _aggroSystem, _actionHandler, camera, canvas,
            hpLabel, statsLabel, darkEnergyLabel, darkEnergyBar, statusLabel, attackButton, abilityButton,
            enemyHp, itemBarHBox);

        _player.EdgeFall += appliedDamage => _hudUpdater.StatusText = appliedDamage
            ? "Fell into the abyss! -5 HP"
            : "Boundary recovery triggered.";

        _modifyScreen = GetNode<ModifyStatsSimple>("CanvasLayer/ModifyStatsSimple");
        _pauseScreen = GetNode<PauseScreen>("CanvasLayer/PauseScreen");
        _pauseScreen.ViewStatsRequested += () => _modifyScreen.Open(_player.Stats);
        _pauseScreen.Init(_developerTools);

        var floorMesh = GetNode<MeshInstance3D>("World/Floor/FloorMesh");
        floorMesh.Visible = false;
        var floorCollision = GetNode<CollisionShape3D>("World/Floor/FloorCollision");
        floorCollision.Disabled = true;

        GetNodeOrNull<Node>("World/MapGenerator")?.QueueFree();
        GetNodeOrNull<Node>("World/Enemies")?.QueueFree();
        GetNodeOrNull<Node>("World/ExitDoor")?.QueueFree();

        SetupAudioManager();
        BuildRunWorld();
        _developerTools.BindProvider(_player, _player);
        _developerTools.BindProvider(cameraController, cameraController);
        _developerTools.BindProvider(_aggroSystem, _aggroSystem);
        _developerTools.BindProvider(this, this);
        RefreshBridgeReadiness();

        _player.GlobalPosition = ZoneOrigins[0] + PlayerSpawnOffset;
        _player.SetZoneBounds(BuildZoneBoundsArray());
        GameState.DarkEnergyCarryOver = 0;
        SetActiveZone(1, force: true);

        AudioManager.Instance?.StartExploreMusic();
    }

    private void SetupAudioManager()
    {
        if (AudioManager.Instance != null)
            return;

        var audio = new AudioManager();
        audio.Name = "AudioManager";
        GetTree().Root.AddChild(audio);
    }

    private Node3D EnsureWorldChild(string name)
    {
        var existing = _worldRoot.GetNodeOrNull<Node3D>(name);
        if (existing != null)
            return existing;

        var node = new Node3D();
        node.Name = name;
        _worldRoot.AddChild(node);
        return node;
    }

    private void BuildRunWorld()
    {
        _zoneEnergy = new DarkEnergy[GameState.TotalRooms + 1];
        _zoneProfiles = new RoomMonsterEffectProfile[GameState.TotalRooms + 1];
        _zoneNames = new string[GameState.TotalRooms + 1];
        _zoneBounds = new Aabb[GameState.TotalRooms + 1];
        _zoneBridges = new BridgePoint[GameState.TotalRooms + 1];

        _worldRoot.AddChild(DistantChunkGenerator.Generate(1));

        int openingEnergy = GameState.DarkEnergyCarryOver;
        for (int room = 1; room <= GameState.TotalRooms; room++)
            BuildZone(room, room == 1 ? openingEnergy : 0);
    }

    private void BuildZone(int room, int startingEnergy)
    {
        Vector3 zoneOrigin = ZoneOrigins[room - 1];

        var zoneRoot = new Node3D();
        zoneRoot.Name = $"Zone{room}";
        zoneRoot.Position = zoneOrigin;
        _zonesRoot.AddChild(zoneRoot);

        var mapGen = new MapGenerator();
        mapGen.Name = "MapGenerator";
        zoneRoot.AddChild(mapGen);
        var generatedMap = mapGen.Generate();

        var profile = MonsterEffectRoomProfiles.ForRoom(room);
        _zoneProfiles[room] = profile;
        _zoneNames[room] = ChunkNames.RandomName();
        _zoneEnergy[room] = new DarkEnergy(DarkEnergy.ThresholdForRoom(room), startingEnergy);
        _zoneBounds[room] = CreateZoneBounds(zoneOrigin);

        var enemiesContainer = new Node3D();
        enemiesContainer.Name = "Enemies";
        zoneRoot.AddChild(enemiesContainer);

        var encounter = EnemySpawner.BuildEncounter(room);
        for (int i = 0; i < encounter.Length && i < generatedMap.EnemySpawnPoints.Length; i++)
        {
            var enemy = EnemySpawner.Spawn(enemiesContainer, generatedMap.EnemySpawnPoints[i], encounter[i], room, profile);
            enemy.ZoneRoom = room;
        }

        SpawnCaveChest(zoneOrigin + generatedMap.CaveChestPosition, room);

        if (room >= GameState.TotalRooms)
            return;

        var bridge = GD.Load<PackedScene>(Scenes.BridgePoint).Instantiate<BridgePoint>();
        bridge.Name = $"Bridge{room}To{room + 1}";
        bridge.Position = zoneOrigin + ZoneExitOffset;
        _bridgePointsRoot.AddChild(bridge);
        bridge.Configure(ZoneOrigins[room] + ZoneEntryOffset);
        _zoneBridges[room] = bridge;

        bridge.SetEnergyRequirementSatisfied(IsBridgeEnergySatisfied(room));
    }

    private static Aabb CreateZoneBounds(Vector3 zoneOrigin)
    {
        return new Aabb(
            zoneOrigin + new Vector3(-MapGenerator.PlayWidth * 0.5f, -ZoneBoundsPadding.Y, -MapGenerator.PlayDepth * 0.5f),
            new Vector3(MapGenerator.PlayWidth, ZoneBoundsPadding.Y * 2.0f, MapGenerator.PlayDepth));
    }

    private Aabb[] BuildZoneBoundsArray()
    {
        var bounds = new Aabb[GameState.TotalRooms];
        for (int room = 1; room <= GameState.TotalRooms; room++)
            bounds[room - 1] = _zoneBounds[room];

        return bounds;
    }

    private int FindZoneForPosition(Vector3 worldPosition)
    {
        for (int room = 1; room <= GameState.TotalRooms; room++)
        {
            if (_zoneBounds[room].HasPoint(worldPosition))
                return room;
        }

        return 0;
    }

    private void SetActiveZone(int room, bool force = false)
    {
        if (!force && room == _activeZoneRoom)
            return;

        _activeZoneRoom = room;
        GameState.CurrentRoom = room;
        _roomLabel.Text = $"{_zoneNames[room]}  ({room}/{GameState.TotalRooms})";

        var profile = _zoneProfiles[room];
        _ruleLabel.Text = $"{profile.DisplayName}\n{profile.Description}";

        if (!_isEndingRun && _turnManager.IsExploring)
            _hudUpdater.StatusText = GetZoneIntroText(room);
    }

    private string GetZoneIntroText(int room)
    {
        if (room == GameState.BossRoom)
            return "Boss zone! Defeat the boss to finish the run.";

        var bridge = _zoneBridges[room];
        if (bridge != null && bridge.IsBuilt)
            return "Bridge is built. Cross to the next zone when you're ready.";

        if (IsBridgeEnergySatisfied(room))
            return "Enough dark energy! Find the bridge point to cross!";

        return "Defeat enemies to gather dark energy for the bridge.";
    }

    private bool TryBuildCurrentBridge()
    {
        if (!_turnManager.IsExploring || _activeZoneRoom >= GameState.TotalRooms)
            return false;

        var bridge = _zoneBridges[_activeZoneRoom];
        return bridge != null && bridge.TryBuild();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(GameKeys.Attack))
        {
            if (TryBuildCurrentBridge())
                _hudUpdater.StatusText = "Bridge formed. Cross to the next zone.";
            else
                _actionHandler.OnAttackPressed();
        }
        else if (@event.IsActionPressed(GameKeys.Ability))
        {
            _actionHandler.OnAbilityPressed();
        }

        for (int i = 0; i < _player.Stats.Inventory.Capacity && i < GameKeys.KeyboardItemSlotCount; i++)
        {
            if (!@event.IsActionPressed(GameKeys.KeyboardItemSlotAction(i)))
                continue;

            _actionHandler.OnItemSlotPressed(i);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public override void _Process(double delta)
    {
        if (IsEffectEnabled(ActiveZoneDetectionEffectId))
        {
            int detectedZone = FindZoneForPosition(_player.GlobalPosition);
            if (detectedZone != 0)
                SetActiveZone(detectedZone);
        }

        _hudUpdater.UpdateAll(_zoneEnergy[_activeZoneRoom]);

        if (!_isEndingRun && _player.Hp <= 0 && _turnManager.State != TurnState.Defeat)
        {
            _turnManager.SetState(TurnState.Defeat);
            return;
        }

        if (_turnManager.IsExploring)
        {
            if (IsEffectEnabled(ExploreRegenEffectId))
                _player.TickRegen((float)delta);
            _aggroSystem.Tick((float)delta);
        }
    }

    private void OnCombatEnded()
    {
        int room = Mathf.Clamp(_combatManager.LastKillRoom, 1, GameState.TotalRooms);
        bool wasBoss = _combatManager.LastKillWasBoss;
        bool wasElite = _combatManager.LastKillWasElite;
        int energy = DarkEnergy.EnergyForKill(wasBoss, wasElite);
        bool thresholdWasMet = IsBridgeEnergySatisfied(room);
        _zoneEnergy[room].Add(energy);
        RefreshBridgeReadiness(room);
        GameState.RecordKill(wasBoss);
        _hudUpdater.AttackButton.Visible = false;
        _hudUpdater.AbilityButton.Visible = false;

        if (wasBoss)
            AudioManager.Instance?.PlayBossDeath();
        else
            AudioManager.Instance?.PlayEnemyDeath();

        AudioManager.Instance?.StopCombatMusic();

        if (_player.Hp <= 0)
        {
            _turnManager.SetState(TurnState.Defeat);
            return;
        }

        if (wasBoss && room == GameState.BossRoom)
        {
            _hudUpdater.StatusText = "Boss defeated! The path home is clear.";
            BeginVictory();
            return;
        }

        SpawnLoot(_combatManager.LastKillPosition);
        var droppedItem = _combatManager.LastKillItemDrop;
        if (droppedItem != null)
            SpawnInventoryItem(_combatManager.LastKillPosition + new Vector3(0.75f, 0, 0.4f), droppedItem);

        bool thresholdIsMet = IsBridgeEnergySatisfied(room);
        if (room < GameState.TotalRooms && !thresholdWasMet && thresholdIsMet)
        {
            AudioManager.Instance?.PlayLevelUp();
            _hudUpdater.StatusText = droppedItem == null
                ? "Enough dark energy! Find the bridge point to cross!"
                : $"Enough dark energy! Find the bridge point to cross! {droppedItem.Name} dropped nearby.";
        }
        else
        {
            string energyText = $"+{energy} dark energy";
            _hudUpdater.StatusText = droppedItem == null
                ? $"Enemy defeated! {energyText}"
                : $"Enemy defeated! {energyText}. {droppedItem.Name} dropped nearby.";
        }

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

    private void BeginVictory()
    {
        if (_isEndingRun)
            return;

        _isEndingRun = true;
        _hudUpdater.HideAllCombatUI();
        _player.SetPhysicsProcess(false);
        _pauseScreen.Visible = false;
        _modifyScreen.Visible = false;
        GetTree().Paused = false;

        CallDeferred(nameof(ShowVictoryScreen));
    }

    private void ShowGameOverScreen()
    {
        GameState.FinalizeCurrentRun(RunOutcome.Defeat, _player?.Stats);
        GetTree().ChangeSceneToFile(Scenes.GameOverScreen);
    }

    private void ShowVictoryScreen()
    {
        GameState.FinalizeCurrentRun(RunOutcome.Victory, _player?.Stats);
        GetTree().ChangeSceneToFile(Scenes.VictoryScreen);
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
            string keyName = GameKeys.ItemSlotUseHint(slotIndex);
            _hudUpdater.StatusText = $"Picked up {itemName} ({keyName})";
            AudioManager.Instance?.PlayPickup();
        };
        pickup.InventoryFull += itemName =>
        {
            _hudUpdater.StatusText = $"Inventory full for {itemName}.";
        };
        _worldRoot.AddChild(pickup);
    }

    private void SpawnCaveChest(Vector3 position, int room)
    {
        var chest = GD.Load<PackedScene>(Scenes.CaveChest).Instantiate<CaveChest>();
        chest.Position = position;
        chest.Init(InventoryItem.CreateForRoom(room));
        chest.Opened += itemName =>
        {
            _hudUpdater.StatusText = $"Opened cave cache: {itemName}.";
            AudioManager.Instance?.PlayPickup();
        };
        _worldRoot.AddChild(chest);
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
        _worldRoot.AddChild(pickup);
    }

    public void RegisterDeveloperEffects(DeveloperToolsManager developerTools)
    {
        if (_developerTools != developerTools)
            _developerTools = developerTools;

        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                ActiveZoneDetectionEffectId,
                "Active Zone Detection",
                "Update the current room and HUD labels from the player's world position.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Boundary,
                OwnerLabel: "World",
                Order: 30),
            enabled =>
            {
                if (!enabled || _player == null)
                    return;

                int detectedZone = FindZoneForPosition(_player.GlobalPosition);
                if (detectedZone != 0)
                    SetActiveZone(detectedZone);
            });
        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                ExploreRegenEffectId,
                "Explore Regen",
                "Tick passive HP regeneration while the run is in exploration mode.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Progression,
                OwnerLabel: "World",
                Order: 10));
        _developerTools?.RegisterEffect(
            this,
            new DeveloperEffectDescriptor(
                BridgeEnergyRequirementEffectId,
                "Bridge Energy Requirement",
                "Require the dark energy threshold before a bridge becomes ready.",
                DeveloperEffectKind.Toggle,
                DeveloperEffectGroups.Progression,
                OwnerLabel: "World",
                Order: 20),
            _ => RefreshBridgeReadiness());
    }

    private bool IsEffectEnabled(string effectId)
    {
        return _developerTools?.IsEffectEnabled(this, effectId) ?? true;
    }

    private bool IsBridgeEnergySatisfied(int room)
    {
        if (room < 1 || room >= GameState.TotalRooms)
            return true;

        return !IsEffectEnabled(BridgeEnergyRequirementEffectId) || _zoneEnergy[room].IsThresholdMet;
    }

    private void RefreshBridgeReadiness()
    {
        if (_zoneBridges == null)
            return;

        for (int room = 1; room < _zoneBridges.Length; room++)
            RefreshBridgeReadiness(room);
    }

    private void RefreshBridgeReadiness(int room)
    {
        if (_zoneBridges == null || room <= 0 || room >= _zoneBridges.Length)
            return;

        _zoneBridges[room]?.SetEnergyRequirementSatisfied(IsBridgeEnergySatisfied(room));
    }
}
