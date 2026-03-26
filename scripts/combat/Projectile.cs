using Godot;

namespace ARPG;

/// <summary>
/// A projectile that travels in a straight line and resolves damage on hit.
/// Uses per-frame raycasting for reliable collision detection at any speed.
/// </summary>
public partial class Projectile : Node3D
{
    private const float DefaultLifetime = 3.0f;

    private Vector3 _direction;
    private float _speed;
    private int _damage;
    private float _lifetime;
    private float _elapsed;
    private CharacterBody3D _ownerBody;
    private Enemy _sourceEnemy;
    private bool _isPlayerProjectile;
    private CombatManager _combatManager;
    private MeshInstance3D _mesh;
    private bool _resolved;

    /// <summary>
    /// Create a player-fired projectile. Damage is pre-computed at fire time
    /// (so stat buffs are consumed correctly).
    /// </summary>
    public static Projectile CreatePlayerProjectile(
        Vector3 direction, float speed, int damage,
        PlayerController owner, CombatManager combatManager,
        float visualRadius, Color color, float lifetime = DefaultLifetime)
    {
        var proj = new Projectile();
        proj._direction = direction.Normalized();
        proj._speed = speed;
        proj._damage = damage;
        proj._lifetime = lifetime;
        proj._ownerBody = owner;
        proj._sourceEnemy = null;
        proj._isPlayerProjectile = true;
        proj._combatManager = combatManager;
        proj.BuildVisual(visualRadius, color);
        return proj;
    }

    /// <summary>
    /// Create an enemy-fired projectile. Damage is resolved at hit time
    /// through the full monster-effect pipeline.
    /// </summary>
    public static Projectile CreateEnemyProjectile(
        Vector3 direction, float speed,
        Enemy source, CombatManager combatManager,
        float visualRadius, Color color, float lifetime = DefaultLifetime)
    {
        var proj = new Projectile();
        proj._direction = direction.Normalized();
        proj._speed = speed;
        proj._damage = source.AttackDamage;
        proj._lifetime = lifetime;
        proj._ownerBody = source;
        proj._sourceEnemy = source;
        proj._isPlayerProjectile = false;
        proj._combatManager = combatManager;
        proj.BuildVisual(visualRadius, color);
        return proj;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_resolved)
            return;

        float dt = (float)delta;
        _elapsed += dt;
        if (_elapsed >= _lifetime)
        {
            QueueFree();
            return;
        }

        Vector3 movement = _direction * _speed * dt;
        Vector3 from = GlobalPosition;
        Vector3 to = from + movement;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;

        var exclude = new Godot.Collections.Array<Rid>();
        if (_ownerBody != null && IsInstanceValid(_ownerBody))
            exclude.Add(_ownerBody.GetRid());
        query.Exclude = exclude;

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            GlobalPosition = result["position"].AsVector3();
            OnHit(result["collider"].As<Node3D>());
            return;
        }

        GlobalPosition = to;
    }

    private void OnHit(Node3D body)
    {
        if (_resolved)
            return;

        _resolved = true;

        if (_isPlayerProjectile && body is Enemy enemy && !enemy.IsDead)
        {
            _combatManager?.ResolveProjectileHitEnemy(enemy, _damage);
        }
        else if (!_isPlayerProjectile && body is PlayerController)
        {
            _combatManager?.ResolveProjectileHitPlayer(_sourceEnemy);
        }

        QueueFree();
    }

    private void BuildVisual(float radius, Color color)
    {
        _mesh = new MeshInstance3D();
        _mesh.Mesh = new SphereMesh
        {
            Radius = radius,
            Height = radius * 2.0f,
            RadialSegments = 8,
            Rings = 4,
        };
        _mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 2.5f,
            Roughness = 0.2f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        _mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        AddChild(_mesh);
    }
}
