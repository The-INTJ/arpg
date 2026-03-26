using System.Collections.Generic;
using Godot;

namespace ARPG;

public partial class CombatQueryService
{
    public AttackHitResult[] QueryTargets(AttackQuery query)
    {
        if (query.Attacker?.CombatNode == null || !GodotObject.IsInstanceValid(query.Attacker.CombatNode))
            return System.Array.Empty<AttackHitResult>();

        var world = query.Attacker.CombatNode.GetWorld3D();
        if (world == null)
            return System.Array.Empty<AttackHitResult>();

        var shape = query.Attack.Volume.CreateShape(query.AttackReach, query.AttackSize);
        var transform = query.Attack.Volume.BuildTransform(
            query.Origin,
            query.Direction,
            query.AttackReach,
            query.AttackSize);

        var parameters = new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = transform,
            CollisionMask = CombatLayers.CombatHurtboxes,
            CollideWithAreas = true,
            CollideWithBodies = false,
            Margin = 0.01f,
        };

        var exclude = new Godot.Collections.Array<Rid>();
        if (query.Attacker.CombatNode is CollisionObject3D attackerBody)
            exclude.Add(attackerBody.GetRid());
        if (query.Attacker.Hurtbox != null && GodotObject.IsInstanceValid(query.Attacker.Hurtbox))
            exclude.Add(query.Attacker.Hurtbox.GetRid());
        parameters.Exclude = exclude;

        var rawResults = world.DirectSpaceState.IntersectShape(parameters, 32);
        if (rawResults.Count == 0)
            return System.Array.Empty<AttackHitResult>();

        var hits = new List<AttackHitResult>();
        var seenTargets = new HashSet<ICombatant>();
        Vector3 forward = query.Direction;
        forward.Y = 0.0f;
        if (forward.LengthSquared() <= 0.001f)
            forward = Vector3.Forward;
        else
            forward = forward.Normalized();

        foreach (var rawResult in rawResults)
        {
            if (!rawResult.TryGetValue("collider", out Variant colliderVariant))
                continue;

            if (colliderVariant.AsGodotObject() is not CombatHurtbox hurtbox)
                continue;

            ICombatant target = hurtbox.OwnerCombatant;
            if (target == null || target == query.Attacker || !target.IsCombatAlive)
                continue;

            if (target.ZoneRoom != query.ZoneRoom || target.CombatTeam == query.Attacker.CombatTeam)
                continue;

            if (!seenTargets.Add(target))
                continue;

            if (query.Attack.RequiresClearPath && !HasClearPath(query.Attacker, target, query.Origin))
                continue;

            Vector3 targetPoint = hurtbox.GlobalPosition;
            Vector3 toTarget = targetPoint - query.Origin;
            toTarget.Y = 0.0f;
            float distance = toTarget.Length();
            Vector3 attackDirection = distance <= 0.001f ? forward : toTarget / distance;
            float score = forward.Dot(attackDirection) * 100.0f - distance;
            hits.Add(new AttackHitResult(target, hurtbox, score, distance, targetPoint));
        }

        hits.Sort((left, right) => right.Score.CompareTo(left.Score));
        if (hits.Count > query.Attack.TargetCap)
            hits.RemoveRange(query.Attack.TargetCap, hits.Count - query.Attack.TargetCap);

        return hits.ToArray();
    }

    public AttackHitResult? PreviewBestTarget(AttackQuery query)
    {
        var hits = QueryTargets(query);
        return hits.Length > 0 ? hits[0] : null;
    }

    public bool CanConnect(AttackQuery query)
    {
        return QueryTargets(query).Length > 0;
    }

    private static bool HasClearPath(ICombatant attacker, ICombatant target, Vector3 origin)
    {
        if (attacker?.CombatNode == null || target?.CombatNode == null)
            return false;

        var world = attacker.CombatNode.GetWorld3D();
        if (world == null)
            return false;

        Vector3 targetPoint = target.Hurtbox?.GlobalPosition ?? target.CombatNode.GlobalPosition;
        var ray = PhysicsRayQueryParameters3D.Create(origin, targetPoint);
        ray.CollideWithAreas = false;
        ray.CollideWithBodies = true;
        ray.CollisionMask = CombatLayers.WorldBodies | CombatLayers.ActorBodies;
        var exclude = new Godot.Collections.Array<Rid>();
        if (attacker.CombatNode is CollisionObject3D attackerBody)
            exclude.Add(attackerBody.GetRid());
        if (target.CombatNode is CollisionObject3D targetBody)
            exclude.Add(targetBody.GetRid());
        ray.Exclude = exclude;

        var hit = world.DirectSpaceState.IntersectRay(ray);
        return hit.Count == 0;
    }
}
