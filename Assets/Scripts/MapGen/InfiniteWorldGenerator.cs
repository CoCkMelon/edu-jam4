using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

// Infinite, chunked world with:
// - Surface tunnel entrances (tapered shafts)
// - Worm- & lava-carved caves
// - Trees: trunks + platform branches (foreground), canopy (background tiles or prefab)
// - Decorations: two flower tiles on surface, small stones on surface and cave floors
//
// Setup:
// - Scene: Grid with two Tilemaps: Foreground (solid/platform) and Background (behind).
// - Assign tiles for dirt/grass/stone/lava, wood, branch (1-way platform tile recommended).
// - Assign leafBackgroundTile or canopyPrefab for canopies.
// - Assign smallStoneSurfaceTile (no collider), smallStoneCaveTile (no collider), flowerTileA/B (no collider).
public class InfiniteWorldGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap foregroundTilemap;
    public Tilemap backgroundTilemap;

    [Header("Tiles (Foreground)")]
    public TileBase dirtTile;
    public TileBase grassTile;
    public TileBase stoneTile;
    public TileBase lavaTile;
    public TileBase woodTile;           // trunks
    public TileBase branchTile;         // platform branches (use 1-way platform tile or set Tilemap collider + effector)

    [Header("Tiles (Background)")]
    public TileBase leafBackgroundTile; // canopy leaves as background tiles
    public TileBase cloudTile;          // floating islands core alt

    [Header("Optional Canopy Prefab (Background)")]
    public bool useCanopyPrefab = false;
    public GameObject canopyPrefab;
    public string canopySortingLayer = "Background";
    public int canopySortingOrder = 0;
    public Vector2 canopyScale = Vector2.one;

    [Header("World Seed & Baseline")]
    public int seed = 12345;
    public int groundLevel = 150;

    [Header("Terrain Noise (surface shape)")]
    [Range(10, 150)] public float terrainHeightVariation = 40f;
    [Range(0.001f, 0.05f)] public float terrainFrequency = 0.01f;

    [Header("Caves (worm + lava carving)")]
    public int wormRegionSize = 128;
    [Range(0, 3)] public int wormsPerRegionHorizontal = 1;
    [Range(0, 3)] public int wormsPerRegionVertical = 1;
    [Range(1f, 32f)] public float wormBaseRadius = 3.5f;
    [Range(0.002f, 0.05f)] public float wormCurveFrequency = 0.015f;
    [Range(2f, 32f)] public float wormAmplitudeMin = 6f;
    [Range(4f, 64f)] public float wormAmplitudeMax = 18f;

    [Range(16, 256)] public int lavaCarveHeight = 80; // carving band above magma
    [Range(0.001f, 0.05f)] public float lavaChamberFrequency = 0.01f;
    [Range(0.0f, 1.0f)] public float lavaChamberThreshold = 0.62f;

    [Header("Cave Safe Margins")]
    public int caveRoofPadding = 8;  // don't carve too close to surface
    public int caveFloorPadding = 3; // don't carve into magma plane

    [Header("Tunnel Entrances (from surface)")]
    public int entranceClusterSize = 48;
    [Range(0f, 1f)] public float entranceChancePerCluster = 0.45f;
    public int entranceMinDepth = 20;
    public int entranceMaxDepth = 60;
    public float entranceBaseRadius = 2.0f;
    public float entranceTopWiden = 1.35f;  // wider at top for a mouth
    public float entranceTaper = 0.035f;    // radius decrease per tile of depth
    public float entranceWobbleFreq = 0.15f;
    public float entranceWobbleAmp = 0.8f;

    [Header("Floating Islands")]
    public bool enableIslands = true;
    public int islandsMinY = 200;
    public int islandsMaxY = 360;
    [Range(0.001f, 0.05f)] public float islandFrequency = 0.01f;
    [Range(0.0f, 1.0f)] public float islandThreshold = 0.72f;

    [Header("Magma")]
    public int magmaPlaneY = -200; // tiles below are lava

    [Header("Trees - Normal")]
    public bool enableTrees = true;
    [Range(0f, 1f)] public float normalTreeChancePerColumn = 0.10f;
    [Range(4, 28)] public int minTreeHeight = 8;
    [Range(8, 48)] public int maxTreeHeight = 18;
    [Range(1, 3)] public int normalTrunkWidth = 1;

    [Header("Trees - Giant")]
    public bool enableGiantTrees = true;
    [Range(0f, 0.1f)] public float giantTreeChancePerCluster = 0.02f; // per cluster
    [Range(16, 256)] public int giantClusterSize = 96;
    [Range(24, 160)] public int giantMinHeight = 40;
    [Range(36, 220)] public int giantMaxHeight = 90;
    [Range(3, 11)] public int giantMinTrunkWidth = 3;
    [Range(5, 15)] public int giantMaxTrunkWidth = 7;

    [Header("Branches (Platforms)")]
    [Range(1, 5)] public int branchThickness = 1;
    public int normalBranchMin = 2;
    public int normalBranchMax = 4;
    public int giantBranchMin = 3;
    public int giantBranchMax = 6;
    public int normalBranchLenMin = 5;
    public int normalBranchLenMax = 12;
    public int giantBranchLenMin = 8;
    public int giantBranchLenMax = 16;
    [Range(0f, 0.6f)] public float branchSlopePerTile = 0.15f;
    [Range(0f, 0.5f)] public float branchWiggle = 0.12f;
    public int branchGenerationMargin = 20;

    [Header("Canopy (Background)")]
    public bool useBackgroundLeafTiles = true;
    public int normalCanopyRadiusMin = 6;
    public int normalCanopyRadiusMax = 10;
    public int giantCanopyRadiusMin = 12;
    public int giantCanopyRadiusMax = 22;
    [Range(0f, 0.6f)] public float canopyEdgeNoise = 0.25f;

    [Header("Decorations")]
    public TileBase smallStoneSurfaceTile; // no collider recommended
    [Range(0f, 1f)] public float smallStoneSurfaceChance = 0.10f;
    public TileBase smallStoneCaveTile;    // no collider recommended
    [Range(0f, 1f)] public float smallStoneCaveChance = 0.02f;
    public TileBase flowerTileA;           // no collider
    public TileBase flowerTileB;           // no collider
    [Range(0f, 1f)] public float flowerChance = 0.14f;
    [Range(0f, 1f)] public float flowerBPortion = 0.45f; // portion of flowers that use type B

    [Header("Chunk Streaming")]
    public int chunkSize = 64;
    public int viewRadiusChunks = 6;
    public int chunksGeneratedPerFrame = 2;
    public int chunksClearedPerFrame = 4;
    [Tooltip("Compress bounds occasionally (expensive). 0 = never")]
    public int compressEveryNGenerations = 0;

    // Tile codes (byte, compact) for base layer mapping
    private const byte Air = 0;
    private const byte Dirt = 1;
    private const byte Grass = 2;
    private const byte Stone = 3;
    private const byte Lava = 4;
    private const byte Wood = 5;     // trunk segments (foreground)
    private const byte Branch = 6;   // platform branch segments (foreground)
    private const byte Cloud = 7;    // islands core if no cloud tile

    // Streaming
    private Camera cam;
    private Vector2Int lastCenterChunk;
    private bool firstRun = true;

    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> desiredChunks = new HashSet<Vector2Int>();
    private Queue<Vector2Int> genQueue = new Queue<Vector2Int>();
    private Queue<Vector2Int> clearQueue = new Queue<Vector2Int>();

    // Empty buffers for fast clears
    private TileBase[] emptyTiles;

    // Canopy prefab bookkeeping
    private Dictionary<Vector2Int, List<long>> chunkTreeKeys = new Dictionary<Vector2Int, List<long>>();
    private Dictionary<long, GameObject> canopyInstancesByKey = new Dictionary<long, GameObject>();

    void Start()
    {
        cam = Camera.main;

        if (foregroundTilemap == null || backgroundTilemap == null)
        {
            Debug.LogError("Assign both Foreground and Background Tilemaps.");
            enabled = false;
            return;
        }
        if (dirtTile == null || grassTile == null || stoneTile == null || lavaTile == null)
        {
            Debug.LogError("Assign dirt/grass/stone/lava tiles.");
            enabled = false;
            return;
        }
        if (!useBackgroundLeafTiles && useCanopyPrefab && canopyPrefab == null)
        {
            Debug.LogWarning("useCanopyPrefab enabled but no canopyPrefab assigned.");
        }

        foregroundTilemap.ClearAllTiles();
        backgroundTilemap.ClearAllTiles();
        emptyTiles = new TileBase[chunkSize * chunkSize];

        StartCoroutine(Streamer());
    }

    IEnumerator Streamer()
    {
        int genCounter = 0;

        while (true)
        {
            UpdateDesiredChunks();
            EnqueueChunkWork();

            int g = 0;
            while (genQueue.Count > 0 && g < chunksGeneratedPerFrame)
            {
                var cc = genQueue.Dequeue();
                yield return GenerateAndRenderChunk(cc);
                loadedChunks.Add(cc);
                g++;
                genCounter++;
                if (compressEveryNGenerations > 0 && (genCounter % compressEveryNGenerations) == 0)
                {
                    foregroundTilemap.CompressBounds();
                    backgroundTilemap.CompressBounds();
                }
            }

            int c = 0;
            while (clearQueue.Count > 0 && c < chunksClearedPerFrame)
            {
                var cc = clearQueue.Dequeue();
                ClearChunk(cc);
                loadedChunks.Remove(cc);
                c++;
            }

            yield return null;
        }
    }

    void UpdateDesiredChunks()
    {
        if (cam == null) return;

        Vector3 p = cam.transform.position;
        int cx = Mathf.FloorToInt(p.x);
        int cy = Mathf.FloorToInt(p.y);
        var center = new Vector2Int(FloorDiv(cx, chunkSize), FloorDiv(cy, chunkSize));

        if (!firstRun && center == lastCenterChunk) return;
        firstRun = false;
        lastCenterChunk = center;

        desiredChunks.Clear();
        for (int dy = -viewRadiusChunks; dy <= viewRadiusChunks; dy++)
        {
            for (int dx = -viewRadiusChunks; dx <= viewRadiusChunks; dx++)
            {
                desiredChunks.Add(new Vector2Int(center.x + dx, center.y + dy));
            }
        }
    }

    void EnqueueChunkWork()
    {
        foreach (var cc in desiredChunks)
        {
            if (!loadedChunks.Contains(cc) && !genQueue.Contains(cc))
                genQueue.Enqueue(cc);
        }

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

        // Prepare buffers for both tilemaps
        TileBase[] fg = new TileBase[sizeX * sizeY];
        TileBase[] bg = new TileBase[sizeX * sizeY];

        // 1) Base terrain + caves + islands + magma
        for (int ly = 0; ly < sizeY; ly++)
        {
            int y = startY + ly;
            for (int lx = 0; lx < sizeX; lx++)
            {
                int x = startX + lx;
                byte baseT = SampleBaseTile(x, y);
                fg[ly * sizeX + lx] = ToForegroundTile(baseT);
            }
            if ((ly & 15) == 0) yield return null;
        }

        // 2) Trees: find spawns in and near this chunk
        var trees = EnumerateTreesInRange(startX - branchGenerationMargin, startX + sizeX + branchGenerationMargin);

        // 2a) Draw trunks (foreground)
        foreach (var t in trees)
        {
            int trunkHalf = t.trunkWidth / 2;
            for (int dx = -trunkHalf; dx <= trunkHalf; dx++)
            {
                int tx = t.x + dx;
                if (tx < startX || tx >= startX + sizeX) continue;

                for (int yy = t.surface; yy < t.surface + t.height; yy++)
                {
                    if (yy < startY || yy >= startY + sizeY) continue;

                    int lx = tx - startX;
                    int ly = yy - startY;
                    fg[ly * sizeX + lx] = ToForegroundTile(Wood);
                }
            }
        }

        // 2b) Branch platforms (foreground)
        foreach (var t in trees)
        {
            int branchCount = t.isGiant
                ? Mathf.RoundToInt(Mathf.Lerp(giantBranchMin, giantBranchMax, Hash01(t.x * 137 + seed * 53)))
                : Mathf.RoundToInt(Mathf.Lerp(normalBranchMin, normalBranchMax, Hash01(t.x * 257 + seed * 97)));
            branchCount = Mathf.Clamp(branchCount, t.isGiant ? giantBranchMin : normalBranchMin, t.isGiant ? giantBranchMax : normalBranchMax);

            if (branchCount <= 0) continue;
            int baseMinY = t.surface + (t.height / 3);
            int baseMaxY = t.surface + t.height - 3;

            for (int i = 0; i < branchCount; i++)
            {
                int branchSeed = t.x * 9187 + i * 193 + seed * 7919;
                int dir = Hash01(branchSeed) < 0.5f ? -1 : 1;
                int brLenMin = t.isGiant ? giantBranchLenMin : normalBranchLenMin;
                int brLenMax = t.isGiant ? giantBranchLenMax : normalBranchLenMax;
                int brLen = Mathf.RoundToInt(Mathf.Lerp(brLenMin, brLenMax, Hash01(branchSeed * 7)));

                int by = Mathf.RoundToInt(Mathf.Lerp(baseMinY, baseMaxY, Hash01(branchSeed * 11)));
                int bx = t.x + Mathf.RoundToInt((t.trunkWidth / 2f) * dir);

                PlotBranchIntoBuffer(bx, by, dir, brLen, t.isGiant, startX, startY, sizeX, sizeY, fg);
            }
        }

        // 3) Background canopy (prefab or tiles)
        var treeKeysInChunk = new List<long>();
        foreach (var t in trees)
        {
            int canopyRadius = t.isGiant
                ? Mathf.RoundToInt(Mathf.Lerp(giantCanopyRadiusMin, giantCanopyRadiusMax, Hash01(t.x * 733 + seed * 211)))
                : Mathf.RoundToInt(Mathf.Lerp(normalCanopyRadiusMin, normalCanopyRadiusMax, Hash01(t.x * 487 + seed * 157)));
            canopyRadius = Mathf.Max(3, canopyRadius);

            int canopyCenterX = t.x;
            int canopyCenterY = t.surface + t.height;

            if (useCanopyPrefab)
            {
                if (canopyCenterX >= startX && canopyCenterX < startX + sizeX &&
                    canopyCenterY >= startY && canopyCenterY < startY + sizeY)
                {
                    long key = ComposeTreeKey(t.x, t.surface);
                    treeKeysInChunk.Add(key);

                    if (!canopyInstancesByKey.ContainsKey(key) && canopyPrefab != null)
                    {
                        var go = Instantiate(canopyPrefab, new Vector3(canopyCenterX + 0.5f, canopyCenterY + 0.5f, 0f), Quaternion.identity);
                        var sr = go.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sortingLayerName = canopySortingLayer;
                            sr.sortingOrder = canopySortingOrder;
                        }
                        go.transform.localScale = new Vector3(canopyScale.x, canopyScale.y, 1f);
                        canopyInstancesByKey[key] = go;
                    }
                }
            }

            if (useBackgroundLeafTiles && leafBackgroundTile != null)
            {
                PlotCanopyIntoBackground(canopyCenterX, canopyCenterY, canopyRadius, t.isGiant, startX, startY, sizeX, sizeY, bg);
            }
        }
        if (treeKeysInChunk.Count > 0)
            chunkTreeKeys[chunkCoord] = treeKeysInChunk;

        // 4) Decorations: flowers + small stones (surface and cave floors)
        // 4a) Flowers and surface stones: place in the air cell directly above surface
        for (int lx = 0; lx < sizeX; lx++)
        {
            int x = startX + lx;
            int ySurface = GetSurfaceY(x);

            if (ySurface >= startY && ySurface < startY + sizeY)
            {
                int idx = (ySurface - startY) * sizeX + lx;

                // Place only if still air (no branch/trunk already there)
                if (fg[idx] == null)
                {
                    // Ensure there's solid ground below (within chunk, or from tilemap/procedural sample)
                    if (!HasSolidBelow(x, ySurface, startX, startY, sizeX, sizeY, fg))
                        continue;

                    // Choose flower or stone
                    float r = Hash01(x * 9157 ^ seed * 29791);
                    if (flowerTileA != null || flowerTileB != null)
                    {
                        if (r < flowerChance)
                        {
                            bool useB = Hash01(x * 53129 ^ seed * 191) < flowerBPortion;
                            var flowerTile = (useB && flowerTileB != null) ? flowerTileB : (flowerTileA != null ? flowerTileA : null);
                            if (flowerTile != null)
                                fg[idx] = flowerTile;
                            continue;
                        }
                    }
                    if (smallStoneSurfaceTile != null && r < flowerChance + smallStoneSurfaceChance)
                    {
                        fg[idx] = smallStoneSurfaceTile;
                    }
                }
            }
        }

        // 4b) Cave small stones: in air cells with solid below, below surface
        for (int ly = 0; ly < sizeY; ly++)
        {
            int y = startY + ly;
            for (int lx = 0; lx < sizeX; lx++)
            {
                int x = startX + lx;
                int idx = ly * sizeX + lx;

                if (fg[idx] != null) continue; // something already placed here (branch/flower/etc.)

                byte here = SampleBaseTile(x, y);
                if (here != Air) continue;

                int surface = GetSurfaceY(x);
                if (y >= surface - 2) continue; // avoid near-surface floating stones

                // Must have solid ground below (and not lava)
                if (!HasSolidBelow(x, y, startX, startY, sizeX, sizeY, fg)) continue;

                float r = Hash01(x * 92821 + y * 68917 + seed * 1021);
                if (smallStoneCaveTile != null && r < smallStoneCaveChance)
                {
                    fg[idx] = smallStoneCaveTile;
                }
            }
            if ((ly & 15) == 0) yield return null;
        }

        // 5) Write buffers
        var bounds = new BoundsInt(new Vector3Int(startX, startY, 0), new Vector3Int(sizeX, sizeY, 1));
        foregroundTilemap.SetTilesBlock(bounds, fg);
        backgroundTilemap.SetTilesBlock(bounds, bg);
    }

    void ClearChunk(Vector2Int chunkCoord)
    {
        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;
        var bounds = new BoundsInt(new Vector3Int(startX, startY, 0), new Vector3Int(chunkSize, chunkSize, 1));

        foregroundTilemap.SetTilesBlock(bounds, emptyTiles);
        backgroundTilemap.SetTilesBlock(bounds, emptyTiles);

        // Destroy canopy prefabs spawned by this chunk
        if (chunkTreeKeys.TryGetValue(chunkCoord, out var keys))
        {
            foreach (var key in keys)
            {
                if (canopyInstancesByKey.TryGetValue(key, out var go) && go != null)
                {
                    Destroy(go);
                }
                canopyInstancesByKey.Remove(key);
            }
            chunkTreeKeys.Remove(chunkCoord);
        }
    }
    // Checks whether the cell directly below (worldX, worldY-1) exists and is solid ground for decoration.
    // Prefers the current chunk buffer; falls back to the tilemap (neighbor chunk) or procedural sample.
    bool HasSolidBelow(int worldX, int worldY, int chunkStartX, int chunkStartY, int sizeX, int sizeY, TileBase[] fg)
    {
        int belowY = worldY - 1;

        // Within current chunk?
        if (worldX >= chunkStartX && worldX < chunkStartX + sizeX &&
            belowY >= chunkStartY && belowY < chunkStartY + sizeY)
        {
            int lx = worldX - chunkStartX;
            int ly = belowY - chunkStartY;
            int idxBelow = ly * sizeX + lx;

            TileBase t = fg[idxBelow];
            if (t != null)
            {
                return IsForegroundTileSolidForDeco(t); // exclude wood/branch/lava
            }

            // If fg is null here (unlikely for base ground), fall back to base sampling
            byte baseT = SampleBaseTile(worldX, belowY);
            return IsSolidGroundForDeco(baseT);
        }

        // Outside current chunk: check tilemap (neighbor chunk may already be drawn)
        TileBase tmTile = foregroundTilemap.GetTile(new Vector3Int(worldX, belowY, 0));
        if (tmTile != null)
        {
            return IsForegroundTileSolidForDeco(tmTile);
        }

        // Fallback: procedural sample
        byte sample = SampleBaseTile(worldX, belowY);
        return IsSolidGroundForDeco(sample);
    }

    // Treat only terrain solids as "ground" for decorations (no wood/branches/lava)
    bool IsForegroundTileSolidForDeco(TileBase t)
    {
        if (t == null) return false;
        if (t == stoneTile) return true;
        if (t == dirtTile) return true;
        if (t == grassTile) return true;
        if (cloudTile != null && t == cloudTile) return true; // if you use cloud as solid island core
        return false;
    }
    // =========================
    // Terrain sampling
    // =========================

    byte SampleBaseTile(int x, int y)
    {
        // Magma plane
        if (y < magmaPlaneY) return Lava;

        // Floating islands (background bodies carved into air space)
        if (enableIslands && y >= islandsMinY && y <= islandsMaxY)
        {
            float n = Mathf.PerlinNoise(x * islandFrequency + seed * 0.111f, y * islandFrequency + seed * 0.222f);
            if (n > islandThreshold)
            {
                float nAbove = Mathf.PerlinNoise(x * islandFrequency + seed * 0.111f, (y + 1) * islandFrequency + seed * 0.222f);
                if (nAbove <= islandThreshold) return Grass;
                float nAbove2 = Mathf.PerlinNoise(x * islandFrequency + seed * 0.111f, (y + 2) * islandFrequency + seed * 0.222f);
                if (nAbove2 <= islandThreshold) return Dirt;
                return (cloudTile != null) ? Cloud : Stone;
            }
        }

        // Surface height
        int surface = GetSurfaceY(x);
        if (y >= surface) return Air;

        // Surface tunnel entrances (carved regardless of cave roof padding)
        if (IsSurfaceEntranceCarved(x, y, surface)) return Air;

        // Caves: worms + lava carving, not too near surface or magma
        if (y >= magmaPlaneY + caveFloorPadding && y < surface - caveRoofPadding)
        {
            if (IsWormCarved(x, y) || IsLavaCarved(x, y))
                return Air;
        }

        // Stratification
        if (y >= surface - 1) return Grass;
        if (y >= surface - 5) return Dirt;
        return Stone;
    }

    int GetSurfaceY(int x)
    {
        float fx = x * terrainFrequency;
        float h1 = (Mathf.PerlinNoise(fx + seed * 0.173f, 0f) - 0.5f) * (terrainHeightVariation * 2f);
        float h2 = (Mathf.PerlinNoise(fx * 2f + seed * 0.317f, 0f) - 0.5f) * (terrainHeightVariation * 1.0f);
        float h3 = (Mathf.PerlinNoise(fx * 4f + seed * 0.619f, 0f) - 0.5f) * (terrainHeightVariation * 0.5f);
        return groundLevel + Mathf.RoundToInt(h1 + h2 + h3);
    }

    bool IsSurfaceEntranceCarved(int x, int y, int surface)
    {
        int cluster = FloorDiv(x, entranceClusterSize);
        uint h = (uint)(cluster * 1103515245 ^ seed * 12345);
        if (Hash01(h) >= entranceChancePerCluster) return false;

        int offset = Mathf.FloorToInt(Hash01(h * 3u + 1u) * (entranceClusterSize - 6)) + 3;
        float centerX = cluster * entranceClusterSize + offset;

        int depth = Mathf.RoundToInt(Mathf.Lerp(entranceMinDepth, entranceMaxDepth, Hash01(h * 5u + 7u)));
        if (y < surface - depth || y >= surface) return false;

        float d = surface - y; // depth from surface
        float wobble = Mathf.Sin(d * entranceWobbleFreq + Hash01(h * 11u + 17u) * Mathf.PI * 2f) * entranceWobbleAmp;
        float center = centerX + wobble;

        float radius = entranceBaseRadius * entranceTopWiden - entranceTaper * d * entranceBaseRadius;
        radius = Mathf.Max(0.6f, radius);
        radius *= Mathf.Lerp(0.9f, 1.15f, Hash01(Hash((int)(x * 92821 + y * 68917 + seed))));

        return Mathf.Abs(x - center) <= radius;
    }

    bool IsLavaCarved(int x, int y)
    {
        int dy = y - magmaPlaneY;
        if (dy > lavaCarveHeight) return false;

        float t = 1f - Mathf.Clamp01(dy / (float)lavaCarveHeight); // 1 near magma, 0 at top
        float n = Mathf.PerlinNoise(x * lavaChamberFrequency + seed * 0.411f, y * lavaChamberFrequency + seed * 0.733f);
        float ridged = 1f - Mathf.Abs(2f * n - 1f);
        float threshold = Mathf.Lerp(lavaChamberThreshold, lavaChamberThreshold * 0.5f, t);
        return ridged > threshold;
    }

    bool IsWormCarved(int x, int y)
    {
        int rx = Mathf.FloorToInt((float)x / wormRegionSize);
        int ry = Mathf.FloorToInt((float)y / wormRegionSize);

        float nearMagma = 0f;
        if (y < magmaPlaneY + lavaCarveHeight)
            nearMagma = 1f - Mathf.Clamp01((y - magmaPlaneY) / (float)lavaCarveHeight);

        float radiusBoost = 1f + 1.5f * nearMagma;

        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                int crx = rx + ox;
                int cry = ry + oy;

                // Horizontal worms
                for (int i = 0; i < Mathf.Max(0, wormsPerRegionHorizontal); i++)
                {
                    int wseed = Hash(crx * 73856093 ^ cry * 19349663 ^ i * 83492791 ^ seed * 374761393);
                    float baseY = cry * wormRegionSize + Mathf.Lerp(0f, wormRegionSize, Hash01(wseed * 3));
                    float amp = Mathf.Lerp(wormAmplitudeMin, wormAmplitudeMax, Hash01(wseed * 5));
                    float phase = Hash01(wseed * 7) * Mathf.PI * 2f;
                    float wiggle = (Mathf.PerlinNoise(x * 0.05f + wseed * 0.001f, 0f) - 0.5f) * amp * 0.5f;
                    float yCenter = baseY + amp * Mathf.Sin(x * wormCurveFrequency + phase) + wiggle;
                    float radius = wormBaseRadius * Mathf.Lerp(0.8f, 1.4f, Hash01(wseed * 11)) * radiusBoost;
                    if (Mathf.Abs(y - yCenter) <= radius) return true;
                }

                // Vertical worms
                for (int i = 0; i < Mathf.Max(0, wormsPerRegionVertical); i++)
                {
                    int wseed = Hash(cry * 9154889 ^ crx * 192239 ^ i * 274763641 ^ seed * 1013904223);
                    float baseX = crx * wormRegionSize + Mathf.Lerp(0f, wormRegionSize, Hash01(wseed * 3));
                    float amp = Mathf.Lerp(wormAmplitudeMin * 0.7f, wormAmplitudeMax * 0.8f, Hash01(wseed * 5));
                    float phase = Hash01(wseed * 7) * Mathf.PI * 2f;
                    float wiggle = (Mathf.PerlinNoise(y * 0.05f + wseed * 0.001f, 0f) - 0.5f) * amp * 0.5f;
                    float xCenter = baseX + amp * Mathf.Sin(y * wormCurveFrequency + phase) + wiggle;
                    float radius = wormBaseRadius * Mathf.Lerp(0.8f, 1.4f, Hash01(wseed * 11)) * radiusBoost;
                    if (Mathf.Abs(x - xCenter) <= radius) return true;
                }
            }
        }
        return false;
    }

    // =========================
    // Trees
    // =========================

    struct Tree
    {
        public int x;
        public int surface;
        public int height;
        public int trunkWidth;
        public bool isGiant;
    }

    List<Tree> EnumerateTreesInRange(int xMinInclusive, int xMaxInclusive)
    {
        var list = new List<Tree>();
        for (int x = xMinInclusive; x <= xMaxInclusive; x++)
        {
            bool giant = false;
            int trunkWidth = normalTrunkWidth;
            int height = 0;
            int surface = GetSurfaceY(x);

            // Avoid cliffs
            int sL = GetSurfaceY(x - 1);
            int sR = GetSurfaceY(x + 1);
            if (Mathf.Abs(sL - sR) > 3) continue;

            if (enableGiantTrees)
            {
                int cluster = FloorDiv(x, giantClusterSize);
                float clusterRng = Hash01((int)(cluster * 2654435761u ^ (uint)(seed * 1013904223)));
                if (clusterRng < giantTreeChancePerCluster)
                {
                    int centerOffset = Mathf.FloorToInt(Hash01(cluster * 97531 ^ seed * 50123) * (giantClusterSize - 8)) + 4;
                    int centerX = cluster * giantClusterSize + centerOffset;
                    if (Mathf.Abs(x - centerX) <= 1)
                    {
                        giant = true;
                        trunkWidth = Mathf.Clamp(
                            Mathf.RoundToInt(Mathf.Lerp(giantMinTrunkWidth, giantMaxTrunkWidth, Hash01(cluster * 1237 ^ seed * 8911))),
                            giantMinTrunkWidth, giantMaxTrunkWidth);
                        height = Mathf.RoundToInt(Mathf.Lerp(giantMinHeight, giantMaxHeight, Hash01(cluster * 3571 ^ seed * 9623)));
                    }
                }
            }

            if (!giant && enableTrees)
            {
                float rng = Hash01(x * 73856093 ^ seed * 19349663);
                if (rng < normalTreeChancePerColumn)
                {
                    height = Mathf.RoundToInt(Mathf.Lerp(minTreeHeight, maxTreeHeight, Hash01(x * 83492791 ^ seed * 374761393)));
                    trunkWidth = Mathf.Max(1, normalTrunkWidth);
                }
            }

            if (height > 0 && surface > magmaPlaneY + 2)
            {
                list.Add(new Tree
                {
                    x = x,
                    surface = surface,
                    height = height,
                    trunkWidth = trunkWidth,
                    isGiant = giant
                });
            }
        }
        return list;
    }

    void PlotBranchIntoBuffer(int startX, int startY, int dir, int length, bool isGiant,
                              int chunkStartX, int chunkStartY, int sizeX, int sizeY, TileBase[] fg)
    {
        if (length <= 0) return;

        float slope = branchSlopePerTile * Mathf.Lerp(0.5f, isGiant ? 1.2f : 0.9f, Hash01(startX * 13 + startY * 29 + seed));
        float wig = branchWiggle;

        int endX = startX + dir * length;
        int minX = Mathf.Min(startX, endX) - branchThickness;
        int maxX = Mathf.Max(startX, endX) + branchThickness;

        if (maxX < chunkStartX || minX >= chunkStartX + sizeX) return;

        for (int i = 0; i <= length; i++)
        {
            int bx = startX + dir * i;
            float rise = slope * i + (Mathf.PerlinNoise((bx + seed) * 0.15f, (startY + seed) * 0.15f) - 0.5f) * wig * length;
            int by = startY + Mathf.RoundToInt(rise);

            for (int t = -branchThickness / 2; t <= branchThickness / 2; t++)
            {
                int px = bx;
                int py = by + t;

                if (px < chunkStartX || px >= chunkStartX + sizeX || py < chunkStartY || py >= chunkStartY + sizeY)
                    continue;

                int lx = px - chunkStartX;
                int ly = py - chunkStartY;
                int idx = ly * sizeX + lx;

                byte baseT = SampleBaseTile(px, py);
                if (baseT == Air)
                    fg[idx] = ToForegroundTile(Branch);
            }
        }
    }

    void PlotCanopyIntoBackground(int cx, int cy, int radius, bool isGiant,
                                  int chunkStartX, int chunkStartY, int sizeX, int sizeY, TileBase[] bg)
    {
        int minX = Mathf.Max(chunkStartX, cx - radius - 2);
        int maxX = Mathf.Min(chunkStartX + sizeX - 1, cx + radius + 2);
        int minY = Mathf.Max(chunkStartY, cy - radius - 2);
        int maxY = Mathf.Min(chunkStartY + sizeY - 1, cy + radius + 2);

        float r2 = radius * radius;
        float irregularity = canopyEdgeNoise;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                float dist2 = dx * dx + dy * dy;
                if (dist2 > r2 * 1.3f) continue;

                float edge = 1f + (Mathf.PerlinNoise(x * 0.1f + seed * 0.01f, y * 0.1f + seed * 0.02f) - 0.5f) * irregularity * 2f;
                if (dist2 <= (radius * edge) * (radius * edge))
                {
                    byte baseT = SampleBaseTile(x, y);
                    if (baseT != Air) continue;

                    int lx = x - chunkStartX;
                    int ly = y - chunkStartY;
                    bg[ly * sizeX + lx] = leafBackgroundTile;
                }
            }
        }
    }

    // =========================
    // Utils
    // =========================

    bool IsSolidGroundForDeco(byte t)
    {
        return t == Stone || t == Dirt || t == Grass || t == Cloud;
    }

    long ComposeTreeKey(int x, int surfaceY)
    {
        return ((long)x << 32) ^ (uint)surfaceY ^ ((long)seed << 1);
    }

    TileBase ToForegroundTile(byte t)
    {
        switch (t)
        {
            case Dirt: return dirtTile;
            case Grass: return grassTile;
            case Stone: return stoneTile;
            case Lava: return lavaTile;
            case Wood: return woodTile != null ? woodTile : stoneTile;
            case Branch: return branchTile != null ? branchTile : (woodTile != null ? woodTile : stoneTile);
            case Cloud: return cloudTile != null ? cloudTile : stoneTile;
            default: return null; // Air
        }
    }

    static int FloorDiv(int a, int b)
    {
        if (b < 0) { a = -a; b = -b; }
        int q = a / b;
        int r = a % b;
        if ((r != 0) && ((r < 0) != (b < 0))) --q;
        return q;
    }

    static int Hash(int x)
    {
        unchecked
        {
            uint u = (uint)x;
            u ^= 2747636419u;
            u *= 2654435769u;
            u ^= u >> 16;
            u *= 2654435769u;
            u ^= u >> 16;
            return (int)u;
        }
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
            return (u & 0xFFFFFF) / 16777215f;
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
}
