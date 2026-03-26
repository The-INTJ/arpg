using Godot;

namespace ARPG;

public readonly partial record struct AttackHitResult(
    ICombatant Target,
    CombatHurtbox Hurtbox,
    float Score,
    float Distance,
    Vector3 HitPoint);
