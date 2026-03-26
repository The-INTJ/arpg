using Godot;

namespace ARPG;

public readonly partial record struct AttackVolumeDefinition(
    AttackVolumeShape Shape,
    Vector3 BaseSize,
    Vector3 LocalOffset,
    bool ScaleForwardByReach = true,
    bool ScaleCrossSectionByAttackSize = true,
    bool ScaleHeightByAttackSize = true)
{
    public static AttackVolumeDefinition CreateBox(Vector3 baseSize, Vector3 localOffset)
        => new(AttackVolumeShape.Box, baseSize, localOffset);

    public static AttackVolumeDefinition CreateSphere(float radius, Vector3 localOffset)
        => new(AttackVolumeShape.Sphere, new Vector3(radius * 2.0f, radius * 2.0f, radius * 2.0f), localOffset);

    public Vector3 ResolveSize(float attackReach, float attackSize)
    {
        float reach = Mathf.Max(0.05f, attackReach);
        float size = Mathf.Max(0.05f, attackSize);

        return new Vector3(
            BaseSize.X * (ScaleCrossSectionByAttackSize ? size : 1.0f),
            BaseSize.Y * (ScaleHeightByAttackSize ? size : 1.0f),
            BaseSize.Z * (ScaleForwardByReach ? reach : 1.0f));
    }

    public Vector3 ResolveLocalOffset(float attackReach, float attackSize)
    {
        float reach = Mathf.Max(0.05f, attackReach);
        float size = Mathf.Max(0.05f, attackSize);
        return new Vector3(
            LocalOffset.X * size,
            LocalOffset.Y * size,
            LocalOffset.Z * reach);
    }

    public Shape3D CreateShape(float attackReach, float attackSize)
    {
        Vector3 size = ResolveSize(attackReach, attackSize);
        return Shape switch
        {
            AttackVolumeShape.Box => new BoxShape3D { Size = size },
            AttackVolumeShape.Sphere => new SphereShape3D { Radius = Mathf.Max(size.X, Mathf.Max(size.Y, size.Z)) * 0.5f },
            AttackVolumeShape.Capsule => new CapsuleShape3D
            {
                Radius = Mathf.Max(size.X, size.Z) * 0.5f,
                Height = Mathf.Max(size.Y, Mathf.Max(size.Z, size.X)),
            },
            AttackVolumeShape.Cylinder => new CylinderShape3D
            {
                Radius = Mathf.Max(size.X, size.Z) * 0.5f,
                Height = size.Y,
            },
            _ => new BoxShape3D { Size = size },
        };
    }

    public Transform3D BuildTransform(Vector3 origin, Vector3 direction, float attackReach, float attackSize)
    {
        Vector3 flatDirection = direction;
        flatDirection.Y = 0.0f;
        if (flatDirection.LengthSquared() <= 0.001f)
            flatDirection = Vector3.Forward;
        else
            flatDirection = flatDirection.Normalized();

        float yaw = Mathf.Atan2(flatDirection.X, flatDirection.Z);
        Basis basis = new Basis(Vector3.Up, yaw);
        Vector3 worldOffset = basis * ResolveLocalOffset(attackReach, attackSize);
        return new Transform3D(basis, origin + worldOffset);
    }
}
