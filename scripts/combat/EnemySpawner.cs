using Godot;

namespace ARPG;

/// <summary>
/// Static factory for creating and configuring enemy instances.
/// </summary>
public static class EnemySpawner
{
    private static PackedScene _slimeVisualScene;

    public static EnemySpawnPlan[] BuildEncounter(int room)
    {
        return room switch
        {
            1 => new[]
            {
                EnemySpawnPlan.Normal(forcedVariant: SpriteFactory.SlimeVariant),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room)),
            },
            2 => new[]
            {
                EnemySpawnPlan.Normal(forcedVariant: SpriteFactory.SlimeVariant),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room)),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room)),
            },
            3 => new[]
            {
                EnemySpawnPlan.Boss(InventoryItem.CreateEnemyDrop(room, fromBoss: true)),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room), forcedVariant: SpriteFactory.SlimeVariant),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room)),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Normal(),
            },
            _ => new[]
            {
                EnemySpawnPlan.Normal(forcedVariant: SpriteFactory.SlimeVariant),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Normal(),
                EnemySpawnPlan.Ranged(),
                EnemySpawnPlan.Elite(InventoryItem.CreateEnemyDrop(room)),
            },
        };
    }

    public static Enemy Spawn(Node3D container, Vector3 position, EnemySpawnPlan plan, int room, RoomMonsterEffectProfile profile)
    {
        var enemy = new Enemy();
        enemy.Position = position;
        enemy.AddToGroup("enemies");

        var visualRoot = new Node3D();
        visualRoot.Name = "VisualRoot";
        enemy.AddChild(visualRoot);

        enemy.ScaleForRoom(room);
        enemy.AssignItemDrop(plan.ItemDrop);

        if (plan.IsElite)
            enemy.MakeElite();

        if (plan.IsBoss)
        {
            enemy.MakeBoss();
            var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateBossTexture(), 0.07f);
            sprite.Position = new Vector3(0, 0.35f, 0);
            visualRoot.AddChild(sprite);
        }
        else
        {
            int variant = plan.ForcedVariant ?? SpriteFactory.RandomEnemyVariant();
            enemy.VariantName = SpriteFactory.EnemyVariantName(variant);

            if (!TryAddVariantVisual(visualRoot, variant, plan.IsElite))
            {
                var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateEnemyTexture(variant), plan.IsElite ? 0.055f : 0.05f);
                sprite.Position = new Vector3(0, 0.25f, 0);
                visualRoot.AddChild(sprite);
            }

            if (plan.IsElite)
                AddEliteMarker(enemy);
        }

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D
        {
            Size = plan.IsBoss
                ? new Vector3(0.42f, 0.72f, 0.42f)
                : plan.IsElite
                    ? new Vector3(0.34f, 0.58f, 0.34f)
                    : new Vector3(0.3f, 0.5f, 0.3f)
        };
        shape.Position = new Vector3(0, plan.IsBoss ? 0.34f : plan.IsElite ? 0.29f : 0.25f, 0);
        enemy.AddChild(shape);

        container.AddChild(enemy);

        if (plan.IsRanged)
            enemy.SetCombatProfile(EnemyCombatProfile.CreateRangedMvp());

        var effectPlan = MonsterEffectGenerator.Generate(new MonsterEffectRollContext(
            room,
            profile,
            enemy.IsBoss));
        enemy.SetMonsterEffects(effectPlan);

        return enemy;
    }

    private static bool TryAddVariantVisual(Node3D visualRoot, int variant, bool isElite)
    {
        if (variant != SpriteFactory.SlimeVariant)
            return false;

        var scene = GetSlimeVisualScene();
        if (scene == null)
            return false;

        var slimeVisual = scene.Instantiate<Node3D>();
        slimeVisual.Name = "SlimeVisual";
        slimeVisual.Scale = isElite ? new Vector3(1.08f, 1.08f, 1.08f) : Vector3.One;
        visualRoot.AddChild(slimeVisual);
        return true;
    }

    private static PackedScene GetSlimeVisualScene()
    {
        _slimeVisualScene ??= ResourceLoader.Load<PackedScene>(Scenes.SlimeVisual);
        return _slimeVisualScene;
    }

    private static void AddEliteMarker(Enemy enemy)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = "EliteMarker";
        mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        mesh.Position = new Vector3(0, 0.03f, 0);
        mesh.Mesh = new CylinderMesh
        {
            TopRadius = 0.26f,
            BottomRadius = 0.31f,
            Height = 0.04f,
            RadialSegments = 18,
        };

        var markerColor = Palette.Accent;
        markerColor.A = 0.82f;
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = markerColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = Palette.Accent,
            EmissionEnergyMultiplier = 1.25f,
            Roughness = 0.25f,
        };

        enemy.AddChild(mesh);
    }
}
