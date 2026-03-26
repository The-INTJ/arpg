using Godot;

namespace ARPG;

public interface ICombatant
{
    CombatTeam CombatTeam { get; }
    int ZoneRoom { get; }
    bool IsCombatAlive { get; }
    Node3D CombatNode { get; }
    CombatHurtbox Hurtbox { get; }
    Vector3 AttackOrigin { get; }
    Vector3 ProjectileOrigin { get; }
}
