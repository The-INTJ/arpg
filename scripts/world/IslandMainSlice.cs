using System.Collections.Generic;
using Godot;

namespace ARPG;

[Tool]
public partial class IslandMainSlice : Node3D
{
    private static StandardMaterial3D _fallbackTerrainMaterial;

    [Export]
    public bool ApplyFallbackTerrainMaterial { get; set; } = true;

    [Export]
    public bool AlignVisualTopToGround { get; set; }

    [Export]
    public bool ForceTerrainMaterialOverride { get; set; }

    [Export]
    public bool UseDoubleSidedTerrainMaterial { get; set; }

    [Export]
    public bool EnableTunnelInteriorGlow { get; set; }

    public override void _Ready()
    {
        RefreshTerrain();
    }

    public void RefreshTerrain()
    {
        var terrainBody = GetNodeOrNull<StaticBody3D>("TerrainBody");
        var visualRoot = GetNodeOrNull<Node3D>("TerrainBody/VisualRoot");
        var collision = GetNodeOrNull<CollisionShape3D>("TerrainBody/CollisionShape3D");
        if (terrainBody == null || visualRoot == null || collision == null)
            return;

        if (!terrainBody.IsInGroup(WorldGroups.CameraBlockers))
            terrainBody.AddToGroup(WorldGroups.CameraBlockers);

        if (AlignVisualTopToGround)
            AlignVisualRootToGround(visualRoot);

        if (ApplyFallbackTerrainMaterial)
            ApplyFallbackMaterialRecursive(visualRoot, forceOverride: ForceTerrainMaterialOverride);

        if (EnableTunnelInteriorGlow)
            RefreshTunnelInteriorGlow(visualRoot);

        // Generated faces are already expressed in TerrainBody local space.
        // Keep the collision node itself untransformed so the collider matches the mesh exactly.
        collision.Transform = Transform3D.Identity;
        collision.Shape = BuildCollisionShape(visualRoot);
    }

    private static void AlignVisualRootToGround(Node3D visualRoot)
    {
        if (!TryGetTopY(visualRoot, Transform3D.Identity, out float topY) || Mathf.IsZeroApprox(topY))
            return;

        visualRoot.Position -= Vector3.Up * topY;
    }

    private static Shape3D BuildCollisionShape(Node3D visualRoot)
    {
        var faces = new List<Vector3>();
        AppendCollisionFaces(visualRoot, Transform3D.Identity, faces);
        if (faces.Count == 0)
            return null;

        var shape = new ConcavePolygonShape3D();
        shape.Data = faces.ToArray();
        shape.BackfaceCollision = true;
        return shape;
    }

    private static bool TryGetTopY(Node node, Transform3D parentTransform, out float topY)
    {
        bool found = false;
        topY = 0.0f;
        AppendTopY(node, parentTransform, ref found, ref topY);
        return found;
    }

    private static void AppendTopY(Node node, Transform3D parentTransform, ref bool found, ref float topY)
    {
        Transform3D currentTransform = parentTransform;
        if (node is Node3D node3D)
            currentTransform = parentTransform * node3D.Transform;

        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
            AppendMeshTopY(meshInstance.Mesh.GetAabb(), currentTransform, ref found, ref topY);

        foreach (Node child in node.GetChildren())
            AppendTopY(child, currentTransform, ref found, ref topY);
    }

    private static void AppendMeshTopY(Aabb localBounds, Transform3D transform, ref bool found, ref float topY)
    {
        foreach (Vector3 corner in EnumerateAabbCorners(localBounds))
        {
            float y = (transform * corner).Y;
            if (!found || y > topY)
            {
                topY = y;
                found = true;
            }
        }
    }

    private static IEnumerable<Vector3> EnumerateAabbCorners(Aabb localBounds)
    {
        Vector3 origin = localBounds.Position;
        Vector3 size = localBounds.Size;

        yield return origin;
        yield return origin + new Vector3(size.X, 0, 0);
        yield return origin + new Vector3(0, size.Y, 0);
        yield return origin + new Vector3(0, 0, size.Z);
        yield return origin + new Vector3(size.X, size.Y, 0);
        yield return origin + new Vector3(size.X, 0, size.Z);
        yield return origin + new Vector3(0, size.Y, size.Z);
        yield return origin + size;
    }

    private static void AppendCollisionFaces(Node node, Transform3D parentTransform, List<Vector3> faces)
    {
        Transform3D currentTransform = parentTransform;
        if (node is Node3D node3D)
            currentTransform = parentTransform * node3D.Transform;

        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
            AppendMeshFaces(meshInstance.Mesh, currentTransform, faces);

        foreach (Node child in node.GetChildren())
            AppendCollisionFaces(child, currentTransform, faces);
    }

