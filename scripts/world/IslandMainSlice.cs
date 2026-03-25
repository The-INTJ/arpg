using System.Collections.Generic;
using Godot;

namespace ARPG;

[Tool]
public partial class IslandMainSlice : Node3D
{
    private static StandardMaterial3D _fallbackTerrainMaterial;

    [Export]
    public bool ApplyFallbackTerrainMaterial { get; set; } = true;

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

        if (ApplyFallbackTerrainMaterial)
            ApplyFallbackMaterialRecursive(visualRoot);

        collision.Shape = BuildCollisionShape(visualRoot);
    }

    private static Shape3D BuildCollisionShape(Node3D visualRoot)
    {
        var faces = new List<Vector3>();
        AppendCollisionFaces(visualRoot, Transform3D.Identity, faces);
        if (faces.Count == 0)
            return null;

        var shape = new ConcavePolygonShape3D();
        shape.Data = faces.ToArray();
        return shape;
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

    private static void ApplyFallbackMaterialRecursive(Node node)
    {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null && !HasMaterial(meshInstance))
            meshInstance.MaterialOverride = GetFallbackTerrainMaterial();

        foreach (Node child in node.GetChildren())
            ApplyFallbackMaterialRecursive(child);
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

    private static StandardMaterial3D GetFallbackTerrainMaterial()
    {
        return _fallbackTerrainMaterial ??= WorldMaterials.CreatePrimaryGroundMaterial();
    }
}
