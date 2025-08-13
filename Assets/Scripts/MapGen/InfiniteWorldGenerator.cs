using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

// Infinite, chunked, CPU-side world generation for WebGL.
// Features: terrain (stone/dirt/grass), caves, floating islands, magma plane,
// normal + giant trees, deterministic via seed, bounded memory via chunk ring.
public class InfiniteWorldGenerator : MonoBehaviour
{
    [Header("Tilemap")]
    public Tilemap tilemap;

    [Header("Tiles")]
    public TileBase dirtTile;
    public TileBase grassTile;
    public TileBase stoneTile;
    public TileBase woodTile;
    public TileBase leafTile;
    public TileBase cloudTile;
    public TileBase lavaTile;

    [Header("World Seed & Baseline")]
    public int seed = 12345;
    public int groundLevel = 150; // avg surface altitude (in tiles)

    [Header("Terrain Noise")]
    [Range(10, 150)] public float terrainHeightVariation = 40f;
    [Range(0.001f, 0.05f)] public float terrainFrequency = 0.01f;

    [Header("Caves")]
    [Range(0.0f, 1.0f)] public float caveThreshold = 0.38f;
    [Range(0.005f, 0.2f)] public float caveFrequency = 0.05f;
    public int caveRoofPadding = 8;    // how thick the roof near surface remains solid
    public int caveFloorPadding = 2;   // don't carve into magma plane

    [Header("Floating Islands")]
    public bool enableIslands = true;
    public int islandsMinY = 200;
    public int islandsMaxY = 320;
    [Range(0.001f, 0.05f)] public float islandFrequency = 0.01f;
    [Range(0.0f, 1.0f)] public float islandThreshold = 0.72f;

    [Header("Trees - Normal")]
    public bool enableTrees = true;
    [Range(0f, 1f)] public float normalTreeChancePerColumn = 0.10f; // ~1 per 10 columns
    [Range(4, 20)] public int minTreeHeight = 6;
    [Range(6, 36)] public int maxTreeHeight = 12;

    [Header("Trees - Giant")]
    public bool enableGiantTrees = true;
    [Range(0f, 0.1f)] public float giantTreeChancePerCluster = 0.02f; // ~1 per cluster on average
    [Range(16, 128)] public int giantClusterSize = 64; // cluster width for rare spawn
    [Range(20, 120)] public int giantMinHeight = 30;
    [Range(24, 160)] public int giantMaxHeight = 60;
    [Range(3, 7)] public int giantMinTrunkWidth = 3;
    [Range(3, 9)] public int giantMaxTrunkWidth = 5;

    [Header("Magma")]
    public int magmaPlaneY = -200; // all tiles below become lava (infinite world needs a fixed plane)

    [Header("Chunk Streaming")]
    [Tooltip("Tile chunk size. 64 is a good WebGL default.")]
    public int chunkSize = 64;
    [Tooltip("Radius (in chunks) around camera kept loaded. Total chunks loaded = (2r+1)^2")]
    public int viewRadiusChunks = 6;
    [Tooltip("Chunks generated per frame to avoid hitches.")]
    public int chunksPerFrame = 2;
    [Tooltip("Chunks cleared per frame to avoid hitches.")]
    public int clearsPerFrame = 4;

    [Header("Performance")]
    [Tooltip("Compress bounds after N updates. 0 = never (safer for streaming).")]
    public int compressEveryNGenerations = 0;

    // Tile codes (byte = compact)
    private const byte Air = 0;
    private const byte Dirt = 1;
    private const byte Grass = 2;
    private const byte Stone = 3;
    private const byte Wood = 4;
    private const byte Leaf = 5;
    private const byte Cloud = 6;
    private const byte Lava = 7;

    private Camera cam;
    private Vector2Int lastCenterChunk;
    private bool firstRun = true;

    // Active chunk set and work queues
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> desiredChunks = new HashSet<Vector2Int>();
    private Queue<Vector2Int> genQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> clearQueue = new Queue<Vector2Int>();

    // Preallocated empty buffer for fast clears
    private TileBase[] emptyChunkTiles;

    // Optional: player edits (sparse). Key = world cell, Value = tile code
    private Dictionary<Vector2Int, byte> edits = new Dictionary<Vector2Int, byte>();

