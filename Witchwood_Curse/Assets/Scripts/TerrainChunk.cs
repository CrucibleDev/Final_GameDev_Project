using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainChunk : MonoBehaviour
{
    private TerrainChunkManager manager;
    private Vector2 position;
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
    }

    void GenerateGrid()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int verticesPerWidth = resolution + 1;
        int verticesPerHeight = resolution + 1;
        
        Vector3[] vertices = new Vector3[verticesPerWidth * verticesPerHeight];
        float stepX = width / resolution;
        float stepZ = height / resolution;

        for (int i = 0, z = 0; z < verticesPerHeight; z++)
        {
            for (int x = 0; x < verticesPerWidth; x++, i++)
            {
                float xPos = x * stepX;
                float zPos = z * stepZ;
                float worldX = position.x + xPos;
                float worldZ = position.y + zPos;

                float y = manager.GetTerrainHeight(worldX, worldZ);
                vertices[i] = new Vector3(xPos, y, zPos);
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
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void AddMeshCollider()
    {
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
} 