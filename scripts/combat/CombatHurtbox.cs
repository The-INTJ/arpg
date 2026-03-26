using Godot;

namespace ARPG;

public partial class CombatHurtbox : Area3D
{
    private CollisionShape3D _collisionShape;
    private ICombatant _ownerCombatant;

    public ICombatant OwnerCombatant => _ownerCombatant;
    public CollisionShape3D CollisionShape => _collisionShape;

    public override void _Ready()
    {
        _collisionShape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
        CombatLayers.ConfigureHurtbox(this);
    }

    public void BindOwner(ICombatant ownerCombatant)
    {
        _ownerCombatant = ownerCombatant;
        CombatLayers.ConfigureHurtbox(this);
    }

    public float ApproximateRadius
    {
        get
        {
            if (_collisionShape?.Shape is SphereShape3D sphere)
                return sphere.Radius;

            if (_collisionShape?.Shape is CapsuleShape3D capsule)
                return capsule.Radius;

            if (_collisionShape?.Shape is CylinderShape3D cylinder)
                return cylinder.Radius;

            if (_collisionShape?.Shape is BoxShape3D box)
                return Mathf.Max(box.Size.X, box.Size.Z) * 0.5f;

            return 0.25f;
        }
    }
}