    void Start()
    {
        cam = Camera.main;
        if (tilemap == null)
        {
            Debug.LogError("Tilemap reference is missing.");
            enabled = false;
            return;
        }

        tilemap.ClearAllTiles();

        // Prealloc a null tile buffer for chunk clearing
        emptyChunkTiles = new TileBase[chunkSize * chunkSize];

        StartCoroutine(Streamer());
    }

    IEnumerator Streamer()
    {
        int genCounter = 0;

        while (true)
        {
            // 1) Update desired chunks around camera
            UpdateDesiredChunks();

            // 2) Enqueue generation and clearing tasks
            EnqueueChunkWork();

            // 3) Generate a few chunks this frame
            int genThisFrame = 0;
            while (genQueue.Count > 0 && genThisFrame < chunksPerFrame)
            {
                var cc = genQueue.Dequeue();
                yield return GenerateAndRenderChunk(cc);
                loadedChunks.Add(cc);
                genThisFrame++;
                genCounter++;

                if (compressEveryNGenerations > 0 && (genCounter % compressEveryNGenerations) == 0)
                {
                    tilemap.CompressBounds();
                }
            }

            // 4) Clear a few chunks this frame
            int clearThisFrame = 0;
            while (clearQueue.Count > 0 && clearThisFrame < clearsPerFrame)
            {
                var cc = clearQueue.Dequeue();
                ClearChunk(cc);
                loadedChunks.Remove(cc);
                clearThisFrame++;
            }

            // Let the frame breathe
            yield return null;
        }
    }

    void UpdateDesiredChunks()
    {
        if (cam == null) return;

        // Convert camera position to tile coordinates (assumes Grid cell size 1)
        Vector3 camPos = cam.transform.position;
        int cx = Mathf.FloorToInt(camPos.x);
        int cy = Mathf.FloorToInt(camPos.y);

        // Compute chunk containing the camera using floor division
        var centerChunk = new Vector2Int(FloorDiv(cx, chunkSize), FloorDiv(cy, chunkSize));

        // Only rebuild desired set when we enter a new chunk or on first run
        if (!firstRun && centerChunk == lastCenterChunk) return;
        firstRun = false;
        lastCenterChunk = centerChunk;

        desiredChunks.Clear();
        for (int dy = -viewRadiusChunks; dy <= viewRadiusChunks; dy++)
        {
            for (int dx = -viewRadiusChunks; dx <= viewRadiusChunks; dx++)
            {
                desiredChunks.Add(new Vector2Int(centerChunk.x + dx, centerChunk.y + dy));
            }
        }
    }

    void EnqueueChunkWork()
    {
        // Add new desired chunks to generation queue if not already loaded or queued
        foreach (var cc in desiredChunks)
        {
            if (!loadedChunks.Contains(cc) && !genQueue.Contains(cc))
                genQueue.Enqueue(cc);
        }

        // Any loaded chunks not desired anymore -> clear
        foreach (var cc in loadedChunks)
        {
            if (!desiredChunks.Contains(cc) && !clearQueue.Contains(cc))
                clearQueue.Enqueue(cc);
        }
    }

    IEnumerator GenerateAndRenderChunk(Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;
        int sizeX = chunkSize;
        int sizeY = chunkSize;

        // Build tile buffer
        TileBase[] tiles = new TileBase[sizeX * sizeY];

        // Generate tiles row by row, yielding occasionally
        for (int ly = 0; ly < sizeY; ly++)
        {
            int y = startY + ly;

            for (int lx = 0; lx < sizeX; lx++)
            {
                int x = startX + lx;

                // Base terrain/islands/caves/magma
                byte t = SampleBaseTile(x, y);

                // Player edits override base
                if (TryGetEdit(x, y, out byte editTile))
                    t = editTile;

                // Trees overlay (only if not magma)
                if (enableTrees && t != Lava)
                {
                    byte treeT = SampleTreeOverlay(x, y);
                    if (treeT != Air) t = treeT;
                }

                tiles[ly * sizeX + lx] = ToTile(t);
            }

            // Yield every 16 rows to keep frames responsive
            if ((ly & 15) == 0) yield return null;
        }

        // Write to tilemap in a single batch
        var bounds = new BoundsInt(
            new Vector3Int(startX, startY, 0),
            new Vector3Int(sizeX, sizeY, 1)
        );
        tilemap.SetTilesBlock(bounds, tiles);
    }

