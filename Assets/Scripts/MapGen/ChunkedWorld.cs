// Attach to a GameObject with a Grid + Tilemap child
// Only generates and renders chunks around the camera.
// Replace your monolithic generator with this pattern.

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class ChunkedWorld : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;

    public TileBase dirt, grass, stone, wood, leaf, cloud, lava;

    [Header("World Settings")]
    public int worldWidth = 2000;
    public int worldHeight = 10000;
    public int groundLevel = 150;
    public int seed = 12345;

    [Header("Noise")]
    [Range(10,100)] public float terrainHeightVariation = 30f;
    [Range(0.001f,0.1f)] public float terrainFrequency = 0.02f;
    [Range(0.0f,1.0f)] public float caveThreshold = 0.4f;
    [Range(0.01f,0.2f)] public float caveFrequency = 0.05f;

    [Header("Magma")]
    [Range(1,64)] public int magmaLayerDepth = 12;

    [Header("Chunks")]
    public int chunkSize = 128;
    public int viewChunksX = 6; // chunks around camera horizontally
    public int viewChunksY = 6; // chunks around camera vertically
    public int chunksPerFrame = 2;

    // Keep only edited tiles (player modifications)
    private Dictionary<Vector2Int, byte> edits = new Dictionary<Vector2Int, byte>();
    private HashSet<Vector2Int> requested = new HashSet<Vector2Int>();
    private Queue<Vector2Int> toGenerate = new Queue<Vector2Int>();

    private Camera cam;
    private int chunkCols, chunkRows;

    // Tile codes as bytes for compactness (0=Air)
    private const byte Air=0, Dirt=1, Grass=2, Stone=3, Wood=4, Leaf=5, Cloud=6, Lava=7;

    void Start()
    {
        cam = Camera.main;
        chunkCols = Mathf.CeilToInt((float)worldWidth / chunkSize);
        chunkRows = Mathf.CeilToInt((float)worldHeight / chunkSize);
        StartCoroutine(ChunkStreamer());
    }

    IEnumerator ChunkStreamer()
    {
        while (true)
        {
            EnqueueVisibleChunks();
            int generated = 0;
            while (toGenerate.Count > 0 && generated < chunksPerFrame)
            {
                var c = toGenerate.Dequeue();
                yield return GenerateAndRenderChunk(c.x, c.y);
                generated++;
            }
            yield return null; // spread work
        }
    }

    void EnqueueVisibleChunks()
    {
        Vector3 worldCenter = cam.transform.position;
        // Convert world pos to tile coords (assuming cellSize 1, origin centered)
        int centerX = Mathf.RoundToInt(worldCenter.x) + worldWidth/2;
        int centerY = Mathf.RoundToInt(worldCenter.y) + worldHeight/2;

        int centerChunkX = Mathf.Clamp(centerX / chunkSize, 0, chunkCols-1);
        int centerChunkY = Mathf.Clamp(centerY / chunkSize, 0, chunkRows-1);

        for (int dy = -viewChunksY/2; dy <= viewChunksY/2; dy++)
        {
            for (int dx = -viewChunksX/2; dx <= viewChunksX/2; dx++)
            {
                int cx = centerChunkX + dx;
                int cy = centerChunkY + dy;
                if (cx < 0 || cy < 0 || cx >= chunkCols || cy >= chunkRows) continue;

                var key = new Vector2Int(cx, cy);
                if (!requested.Contains(key))
                {
                    requested.Add(key);
                    toGenerate.Enqueue(key);
                }
            }
        }
    }

    IEnumerator GenerateAndRenderChunk(int chunkX, int chunkY)
    {
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;
        int sizeX = Mathf.Min(chunkSize, worldWidth - startX);
        int sizeY = Mathf.Min(chunkSize, worldHeight - startY);

        // Prepare batch arrays
        var tiles = new TileBase[sizeX * sizeY];

        // Generate CPU-side, but only the tiles that can be solid (up to surface or magma)
        for (int ly = 0; ly < sizeY; ly++)
        {
            int gy = startY + ly;
            for (int lx = 0; lx < sizeX; lx++)
            {
                int gx = startX + lx;
                byte t = SampleTile(gx, gy);
                tiles[ly * sizeX + lx] = ToTile(t);
            }
            // yield occasionally to keep frame responsive
            if ((ly & 15) == 0) yield return null;
        }

        // Blit tiles in one call
        var bounds = new BoundsInt(
            new Vector3Int(startX - worldWidth/2, startY - worldHeight/2, 0),
            new Vector3Int(sizeX, sizeY, 1)
        );
        tilemap.SetTilesBlock(bounds, tiles);
        tilemap.CompressBounds();
    }

    // Base terrain + caves + magma, deterministic (no worldData array)
    byte SampleTile(int x, int y)
    {
        // magma
        if (y < magmaLayerDepth) return Lava;

        // surface height via centered fBm
        float fx = x * terrainFrequency;
        float h1 = (Mathf.PerlinNoise(fx + seed * 0.173f, 0f) - 0.5f) * (terrainHeightVariation * 2f);
        float h2 = (Mathf.PerlinNoise(fx * 2f + seed * 0.317f, 0f) - 0.5f) * (terrainHeightVariation * 1.0f);
        float h3 = (Mathf.PerlinNoise(fx * 4f + seed * 0.619f, 0f) - 0.5f) * (terrainHeightVariation * 0.5f);
        int surface = Mathf.Clamp(groundLevel + Mathf.RoundToInt(h1 + h2 + h3), 2, worldHeight-2);

        // floating islands (simple ellipse noise mask example)
        // Keep cheap: only a small band
        if (y >= 180 && y <= 260)
        {
            float island = Mathf.PerlinNoise(x * 0.01f + seed * 0.11f, y * 0.02f + seed * 0.23f);
            if (island > 0.7f)
            {
                if (y > 0) return Grass;
                else return Stone;
            }
        }

        // above surface is air
        if (y >= surface) return Air;

        // caves
        if (y > magmaLayerDepth + 2 && y < surface - 8)
        {
            float c1 = Mathf.PerlinNoise(x * caveFrequency + seed * 0.217f, y * caveFrequency + seed * 0.489f);
            float c2 = Mathf.PerlinNoise(x * caveFrequency * 2f + 100f, y * caveFrequency * 2f + 100f);
            float cav = (c1 + c2) * 0.5f;
            if (cav < caveThreshold) return Air;
        }

        // stratification near surface
        if (y >= surface - 1) return Grass;
        if (y >= surface - 5) return Dirt;
        return Stone;
    }

    TileBase ToTile(byte t)
    {
        switch (t)
        {
            case Dirt: return dirt;
            case Grass: return grass;
            case Stone: return stone;
            case Wood: return wood;
            case Leaf: return leaf;
            case Cloud: return cloud ? cloud : stone;
            case Lava: return lava ? lava : stone;
            default: return null; // Air
        }
    }
}
