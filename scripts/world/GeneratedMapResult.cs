using Godot;

namespace ARPG;

public partial class GeneratedMapResult
{
    public Vector3[] EnemySpawnPoints { get; }
    public Vector3 CaveChestPosition { get; }
    public Vector3 FallbackItemPosition { get; }

    public GeneratedMapResult(Vector3[] enemySpawnPoints, Vector3 caveChestPosition, Vector3 fallbackItemPosition)
    {
        EnemySpawnPoints = enemySpawnPoints ?? System.Array.Empty<Vector3>();
        CaveChestPosition = caveChestPosition;
        FallbackItemPosition = fallbackItemPosition;
    }
}