    void ClearChunk(Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;
        var bounds = new BoundsInt(
            new Vector3Int(startX, startY, 0),
            new Vector3Int(chunkSize, chunkSize, 1)
        );
        tilemap.SetTilesBlock(bounds, emptyChunkTiles);
    }

    // =========================
    // Tile sampling (deterministic)
    // =========================

    byte SampleBaseTile(int x, int y)
    {
        // Magma plane
        if (y < magmaPlaneY) return Lava;

        // Surface height via centered fBm
        int surface = GetSurfaceY(x);

        // Floating islands overlay (in air space)
        if (enableIslands && y >= islandsMinY && y <= islandsMaxY)
        {
            bool inside = IslandMask(x, y);
            if (inside)
            {
                // Determine top layers by checking neighbor above
                bool aboveInside = IslandMask(x, y + 1);

                if (!aboveInside) return Grass;
                if (!IslandMask(x, y + 2)) return Dirt;
                return cloudTile != null ? Cloud : Stone;
            }
        }

        // Above ground = air
        if (y >= surface) return Air;

        // Caves: carve air beneath the surface, not too near surface or magma
        if (y >= magmaPlaneY + caveFloorPadding && y < surface - caveRoofPadding)
        {
            float c1 = Mathf.PerlinNoise(x * caveFrequency + seed * 0.217f, y * caveFrequency + seed * 0.489f);
            float c2 = Mathf.PerlinNoise(x * caveFrequency * 2f + 100f, y * caveFrequency * 2f + 100f);
            float cav = (c1 + c2) * 0.5f;
            if (cav < caveThreshold) return Air;
        }

        // Stratified ground
        if (y >= surface - 1) return Grass;
        if (y >= surface - 5) return Dirt;
        return Stone;
    }

    // Normal + giant trees as an overlay based on hashed x
    byte SampleTreeOverlay(int x, int y)
    {
        int surface = GetSurfaceY(x);

        // Only place wood/leaves at or above ground
        if (y < surface) return Air;

        // Giant tree check first (rare)
        if (enableGiantTrees)
        {
            byte gt = SampleGiantTree(x, y, surface);
            if (gt != Air) return gt;
        }

        // Normal tree
        if (enableTrees)
        {
            byte nt = SampleNormalTree(x, y, surface);
            if (nt != Air) return nt;
        }

        return Air;
    }

    byte SampleNormalTree(int x, int y, int surface)
    {
        // Decide if this column spawns a tree
        float rng = Hash01(x * 73856093 ^ seed * 19349663);
        if (rng >= normalTreeChancePerColumn) return Air;

        // Avoid cliffs: check slope
        int sL = GetSurfaceY(x - 1);
        int sR = GetSurfaceY(x + 1);
        if (Mathf.Abs(sL - sR) > 2) return Air;

        // Height based on hashed x
        int h = Mathf.RoundToInt(Mathf.Lerp(minTreeHeight, maxTreeHeight, Hash01(x * 83492791 ^ seed * 374761393)));
        int crown = Mathf.Max(2, h / 3);
        int crownTop = surface + h;

        // Trunk (1 wide)
        if (y >= surface && y < surface + h)
            return Wood;

        // Leaves crown
        int dy = y - crownTop;
        int dx = 0; // trunk at exact x
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist <= crown && dist > 0.2f)
        {
            // Only place leaves if base is air (avoid burying in ground)
            byte baseT = SampleBaseTile(x, y);
            if (baseT == Air) return Leaf;
        }

