using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    private TerrainChunkManager manager;
    private Vector2 position;  // X, Z coordinates in world space
    private float width;
    private float height;
    private int resolution;
    private Mesh mesh;
    private MeshCollider meshCollider;

    public void Initialize(Vector2 pos, TerrainChunkSettings settings, TerrainChunkManager terrainManager)
    {
        position = pos;
        manager = terrainManager;
        width = settings.chunkSize;
        height = settings.chunkSize;
        resolution = settings.resolution;
        
        GenerateGrid();
        AddMeshCollider();
        GenerateTrees();
    }

    void GenerateGrid()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int verticesPerWidth = resolution + 1;
        int verticesPerHeight = resolution + 1;
        
        Vector3[] vertices = new Vector3[verticesPerWidth * verticesPerHeight];
        Vector3[] normals = new Vector3[vertices.Length];
        float stepX = width / resolution;
        float stepZ = height / resolution;

        // Generate vertices and calculate normals
        for (int i = 0, z = 0; z < verticesPerHeight; z++)
        {
            for (int x = 0; x < verticesPerWidth; x++, i++)
            {
                float xPos = x * stepX;
                float zPos = z * stepZ;
                // Use position.x and position.y consistently as world X and Z coordinates
                float worldX = position.x + xPos;
                float worldZ = position.y + zPos;
                
                float y = manager.GetTerrainHeight(worldX, worldZ);
                vertices[i] = new Vector3(xPos, y, zPos);

                // Calculate normal using central differences
                float hL = manager.GetTerrainHeight(worldX - stepX, worldZ);
                float hR = manager.GetTerrainHeight(worldX + stepX, worldZ);
                float hD = manager.GetTerrainHeight(worldX, worldZ - stepZ);
                float hU = manager.GetTerrainHeight(worldX, worldZ + stepZ);

                Vector3 normal = new Vector3(
                    (hL - hR) / (2 * stepX),
                    2.0f,
                    (hD - hU) / (2 * stepZ)
                ).normalized;

                normals[i] = normal;
            }
        }

        int[] triangles = new int[(verticesPerWidth - 1) * (verticesPerHeight - 1) * 6];
        for (int ti = 0, vi = 0, z = 0; z < verticesPerHeight - 1; z++, vi++)
        {
            for (int x = 0; x < verticesPerWidth - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + verticesPerWidth;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + verticesPerWidth;
                triangles[ti + 5] = vi + verticesPerWidth + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
    }

    void AddMeshCollider()
    {
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void GenerateTrees()
    {
        Transform existingTrees = transform.Find("Trees");
        if (existingTrees != null)
            Destroy(existingTrees.gameObject);

        GameObject treeContainer = new GameObject("Trees");
        treeContainer.transform.parent = transform;

        // Create lists to store tree data
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        List<Vector3> positions = new List<Vector3>(); // Store positions for collision checks

        float stepSize = 1f / manager.treeDensity;
        System.Random random = new System.Random(manager.seed + GetHashCode());

        // First pass: collect all valid positions
        for (float x = 0; x < width; x += stepSize)
        {
            for (float z = 0; z < height; z += stepSize)
            {
                float randomX = x + (float)(random.NextDouble() - 0.5f) * stepSize;
                float randomZ = z + (float)(random.NextDouble() - 0.5f) * stepSize;

                float worldX = position.x + randomX;
                float worldZ = position.y + randomZ;

                if (manager.IsPointInBoundary(new Vector2(worldX, worldZ)))
                    continue;

                Vector3 worldPos = new Vector3(worldX, 0, worldZ);

                // Check distance from other trees
                bool tooClose = false;
                foreach (Vector3 existingPos in positions)
                {
                    if (Vector3.Distance(worldPos, existingPos) < manager.minTreeDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Get terrain height
                float worldY = manager.GetTerrainHeight(worldX, worldZ);
                worldPos.y = worldY;

                // Calculate transform
                float tiltX = (float)(random.NextDouble() - 0.5f) * manager.maxTreeTilt;
                float tiltZ = (float)(random.NextDouble() - 0.5f) * manager.maxTreeTilt;
                float rotationY = random.Next(360);
                float scale = 0.8f + (float)random.NextDouble() * 0.4f;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    worldPos,
                    Quaternion.Euler(tiltX, rotationY, tiltZ),
                    Vector3.one * scale
                );

                matrices.Add(matrix);
                positions.Add(worldPos);
            }
        }

        // Create batches of 1023 trees (GPU instancing limit)
        const int batchSize = 1023;
        int batchCount = Mathf.CeilToInt(matrices.Count / (float)batchSize);

        // Store the renderers to prevent garbage collection
        manager.AddInstancedRenderers(gameObject, matrices);

        // Add colliders for physics interactions (optional)
        if (manager.useTreeColliders)
        {
            foreach (Vector3 pos in positions)
            {
                GameObject colliderObj = new GameObject("TreeCollider");
                colliderObj.transform.parent = treeContainer.transform;
                colliderObj.transform.position = pos;
                
                CapsuleCollider collider = colliderObj.AddComponent<CapsuleCollider>();
                collider.height = 2f; // Adjust based on your tree size
                collider.radius = 0.5f;
                collider.isTrigger = true;
                
                // Set the layer on the GameObject instead of the Collider
                colliderObj.layer = LayerMask.NameToLayer("Trees");
            }
        }
    }
}