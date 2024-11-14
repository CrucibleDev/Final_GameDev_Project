using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralGrid : MonoBehaviour
{
    public float width = 10f;
    public float height = 10f;
    public int resolution = 1;

    [Header("Noise Settings")]
    public float noiseScale = 1f;
    public int octaves = 4;
    public float amplitude = 5f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Flattened Zones Settings")]
    public int numberOfFlattenedPoints = 8;   // Number of random flattened zones
    public float flattenRadius = 2f;          // Radius for flattening areas

    [Header("Path Settings")]
    public float pathWidth = 1f;              // Width of the path

    public bool debug = false;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private MeshCollider meshCollider;
    private List<Vector2> flattenedPoints = new List<Vector2>();  // List to store random flattened points

    void Start()
    {
        GenerateFlattenedPoints();
        GenerateGrid();
        AddMeshCollider();
    }

    void GenerateFlattenedPoints()
    {
        // Generate random flattened points
        flattenedPoints.Clear();
        for (int i = 0; i < numberOfFlattenedPoints; i++)
        {
            float x = Random.Range(0, width);
            float z = Random.Range(0, height);
            flattenedPoints.Add(new Vector2(x, z));
        }
    }

    void GenerateGrid()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Calculate vertices - no need to multiply width/height by resolution
        int verticesPerWidth = resolution + 1;
        int verticesPerHeight = resolution + 1;
        
        vertices = new Vector3[verticesPerWidth * verticesPerHeight];
        Vector3 center = new Vector3(width / 2, 0, height / 2);
        float maxDistance = Vector3.Distance(center, new Vector3(width, 0, height));

        // Calculate step size
        float stepX = width / resolution;  // Changed from (verticesPerWidth - 1)
        float stepZ = height / resolution; // Changed from (verticesPerHeight - 1)

        for (int i = 0, z = 0; z < verticesPerHeight; z++)
        {
            for (int x = 0; x < verticesPerWidth; x++, i++)
            {
                float xPos = x * stepX;
                float zPos = z * stepZ;
                
                float distFromCenter = Vector3.Distance(center, new Vector3(xPos, 0, zPos)) / maxDistance;
                float falloff = Mathf.Pow(1 - distFromCenter, 2);
                falloff = Mathf.Clamp01(falloff);

                float y = GenerateNoise(xPos, zPos) * falloff;

                y = ApplyFlattenedZones(xPos, zPos, y);
                y = ApplyPath(xPos, zPos, y);

                vertices[i] = new Vector3(xPos, y, zPos);

                if (debug)
                {
                    CreateDebugSphere(new Vector3(xPos, 10, zPos), falloff);
                }
            }
        }

        // Update triangle generation for new resolution
        triangles = new int[(verticesPerWidth - 1) * (verticesPerHeight - 1) * 6];
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

        UpdateMesh();
    }

    float GenerateNoise(float x, float z)
    {
        float total = 0;
        float frequency = 1;
        float amplitudeFactor = 1;
        float maxPossibleHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            // Adjust noise coordinates to account for physical size
            float xCoord = (x / width) * noiseScale * frequency;
            float zCoord = (z / height) * noiseScale * frequency;

            float noiseValue = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;
            total += noiseValue * amplitudeFactor;

            maxPossibleHeight += amplitudeFactor;
            amplitudeFactor *= persistence;
            frequency *= lacunarity;
        }

        return total / maxPossibleHeight * amplitude;
    }

    float ApplyFlattenedZones(float x, float z, float currentY)
    {
        foreach (Vector2 flatPoint in flattenedPoints)
        {
            Vector2 pos = new Vector2(x, z);
            float dist = Vector2.Distance(pos, flatPoint);

            if (dist < flattenRadius)
            {
                float flattenEffect = Mathf.Clamp01(1 - (dist / flattenRadius));
                currentY = Mathf.Lerp(currentY, 0, flattenEffect);
            }
        }
        return currentY;
    }

    float ApplyPath(float x, float z, float currentY)
    {
        for (int i = 0; i < flattenedPoints.Count - 1; i++)
        {
            Vector2 start = flattenedPoints[i];
            Vector2 end = flattenedPoints[i + 1];

            Vector2 pos = new Vector2(x, z);
            float distToSegment = DistanceToLineSegment(pos, start, end);

            if (distToSegment < pathWidth)
            {
                float pathEffect = Mathf.Clamp01(1 - (distToSegment / pathWidth));
                currentY = Mathf.Lerp(currentY, 0, pathEffect);
            }
        }
        return currentY;
    }

    float DistanceToLineSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        Vector2 projected = Vector2.ClampMagnitude(Vector2.Dot(point - start, segment.normalized) * segment.normalized, segment.magnitude);
        return (point - (start + projected)).magnitude;
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void AddMeshCollider()
    {
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.sharedMesh = mesh;
    }

    void CreateDebugSphere(Vector3 position, float value)
    {
        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = Vector3.one * 0.5f;
        debugSphere.GetComponent<Renderer>().material.color = new Color(value, value, value);
        debugSphere.transform.parent = transform;
    }
}