        return Air;
    }

    byte SampleGiantTree(int x, int y, int surface)
    {
        // Work in cluster space to reduce frequency and keep trees spaced out
        int cluster = FloorDiv(x, giantClusterSize);
        int clusterBaseX = cluster * giantClusterSize;

        // Randomly decide if this cluster has a giant tree
        float clusterRng = Hash01((int)(cluster * 2654435761u ^ (uint)(seed * 1013904223)));
        if (clusterRng >= giantTreeChancePerCluster) return Air;

        // Deterministic center within cluster and trunk width/height
        int centerOffset = Mathf.FloorToInt(Hash01(cluster * 97531 ^ seed * 50123) * (giantClusterSize - 8)) + 4;
        int centerX = clusterBaseX + centerOffset;

        int trunkWidth = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(giantMinTrunkWidth, giantMaxTrunkWidth, Hash01(cluster * 1237 ^ seed * 8911))),
            giantMinTrunkWidth, giantMaxTrunkWidth
        );

        int height = Mathf.RoundToInt(Mathf.Lerp(giantMinHeight, giantMaxHeight, Hash01(cluster * 3571 ^ seed * 9623)));
        int halfW = trunkWidth / 2;

        // Trunk
        if (Mathf.Abs(x - centerX) <= halfW && y >= surface && y < surface + height)
            return Wood;

        // Massive crown
        int crownTop = surface + height;
        int crownRadius = Mathf.Max(4, height / 2);
        int dx = x - centerX;
        int dy = y - crownTop;
        float noise = Mathf.PerlinNoise((centerX + x) * 0.05f + seed * 0.1337f, (crownTop + y) * 0.05f + seed * 0.7331f);
        float irregular = crownRadius * (0.65f + noise * 0.4f);
        if (dx * dx + dy * dy <= irregular * irregular)
        {
            // Only leaves if base is air, avoid burying
            byte baseT = SampleBaseTile(x, y);
            if (baseT == Air) return Leaf;
        }

        return Air;
    }

    // =========================
    // Helpers
    // =========================

    int GetSurfaceY(int x)
    {
        float fx = x * terrainFrequency;
        float h1 = (Mathf.PerlinNoise(fx + seed * 0.173f, 0f) - 0.5f) * (terrainHeightVariation * 2f);
        float h2 = (Mathf.PerlinNoise(fx * 2f + seed * 0.317f, 0f) - 0.5f) * (terrainHeightVariation * 1.0f);
        float h3 = (Mathf.PerlinNoise(fx * 4f + seed * 0.619f, 0f) - 0.5f) * (terrainHeightVariation * 0.5f);
        return groundLevel + Mathf.RoundToInt(h1 + h2 + h3);
    }

    bool IslandMask(int x, int y)
    {
        float n = Mathf.PerlinNoise(x * islandFrequency + seed * 0.111f, y * islandFrequency + seed * 0.222f);
        return n > islandThreshold;
    }

    bool TryGetEdit(int x, int y, out byte t)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (edits.TryGetValue(key, out t)) return true;
        t = 0;
        return false;
    }

    // Add/remove player edits externally as needed
    public void SetEdit(int x, int y, byte tileCode)
    {
        var key = new Vector2Int(x, y);
        if (tileCode == Air) edits.Remove(key);
        else edits[key] = tileCode;

        // Force regeneration of the chunk containing this cell
        Vector2Int cc = new Vector2Int(FloorDiv(x, chunkSize), FloorDiv(y, chunkSize));
        if (!genQueue.Contains(cc)) genQueue.Enqueue(cc);
    }

    static int FloorDiv(int a, int b)
    {
        // Floor division for negatives
        if (b < 0) { a = -a; b = -b; }
        int q = a / b;
        int r = a % b;
        if ((r != 0) && ((r < 0) != (b < 0))) --q;
        return q;
    }

    static float Hash01(int x)
    {
        unchecked
        {
            uint u = (uint)x;
            u ^= 2747636419u;
            u *= 2654435769u;
            u ^= u >> 16;
            u *= 2654435769u;
            u ^= u >> 16;
            return (u & 0xFFFFFF) / 16777215f; // 24-bit fraction
        }
    }

    static float Hash01(uint x)
    {
        unchecked
        {
            x ^= 2747636419u;
            x *= 2654435769u;
            x ^= x >> 16;
            x *= 2654435769u;
            x ^= x >> 16;
            return (x & 0xFFFFFF) / 16777215f;
        }
    }

    TileBase ToTile(byte t)
    {
        switch (t)
        {
            case Dirt: return dirtTile;
            case Grass: return grassTile;
            case Stone: return stoneTile;
            case Wood: return woodTile;
            case Leaf: return leafTile;
            case Cloud: return cloudTile != null ? cloudTile : stoneTile;
            case Lava: return lavaTile != null ? lavaTile : stoneTile;
            default: return null; // Air
        }
    }
}
