using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralGrid : MonoBehaviour
{
    public int width = 10;          // Number of vertices along the width
    public int height = 10;         // Number of vertices along the height
    public float cellSize = 1f;     // Distance between vertices

    [Header("Noise Settings")]
    public float noiseScale = 1f;       // Controls the frequency of the noise
    public int octaves = 4;             // Number of layers of noise to combine
    public float amplitude = 5f;        // Height of terrain features
    public float persistence = 0.5f;    // Controls amplitude decay for each octave
    public float lacunarity = 2f;       // Controls frequency increase for each octave

    public bool debug = false;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private MeshCollider meshCollider;

    void Start()
    {
        GenerateGrid();
        AddMeshCollider();
    }

    void GenerateGrid()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Generate vertices
        vertices = new Vector3[(width + 1) * (height + 1)];
        Vector3 center = new Vector3(height/2, 0, width/2);
        float maxDistance = Vector3.Distance(center, new Vector3(width, 0, height));
        for (int i = 0, z = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float distFromCenter = Vector3.Distance(center, new Vector3(x,0,z))/maxDistance;
                float falloff = Mathf.Pow(1 - distFromCenter, 2);
                falloff = Mathf.Clamp01(falloff);

                float y = GenerateNoise(x, z);  // Use the noise function for terrain height
                y *= falloff;
                vertices[i] = new Vector3(x * cellSize, y, z * cellSize);
                if(debug){
                    CreateDebugSphere(new Vector3(x,10,z),falloff);
                }
            }
        }

        // Generate triangles
        triangles = new int[width * height * 6];
        for (int ti = 0, vi = 0, z = 0; z < height; z++, vi++)
        {
            for (int x = 0; x < width; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + width + 1;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + width + 1;
                triangles[ti + 5] = vi + width + 2;
            }
        }

        UpdateMesh();
    }

    float GenerateNoise(int x, int z)
    {
        float total = 0;
        float frequency = 1;
        float amplitudeFactor = 1;
        float maxPossibleHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = x * noiseScale * frequency / width;
            float zCoord = z * noiseScale * frequency / height;

            float noiseValue = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;  // Range [-1, 1]
            total += noiseValue * amplitudeFactor;

            maxPossibleHeight += amplitudeFactor;
            amplitudeFactor *= persistence;
            frequency *= lacunarity;
        }

        // Normalize and scale by the main amplitude
        return total / maxPossibleHeight * amplitude;
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();  // Important for lighting to work
    }

        // Adds a MeshCollider to the generated grid
    void AddMeshCollider()
    {
        if (meshCollider == null) 
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();  // Add MeshCollider if not already present
        }
        
        // Assign the MeshCollider to use the generated mesh
        meshCollider.sharedMesh = mesh;

        // Optionally, set it to be convex for physics-based interaction (if needed)
        meshCollider.convex = false;
    }

    void CreateDebugSphere(Vector3 position, float value)
    {
        // Create a small sphere at each vertex position
        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugSphere.transform.position = position;
        debugSphere.transform.localScale = Vector3.one * 0.5f;

        // Set the color based on the falloff value (0 = black, 1 = white)
        //Color color = value > 0.5f ? Color.white : Color.black;
        debugSphere.GetComponent<Renderer>().material.color = new Color(value,value,value);

        // Optional: Make the debug sphere a child of the grid for easier management
        debugSphere.transform.parent = transform;
    }
}
