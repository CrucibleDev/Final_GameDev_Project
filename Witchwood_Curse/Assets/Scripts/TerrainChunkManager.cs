using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TerrainChunkManager : MonoBehaviour
{
    [Header("Terrain Settings")]
    public TerrainChunkSettings settings;
    public GameObject chunkPrefab;
    public int viewDistance = 3;
    public int seed = 0;
    public bool generateStaticTerrain = false;

    [Header("Tree Settings")]
    public GameObject treePrefab;
    [Range(0f, 1f)]
    public float treeDensity = 0.1f;
    public float minTreeDistance = 2f;
    public float maxTreeTilt = 15f;
    public Mesh treeMesh;
    public Material treeMaterial;
    public bool useTreeColliders = false;

    [SerializeField] private Transform playerTransform;
    [SerializeField] private int maxResolution = 32; // Set this to your desired max resolution

    // Cached components and values
    private readonly Dictionary<Vector2Int, TerrainChunk> chunks = new Dictionary<Vector2Int, TerrainChunk>(50);
    private readonly Stack<TerrainChunk> chunkPool = new Stack<TerrainChunk>(50);
    private List<Vector2> globalFlattenedPoints = new List<Vector2>(20);
    private readonly Dictionary<GameObject, List<Matrix4x4>> instancedTrees = new Dictionary<GameObject, List<Matrix4x4>>(50);
    private readonly HashSet<Vector2Int> newChunkCoords = new HashSet<Vector2Int>(50);
    
    private Vector2Int currentChunkCoord;
    private bool initialChunksGenerated;
    
    // Cached calculations
    private float worldSize;
    private float edgeBuffer;
    private Vector2Int[] chunkOffsets;

    private void Awake()
    {
        InitializeCachedValues();
    }

    private void InitializeCachedValues()
    {
        worldSize = settings.chunkSize * (viewDistance * 2 + 1);
        edgeBuffer = settings.chunkSize * 0.5f;
        
        // Pre-calculate chunk offsets
        chunkOffsets = new Vector2Int[(viewDistance * 2 + 1) * (viewDistance * 2 + 1)];
        int index = 0;
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                chunkOffsets[index++] = new Vector2Int(x, z);
            }
        }
    }

    private void Start()
    {
        Random.InitState(seed);
        GenerateGlobalFlattenedPoints();
        
        if (generateStaticTerrain)
        {
            UpdateChunks();
            initialChunksGenerated = true;
        }
        else
        {
            UpdateChunks();
        }
    }

    private void Update()
    {
        if (generateStaticTerrain && initialChunksGenerated) return;

        Vector2Int newChunkCoord = GetChunkCoordFromPosition(GetPlayerPosition());
        if (newChunkCoord != currentChunkCoord)
        {
            currentChunkCoord = newChunkCoord;
            UpdateChunks();
            
            if (generateStaticTerrain)
            {
                initialChunksGenerated = true;
            }
        }
    }

    private void UpdateChunks()
    {
        newChunkCoords.Clear();

        // Use pre-calculated offsets
        foreach (Vector2Int offset in chunkOffsets)
        {
            Vector2Int coord = currentChunkCoord + offset;
            newChunkCoords.Add(coord);

            if (!chunks.ContainsKey(coord))
            {
                CreateChunk(coord);
            }
        }

        // Remove out-of-range chunks
        foreach (var chunk in chunks.Where(c => !newChunkCoords.Contains(c.Key)).ToList())
        {
            ReturnChunkToPool(chunk.Key);
        }
    }

    private void CreateChunk(Vector2Int coord)
    {
        TerrainChunk chunk;
        Vector3 position = new Vector3(coord.x * settings.chunkSize, 0, coord.y * settings.chunkSize);

        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Pop();
            chunk.transform.position = position;
            chunk.gameObject.SetActive(true);
        }
        else
        {
            GameObject chunkObject = Instantiate(chunkPrefab, position, Quaternion.identity, transform);
            chunk = chunkObject.GetComponent<TerrainChunk>();
        }

        chunk.Initialize(new Vector2(position.x, position.z), settings, this);
        chunks.Add(coord, chunk);
    }

    private void ReturnChunkToPool(Vector2Int coord)
    {
        TerrainChunk chunk = chunks[coord];
        chunk.gameObject.SetActive(false);
        instancedTrees.Remove(chunk.gameObject);
        chunkPool.Push(chunk);
        chunks.Remove(coord);
    }

    void GenerateGlobalFlattenedPoints()
    {
        globalFlattenedPoints.Clear();
        List<Vector2> points = new List<Vector2>();
        
        float worldSize = settings.chunkSize * (viewDistance * 2 + 1);
        float edgeBuffer = settings.chunkSize * 0.5f;
        
        Vector3 centerPos = new Vector3(
            currentChunkCoord.x * settings.chunkSize + (settings.chunkSize * 0.5f),
            0,
            currentChunkCoord.y * settings.chunkSize + (settings.chunkSize * 0.5f)
        );

        float minX = centerPos.x - (worldSize/2) + edgeBuffer;
        float maxX = centerPos.x + (worldSize/2) - edgeBuffer;
        float minZ = centerPos.z - (worldSize/2) + edgeBuffer;
        float maxZ = centerPos.z + (worldSize/2) - edgeBuffer;

        float minDistanceBetweenPoints = settings.minPointDistance;
        int maxAttempts = 50;

        for (int i = 0; i < settings.numberOfFlattenedPoints; i++)
        {
            bool validPointFound = false;
            int attempts = 0;

            while (!validPointFound && attempts < maxAttempts)
            {
                float x = Random.Range(minX + edgeBuffer, maxX - edgeBuffer);
                float z = Random.Range(minZ + edgeBuffer, maxZ - edgeBuffer);
                Vector2 newPoint = new Vector2(x, z);

                bool tooClose = false;
                foreach (Vector2 existingPoint in points)
                {
                    if (Vector2.Distance(newPoint, existingPoint) < minDistanceBetweenPoints)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    points.Add(newPoint);
                    validPointFound = true;
                }

                attempts++;
            }

            if (!validPointFound)
            {
                Debug.LogWarning($"Could not place point {i} after {maxAttempts} attempts. Skipping.");
            }
        }

        if (points.Count >= 2)
        {
            int startIndex = 0;
            float leftmostX = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].x < leftmostX)
                {
                    leftmostX = points[i].x;
                    startIndex = i;
                }
            }

            List<Vector2> orderedPoints = new List<Vector2>();
            List<Vector2> remainingPoints = new List<Vector2>(points);
            
            orderedPoints.Add(remainingPoints[startIndex]);
            remainingPoints.RemoveAt(startIndex);

            while (remainingPoints.Count > 0)
            {
                Vector2 currentPoint = orderedPoints[orderedPoints.Count - 1];
                float minDist = float.MaxValue;
                int nearestIndex = 0;

                for (int i = 0; i < remainingPoints.Count; i++)
                {
                    float dist = Vector2.Distance(currentPoint, remainingPoints[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestIndex = i;
                    }
                }

                orderedPoints.Add(remainingPoints[nearestIndex]);
                remainingPoints.RemoveAt(nearestIndex);
            }

            globalFlattenedPoints = orderedPoints;
        }

        CreateDebugVisuals();
    }

    void CreateDebugVisuals()
    {
        Transform existingDebug = transform.Find("DebugPoints");
        if (existingDebug != null)
            Destroy(existingDebug.gameObject);

        GameObject debugContainer = new GameObject("DebugPoints");
        debugContainer.transform.parent = transform;

        // Create invisible walls for flattened zones
        foreach (Vector2 point in globalFlattenedPoints)
        {
            GameObject cylinder = new GameObject($"ZoneBoundary_{point}");
            cylinder.transform.parent = debugContainer.transform;
            float y = GenerateNoise(point.x, point.y);
            cylinder.transform.position = new Vector3(point.x, y + 5f, point.y);
            
            // Add cylinder collider
            CapsuleCollider collider = cylinder.AddComponent<CapsuleCollider>();
            collider.radius = settings.flattenRadius;
            collider.height = 20f;  // Tall enough to contain player
            collider.isTrigger = true;
            collider.direction = 1; // Orient vertically (Y-axis)
            
            // Add boundary behavior
            cylinder.AddComponent<BoundaryTrigger>();
        }

        // Create path boundaries
        for (int i = 0; i < globalFlattenedPoints.Count - 1; i++)
        {
            Vector2 start = globalFlattenedPoints[i];
            Vector2 end = globalFlattenedPoints[i + 1];
            
            GameObject pathWall = new GameObject($"PathWall_{i}");
            pathWall.transform.parent = debugContainer.transform;
            
            // Calculate path properties
            Vector2 pathDirection = (end - start).normalized;
            float pathLength = Vector2.Distance(start, end);
            float averageY = (GenerateNoise(start.x, start.y) + GenerateNoise(end.x, end.y)) / 2f;
            Vector3 pathCenter = new Vector3(
                (start.x + end.x) / 2f,
                averageY + 5f,
                (start.y + end.y) / 2f
            );
            
            // Create box collider
            BoxCollider boxCollider = pathWall.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(settings.pathWidth * 2, 20f, pathLength);
            pathWall.transform.position = pathCenter;
            
            // Rotate to align with path
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            pathWall.transform.rotation = Quaternion.Euler(0, -angle + 90, 0);
            
            // Add boundary behavior
            pathWall.AddComponent<BoundaryTrigger>();
        }

        // Create spheres for points
        foreach (Vector2 point in globalFlattenedPoints)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = debugContainer.transform;
            float y = GenerateNoise(point.x, point.y);
            sphere.transform.position = new Vector3(point.x, y + 1f, point.y);
            sphere.transform.localScale = Vector3.one * settings.flattenRadius;

            Renderer renderer = sphere.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1, 0, 0, 0.3f);
            renderer.material = material;
        }

        // Draw path connections
        for (int i = 0; i < globalFlattenedPoints.Count - 1; i++)
        {
            Vector2 start = globalFlattenedPoints[i];
            Vector2 end = globalFlattenedPoints[i + 1];
            
            GameObject pathSegment = new GameObject($"PathSegment_{i}");
            pathSegment.transform.parent = debugContainer.transform;
            
            LineRenderer lineRenderer = pathSegment.AddComponent<LineRenderer>();
            lineRenderer.startWidth = settings.pathWidth;
            lineRenderer.endWidth = settings.pathWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(1f, 1f, 0f, 0.5f);
            lineRenderer.endColor = new Color(1f, 1f, 0f, 0.5f);
            
            float startY = GenerateNoise(start.x, start.y);
            float endY = GenerateNoise(end.x, end.y);
            
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] {
                new Vector3(start.x, startY + 0.5f, start.y),
                new Vector3(end.x, endY + 0.5f, end.y)
            });
        }
    }

    public float GetTerrainHeight(float x, float z)
    {
        float height = GenerateNoise(x, z);
        height = ApplyPath(x, z, height);
        height = ApplyFlattenedZones(x, z, height);
        return height;
    }

    float GenerateNoise(float x, float z)
    {
        float amplitude = settings.amplitude;
        float frequency = 1f;
        float noiseHeight = 0;
        float amplitudeSum = 0;

        // Make sure x and z are used correctly in the noise function
        float worldX = x;
        float worldZ = z;

        for (int i = 0; i < settings.octaves; i++)
        {
            // Use x for x and z for z consistently
            float sampleX = (worldX * frequency * settings.noiseScale) / 100f;
            float sampleZ = (worldZ * frequency * settings.noiseScale) / 100f;

            float perlinValue = Mathf.PerlinNoise(sampleX + seed, sampleZ + seed) * 2 - 1;
            noiseHeight += perlinValue * amplitude;
            amplitudeSum += amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        return (noiseHeight / amplitudeSum) * settings.amplitude;
    }

    float ApplyFlattenedZones(float x, float z, float currentY)
    {
        Vector2 pos = new Vector2(x, z);
        float originalY = currentY;
        float totalWeight = 0f;
        float weightedHeight = 0f;

        foreach (Vector2 flatPoint in globalFlattenedPoints)
        {
            float dist = Vector2.Distance(pos, flatPoint);
            if (dist < settings.flattenRadius)
            {
                float weight = 1f - (dist / settings.flattenRadius);
                weight = Mathf.SmoothStep(0, 1, weight);
                float targetHeight = GenerateNoise(flatPoint.x, flatPoint.y);
                weightedHeight += targetHeight * weight;
                totalWeight += weight;
            }
        }

        if (totalWeight > 0)
        {
            float averageHeight = weightedHeight / totalWeight;
            return Mathf.Lerp(originalY, averageHeight, Mathf.Clamp01(totalWeight));
        }

        return originalY;
    }

    float ApplyPath(float x, float z, float currentY)
    {
        Vector2 pos = new Vector2(x, z);

        for (int i = 0; i < globalFlattenedPoints.Count - 1; i++)
        {
            Vector2 start = globalFlattenedPoints[i];
            Vector2 end = globalFlattenedPoints[i + 1];

            float distToSegment = DistanceToLineSegment(pos, start, end);
            if (distToSegment < settings.pathWidth)
            {
                float pathEffect = 1f - (distToSegment / settings.pathWidth);
                pathEffect = Mathf.SmoothStep(0, 1, pathEffect);
                
                float startHeight = GenerateNoise(start.x, start.y);
                float endHeight = GenerateNoise(end.x, end.y);
                float t = Vector2.Distance(pos, start) / Vector2.Distance(start, end);
                float targetHeight = Mathf.Lerp(startHeight, endHeight, t);
                
                currentY = Mathf.Lerp(currentY, targetHeight, pathEffect);
            }
        }

        return currentY;
    }

    float DistanceToLineSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 line = end - start;
        float len = line.magnitude;
        if (len == 0f) return Vector2.Distance(point, start);

        float t = Mathf.Clamp01(Vector2.Dot(point - start, line) / (len * len));
        Vector2 projection = start + line * t;
        
        return Vector2.Distance(point, projection);
    }

    Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / settings.chunkSize);
        int z = Mathf.FloorToInt(position.z / settings.chunkSize);
        return new Vector2Int(x, z);
    }

    public bool IsPointInBoundary(Vector2 point)
    {
        // Check flattened zones
        foreach (Vector2 flatPoint in globalFlattenedPoints)
        {
            if (Vector2.Distance(point, flatPoint) < settings.flattenRadius)
            {
                return true;
            }
        }

        // Check paths
        for (int i = 0; i < globalFlattenedPoints.Count - 1; i++)
        {
            Vector2 start = globalFlattenedPoints[i];
            Vector2 end = globalFlattenedPoints[i + 1];
            float distToPath = DistanceToLineSegment(point, start, end);
            if (distToPath < settings.pathWidth)
            {
                return true;
            }
        }

        return false;
    }

    public void AddInstancedRenderers(GameObject chunk, List<Matrix4x4> matrices)
    {
        instancedTrees[chunk] = matrices;
    }

    private void LateUpdate()
    {
        // Render all instanced trees
        foreach (var kvp in instancedTrees)
        {
            List<Matrix4x4> matrices = kvp.Value;
            const int batchSize = 1023;
            
            for (int i = 0; i < matrices.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, matrices.Count - i);
                Graphics.DrawMeshInstanced(treeMesh, 0, treeMaterial, 
                    matrices.GetRange(i, count).ToArray());
            }
        }
    }

    public Vector3 GetPlayerPosition()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Make sure player has 'Player' tag or assign reference in inspector.");
                return Vector3.zero;
            }
        }
        return playerTransform.position;
    }

    public int GetMaxResolution()
    {
        return maxResolution;
    }
} 