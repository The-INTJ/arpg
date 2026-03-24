using Godot;

namespace ARPG;

/// <summary>
/// Static factory for creating and configuring enemy instances.
/// </summary>
public static class EnemySpawner
{
    public static int GetEnemyCount(int room)
    {
        return room switch
        {
            1 => 3,
            2 => 4,
            3 => 4, // 1 boss + 3 normal
            _ => 3
        };
    }

    public static Enemy Spawn(Node3D container, Vector3 position, bool isBoss, int room, RoomMonsterEffectProfile profile)
    {
        var enemy = new Enemy();
        enemy.Position = position;
        enemy.AddToGroup("enemies");

        enemy.ScaleForRoom(room);

        if (isBoss)
        {
            enemy.MakeBoss();
            var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateBossTexture(), 0.07f);
            sprite.Position = new Vector3(0, 0.35f, 0);
            enemy.AddChild(sprite);
        }
        else
        {
            int variant = SpriteFactory.RandomEnemyVariant();
            enemy.VariantName = SpriteFactory.EnemyVariantName(variant);
            var sprite = SpriteFactory.CreateSprite(SpriteFactory.CreateEnemyTexture(variant), 0.05f);
            sprite.Position = new Vector3(0, 0.25f, 0);
            enemy.AddChild(sprite);
        }

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(0.3f, 0.5f, 0.3f) };
        shape.Position = new Vector3(0, 0.25f, 0);
        enemy.AddChild(shape);

        container.AddChild(enemy);

        var effectPlan = MonsterEffectGenerator.Generate(new MonsterEffectRollContext(
            room,
            profile,
            enemy.IsBoss));
        enemy.SetMonsterEffects(effectPlan);

        return enemy;
    }
}