    private static void AppendMeshFaces(Mesh mesh, Transform3D transform, List<Vector3> faces)
    {
        for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
        {
            var arrays = mesh.SurfaceGetArrays(surface);
            var vertices = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            if (vertices.Length == 0)
                continue;

            var indices = (int[])arrays[(int)Mesh.ArrayType.Index];
            if (indices.Length >= 3)
            {
                for (int i = 0; i + 2 < indices.Length; i += 3)
                    AppendTriangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]], transform, faces);

                continue;
            }

            for (int i = 0; i + 2 < vertices.Length; i += 3)
                AppendTriangle(vertices[i], vertices[i + 1], vertices[i + 2], transform, faces);
        }
    }

    private static void AppendTriangle(Vector3 a, Vector3 b, Vector3 c, Transform3D transform, List<Vector3> faces)
    {
        faces.Add(transform * a);
        faces.Add(transform * b);
        faces.Add(transform * c);
    }

    private void ApplyFallbackMaterialRecursive(Node node, bool forceOverride)
    {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null && (forceOverride || !HasMaterial(meshInstance)))
            meshInstance.MaterialOverride = GetFallbackTerrainMaterial();

        foreach (Node child in node.GetChildren())
            ApplyFallbackMaterialRecursive(child, forceOverride);
    }

    private void RefreshTunnelInteriorGlow(Node3D visualRoot)
    {
        bool foundTunnelMesh = false;
        var tunnelBounds = default(Aabb);

        foreach (MeshInstance3D meshInstance in EnumerateMeshInstances(visualRoot))
        {
            if (!IsTunnelInteriorMesh(meshInstance))
                continue;

            meshInstance.MaterialOverlay = WorldMaterials.GetTunnelGlowOverlayMaterial();

            if (!TryGetMeshBoundsRelativeTo(visualRoot, meshInstance, out var meshBounds))
                continue;

            if (!foundTunnelMesh)
            {
                tunnelBounds = meshBounds;
                foundTunnelMesh = true;
                continue;
            }

            tunnelBounds = tunnelBounds.Merge(meshBounds);
        }

        var glowLight = visualRoot.GetNodeOrNull<OmniLight3D>("TunnelGlowLight");
        if (!foundTunnelMesh)
        {
            glowLight?.QueueFree();
            return;
        }

        glowLight ??= CreateTunnelGlowLight(visualRoot);
        glowLight.Position = tunnelBounds.GetCenter();
        glowLight.OmniRange = Mathf.Max(8.0f, Mathf.Max(tunnelBounds.Size.X, Mathf.Max(tunnelBounds.Size.Y, tunnelBounds.Size.Z)) * 0.35f);
    }

    private static bool HasMaterial(MeshInstance3D meshInstance)
    {
        if (meshInstance.MaterialOverride != null)
            return true;

        for (int surface = 0; surface < meshInstance.Mesh.GetSurfaceCount(); surface++)
        {
            if (meshInstance.GetSurfaceOverrideMaterial(surface) != null)
                return true;

            if (meshInstance.Mesh.SurfaceGetMaterial(surface) != null)
                return true;
        }

        return false;
    }

    private StandardMaterial3D GetFallbackTerrainMaterial()
    {
        if (UseDoubleSidedTerrainMaterial)
            return WorldMaterials.GetDoubleSidedPrimaryGroundMaterial();

        return _fallbackTerrainMaterial ??= WorldMaterials.CreatePrimaryGroundMaterial();
    }

    private static OmniLight3D CreateTunnelGlowLight(Node3D visualRoot)
    {
        var light = new OmniLight3D();
        light.Name = "TunnelGlowLight";
        light.LightColor = Palette.DarkEnergyGlow.Lightened(0.15f);
        light.LightEnergy = 1.8f;
        light.ShadowEnabled = false;
        visualRoot.AddChild(light);
        return light;
    }

    private static bool IsTunnelInteriorMesh(MeshInstance3D meshInstance)
    {
        return meshInstance.Name.ToString().Contains("Cylinder");
    }

    private static IEnumerable<MeshInstance3D> EnumerateMeshInstances(Node node)
    {
        if (node is MeshInstance3D meshInstance)
            yield return meshInstance;

        foreach (Node child in node.GetChildren())
        {
            foreach (MeshInstance3D nested in EnumerateMeshInstances(child))
                yield return nested;
        }
    }

    private static bool TryGetMeshBoundsRelativeTo(Node3D relativeTo, MeshInstance3D meshInstance, out Aabb bounds)
    {
        if (meshInstance.Mesh == null)
        {
            bounds = default;
            return false;
        }

        var localBounds = meshInstance.Mesh.GetAabb();
        bool found = false;
        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;

        foreach (Vector3 corner in EnumerateAabbCorners(localBounds))
        {
            Vector3 relativeCorner = relativeTo.ToLocal(meshInstance.ToGlobal(corner));
            if (!found)
            {
                min = relativeCorner;
                max = relativeCorner;
                found = true;
                continue;
            }

            min = min.Min(relativeCorner);
            max = max.Max(relativeCorner);
        }

        bounds = new Aabb(min, max - min);
        return found;
    }
}
