[System.Serializable]
public class TerrainChunkSettings
{
    public float chunkSize = 10f;
    public int resolution = 100;
    
    public float noiseScale = 1f;
    public int octaves = 4;
    public float amplitude = 10f;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    public int numberOfFlattenedPoints = 16;
    public float flattenRadius = 8f;
    public float minPointDistance = 16f;
    public float pathWidth = 4f;
} 