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
    private CombatSystem _combatSystem;
    private AttackDefinition _attack;
    private MeshInstance3D _mesh;
    private bool _resolved;

    /// <summary>
    /// Create a player-fired projectile. Damage is pre-computed at fire time
    /// (so stat buffs are consumed correctly).
    /// </summary>
    public static Projectile CreatePlayerProjectile(
        Vector3 direction,
        int damage,
        PlayerController owner,
        CombatSystem combatSystem,
        AttackDefinition attack,
        Color color,
        float lifetime = DefaultLifetime)
    {
        var proj = new Projectile();
        proj._direction = direction.Normalized();
        proj._speed = attack.ProjectileSpeed;
        proj._damage = damage;
        proj._lifetime = lifetime;
        proj._ownerBody = owner;
        proj._sourceEnemy = null;
        proj._isPlayerProjectile = true;
        proj._combatSystem = combatSystem;
        proj._attack = attack;
        proj.BuildVisual(attack.ProjectileVisualRadius, color);
        return proj;
    }

    /// <summary>
    /// Create an enemy-fired projectile. Damage is resolved at hit time
    /// through the full monster-effect pipeline.
    /// </summary>
    public static Projectile CreateEnemyProjectile(
        Vector3 direction,
        int damage,
        Enemy source,
        CombatSystem combatSystem,
        AttackDefinition attack,
        Color color,
        float lifetime = DefaultLifetime)
    {
        var proj = new Projectile();
        proj._direction = direction.Normalized();
        proj._speed = attack.ProjectileSpeed;
        proj._damage = damage;
        proj._lifetime = lifetime;
        proj._ownerBody = source;
        proj._sourceEnemy = source;
        proj._isPlayerProjectile = false;
        proj._combatSystem = combatSystem;
        proj._attack = attack;
        proj.BuildVisual(attack.ProjectileVisualRadius, color);
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

        var hurtboxHit = IntersectHurtbox(from, to);
        var blockerHit = IntersectWorldBlocker(from, to);
        if (hurtboxHit.Count > 0 && (blockerHit.Count == 0 || HitDistance(from, hurtboxHit) <= HitDistance(from, blockerHit)))
        {
            GlobalPosition = hurtboxHit["position"].AsVector3();
            OnHitHurtbox(hurtboxHit["collider"].AsGodotObject() as CombatHurtbox);
            return;
        }

        if (blockerHit.Count > 0)
        {
            GlobalPosition = blockerHit["position"].AsVector3();
            QueueFree();
            return;
        }

        GlobalPosition = to;
    }

    private Godot.Collections.Dictionary IntersectHurtbox(Vector3 from, Vector3 to)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollideWithBodies = false;
        query.CollisionMask = CombatLayers.CombatHurtboxes;
        query.Exclude = BuildExcludeList();
        return GetWorld3D().DirectSpaceState.IntersectRay(query);
    }

    private Godot.Collections.Dictionary IntersectWorldBlocker(Vector3 from, Vector3 to)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatLayers.WorldBodies;
        query.Exclude = BuildExcludeList();
        return GetWorld3D().DirectSpaceState.IntersectRay(query);
    }

    private Godot.Collections.Array<Rid> BuildExcludeList()
    {
        var exclude = new Godot.Collections.Array<Rid>();
        if (_ownerBody != null && IsInstanceValid(_ownerBody))
            exclude.Add(_ownerBody.GetRid());

        if (_ownerBody is ICombatant combatant && combatant.Hurtbox != null && IsInstanceValid(combatant.Hurtbox))
            exclude.Add(combatant.Hurtbox.GetRid());

        return exclude;
    }

    private void OnHitHurtbox(CombatHurtbox hurtbox)
    {
        if (_resolved || hurtbox == null)
            return;

        _resolved = true;
        if (_isPlayerProjectile)
            _combatSystem?.ResolveProjectileHitEnemy(hurtbox, _damage);
        else
            _combatSystem?.ResolveProjectileHitPlayer(hurtbox, _sourceEnemy, _damage);

        QueueFree();
    }

    private static float HitDistance(Vector3 from, Godot.Collections.Dictionary hit)
    {
        return hit.Count == 0 ? float.MaxValue : from.DistanceTo(hit["position"].AsVector3());
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
