using Godot;

namespace ARPG;

public static partial class CombatLayers
{
    public const uint WorldBodies = 1u << 0;
    public const uint ActorBodies = 1u << 1;
    public const uint CombatHurtboxes = 1u << 2;
    public const uint Projectiles = 1u << 3;
    public const uint Interactables = 1u << 4;

    public const uint ActorBodyMask = WorldBodies | ActorBodies;
    public const uint ProjectileHitMask = WorldBodies | ActorBodies | CombatHurtboxes;

    public static void ConfigureActorBody(CharacterBody3D body)
    {
        if (body == null)
            return;

        body.CollisionLayer = ActorBodies;
        body.CollisionMask = ActorBodyMask;
    }

    public static void ConfigureHurtbox(CombatHurtbox hurtbox)
    {
        if (hurtbox == null)
            return;

        hurtbox.CollisionLayer = CombatHurtboxes;
        hurtbox.CollisionMask = 0;
        hurtbox.Monitoring = true;
        hurtbox.Monitorable = true;
    }
}
