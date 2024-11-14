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
    private MeshRenderer meshRenderer;


    public void Initialize(Vector2 pos, TerrainChunkSettings settings, TerrainChunkManager terrainManager)
    {
        position = pos;
        manager = terrainManager;
        width = settings.chunkSize;
        height = settings.chunkSize;
        resolution = settings.resolution;
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Force enable renderer initially
        meshRenderer.enabled = true;
        
        // Generate initial mesh and trees
        GenerateGrid();
        AddMeshCollider();
        GenerateTrees();
        
        Debug.Log($"Initialized chunk at position {pos} with resolution {resolution}");
    }

    private void Update()
    {
        UpdateChunkState();
    }

    private void UpdateChunkState()
    {
        if (Camera.main == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }

        Vector3 playerPosition = manager.GetPlayerPosition();
        Vector3 chunkCenter = transform.position + new Vector3(width/2f, 0, height/2f);
        Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);
        Vector2 chunkPos2D = new Vector2(chunkCenter.x, chunkCenter.z);
        
        float chunkSize = width;
        float distanceFromPlayer = Vector2.Distance(playerPos2D, chunkPos2D);
        int chunksAway = Mathf.FloorToInt(distanceFromPlayer / chunkSize * 1.5f);

        int originalResolution = resolution;
        int targetResolution = originalResolution;

        if (chunksAway <= 1) // Only current chunk and direct neighbors
        {
            targetResolution = manager.GetMaxResolution();
            if (manager.showDebugVisuals)
            {
                Debug.DrawLine(chunkCenter, playerPosition, Color.green, 0.1f);
            }
        }
        else if (chunksAway <= 2) // Second ring
        {
            targetResolution = Mathf.Max(8, manager.GetMaxResolution() / 2);
            Debug.DrawLine(chunkCenter, playerPosition, Color.yellow, 0.1f);
        }
        else if (chunksAway <= 3)
        {
            targetResolution = Mathf.Max(4, manager.GetMaxResolution() / 4);
            Debug.DrawLine(chunkCenter, playerPosition, Color.red, 0.1f);
        }
        // else if (chunksAway <= 4)
        // {
        //     targetResolution = Mathf.Max(2, manager.GetMaxResolution() / 8);
        //     Debug.DrawLine(chunkCenter, playerPosition, Color.blue, 0.1f);
        // }
        else
        {
            if (meshRenderer.enabled)
            {
                meshRenderer.enabled = false;
                if (meshCollider != null) meshCollider.enabled = false;
            }
            return;
        }

        if (!meshRenderer.enabled)
        {
            meshRenderer.enabled = true;
            if (meshCollider != null) meshCollider.enabled = true;
        }

        if (targetResolution != originalResolution)
        {
            resolution = targetResolution;
            GenerateGrid();
            AddMeshCollider();
            GenerateTrees();
            Debug.Log($"Updated chunk at {position} from res {originalResolution} to {targetResolution} (chunks away: {chunksAway})");
        }
    }

    void GenerateGrid()
    {
        // Validate resolution
        if (resolution <= 0)
        {
            resolution = 1;
            Debug.LogWarning("Invalid resolution value, defaulting to 1");
        }

        int verticesPerWidth = resolution + 1;
        int verticesPerHeight = resolution + 1;
        int totalVertices = verticesPerWidth * verticesPerHeight;
        
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().mesh = mesh;
        }
        
        Vector3[] vertices = new Vector3[totalVertices];
        Vector3[] normals = new Vector3[totalVertices];
        float stepX = Mathf.Max(0.001f, width / resolution);
        float stepZ = Mathf.Max(0.001f, height / resolution);
        
        float posX = position.x;
        float posZ = position.y;

        // Validate manager
        if (manager == null)
        {
            Debug.LogError("TerrainChunkManager is null!");
            return;
        }

        bool hasInvalidVertices = false;

        // Generate vertices and calculate normals
        for (int i = 0, z = 0; z < verticesPerHeight; z++)
        {
            for (int x = 0; x < verticesPerWidth; x++, i++)
            {
                try
                {
                    float xPos = x * stepX;
                    float zPos = z * stepZ;
                    float worldX = posX + xPos;
                    float worldZ = posZ + zPos;
                    
                    // Get and validate height
                    float y = manager.GetTerrainHeight(worldX, worldZ);
                    if (float.IsNaN(y) || float.IsInfinity(y))
                    {
                        y = 0f;
                        hasInvalidVertices = true;
                    }
                    y = Mathf.Clamp(y, -10000f, 10000f);
                    
                    vertices[i] = new Vector3(xPos, y, zPos);

                    // Calculate and validate normal
                    float hL = ValidateHeight(manager.GetTerrainHeight(worldX - stepX, worldZ), y);
                    float hR = ValidateHeight(manager.GetTerrainHeight(worldX + stepX, worldZ), y);
                    float hD = ValidateHeight(manager.GetTerrainHeight(worldX, worldZ - stepZ), y);
                    float hU = ValidateHeight(manager.GetTerrainHeight(worldX, worldZ + stepZ), y);

                    Vector3 normal = CalculateNormal(hL, hR, hD, hU, stepX, stepZ);
                    normals[i] = normal;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error generating vertex at {x}, {z}: {e.Message}");
                    vertices[i] = Vector3.zero;
                    normals[i] = Vector3.up;
                    hasInvalidVertices = true;
                }
            }
        }

        if (hasInvalidVertices)
        {
            Debug.LogWarning("Some vertices were invalid and were replaced with default values");
        }

        try
        {
            // Generate triangles
            int[] triangles = new int[resolution * resolution * 6];
            int tIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int vertexIndex = z * verticesPerWidth + x;
                    
                    triangles[tIndex] = vertexIndex;
                    triangles[tIndex + 1] = vertexIndex + verticesPerWidth;
                    triangles[tIndex + 2] = vertexIndex + 1;
                    
                    triangles[tIndex + 3] = vertexIndex + 1;
                    triangles[tIndex + 4] = vertexIndex + verticesPerWidth;
                    triangles[tIndex + 5] = vertexIndex + verticesPerWidth + 1;
                    
                    tIndex += 6;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            
            // Validate mesh bounds
            Bounds bounds = mesh.bounds;
            if (float.IsNaN(bounds.min.x) || float.IsNaN(bounds.min.y) || float.IsNaN(bounds.min.z) ||
                float.IsNaN(bounds.max.x) || float.IsNaN(bounds.max.y) || float.IsNaN(bounds.max.z))
            {
                Debug.LogError("Invalid mesh bounds detected, forcing recalculation");
                mesh.RecalculateBounds();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error finalizing mesh: {e.Message}");
        }
    }

    private float ValidateHeight(float height, float defaultValue)
    {
        if (float.IsNaN(height) || float.IsInfinity(height))
        {
            return defaultValue;
        }
        return Mathf.Clamp(height, -10000f, 10000f);
    }

    private Vector3 CalculateNormal(float hL, float hR, float hD, float hU, float stepX, float stepZ)
    {
        Vector3 normal = new Vector3(
            (hL - hR) / (2 * stepX),
            2.0f,
            (hD - hU) / (2 * stepZ)
        );

        // Validate normal
        if (float.IsNaN(normal.x) || float.IsNaN(normal.y) || float.IsNaN(normal.z) ||
            float.IsInfinity(normal.x) || float.IsInfinity(normal.y) || float.IsInfinity(normal.z))
        {
            return Vector3.up;
        }

        normal = normal.normalized;
        if (normal == Vector3.zero)
        {
            return Vector3.up;
        }

        return normal;
    }

    void AddMeshCollider()
    {
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void GenerateTrees()
    {
        // Cache transform lookup
        Transform existingTrees = transform.Find("Trees");
        if (existingTrees != null)
            Destroy(existingTrees.gameObject);

        // Create new tree container
        GameObject treeContainer = new GameObject("Trees");
        treeContainer.transform.parent = transform;
        treeContainer.transform.localPosition = Vector3.zero;

        // Pre-calculate list capacities
        int estimatedTrees = Mathf.CeilToInt((width * height) * manager.treeDensity);
        List<Matrix4x4> matrices = new List<Matrix4x4>(estimatedTrees);
        List<Vector3> positions = new List<Vector3>(estimatedTrees);

        float stepSize = 1f / manager.treeDensity;
        System.Random random = new System.Random(manager.seed + GetHashCode());

        // Cache frequently accessed values
        float halfStepSize = stepSize * 0.5f;
        float posX = position.x;
        float posZ = position.y;

        for (float x = 0; x < width; x += stepSize)
        {
            for (float z = 0; z < height; z += stepSize)
            {
                float randomX = x + ((float)random.NextDouble() - 0.5f) * stepSize;
                float randomZ = z + ((float)random.NextDouble() - 0.5f) * stepSize;

                float worldX = posX + randomX;
                float worldZ = posZ + randomZ;

                if (manager.IsPointInBoundary(new Vector2(worldX, worldZ)))
                    continue;

                Vector3 worldPos = new Vector3(worldX, 0, worldZ);

                // Use squared distance for performance
                float minDistSqr = manager.minTreeDistance * manager.minTreeDistance;
                bool tooClose = false;
                foreach (Vector3 existingPos in positions)
                {
                    if ((worldPos - existingPos).sqrMagnitude < minDistSqr)
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
                
                colliderObj.layer = LayerMask.NameToLayer("Trees");
            }
        }
    }
}