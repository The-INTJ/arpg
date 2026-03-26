using Godot;

namespace ARPG;

public readonly partial record struct AttackQuery(
    ICombatant Attacker,
    AttackDefinition Attack,
    Vector3 Origin,
    Vector3 Direction,
    float AttackReach,
    float AttackSize,
    int ZoneRoom,
    bool IsPreview = false);
