
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap groundTilemap;
    public Tilemap backgroundTilemap;

    [Header("Tile Assets")]
    public TileBase dirtTile;
    public TileBase grassTile;
    public TileBase stoneTile;
    public TileBase woodTile;
    public TileBase leafTile;
    public TileBase sandTile;
    public TileBase cloudTile;
    public TileBase lavaTile; // New magma tile

    [Header("World Settings")]
    public int worldWidth = 500;
    public int worldHeight = 300;
    public int groundLevel = 150;
    public int seed = 12345;

    [Header("Terrain Settings")]
    [Range(10, 100)]
    public float terrainHeightVariation = 30f;
    [Range(0.001f, 0.1f)]
    public float terrainFrequency = 0.02f;

    [Header("Stone Layer Settings")]
    public int stoneLayerThickness = 50;
    public float stoneNoiseFrequency = 0.05f;

    [Header("Cave Settings")]
    [Range(0.0f, 1.0f)]
    public float caveThreshold = 0.4f;
    [Range(0.01f, 0.2f)]
    public float caveFrequency = 0.05f;

    [Header("Tree Settings")]
    [Range(0, 100)]
    public int normalTreeCount = 50;
    [Range(0, 20)]
    public int giantTreeCount = 5;
    [Range(5, 15)]
    public int minTreeHeight = 6;
    [Range(10, 30)]
    public int maxTreeHeight = 12;
    [Range(25, 80)]
    public int giantTreeMinHeight = 25;
    [Range(30, 80)]
    public int giantTreeMaxHeight = 40;

    [Header("Floating Island Settings")]
    [Range(0, 20)]
    public int floatingIslandCount = 8;
    [Range(100, 300)]
    public int floatingIslandMinHeight = 180;
    [Range(150, 400)]
    public int floatingIslandMaxHeight = 250;

    [Header("Magma Settings")]
    public int magmaLayerDepth = 10;
    public float magmaLakeFrequency = 0.3f;

    private int[,] worldData;
    private List<Vector2Int> surfacePoints = new List<Vector2Int>();

    void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("Missing tile references! Please assign all tiles in the inspector.");
            return;
        }

        GenerateWorld();
    }

    bool ValidateReferences()
    {
        return groundTilemap != null &&
               dirtTile != null &&
               grassTile != null &&
               stoneTile != null &&
               lavaTile != null; // Validate magma tile
    }

    void GenerateWorld()
    {
        Debug.Log($"Generating world: {worldWidth}x{worldHeight}");

        Random.InitState(seed);
        worldData = new int[worldWidth, worldHeight];

        // Clear tilemaps first
        groundTilemap.CompressBounds();
        if (backgroundTilemap != null)
            backgroundTilemap.CompressBounds();

        // Generation steps
        GenerateTerrain();
        GenerateStoneLayer(); // New dedicated stone layer
        GenerateMagmaLayer();
        GenerateCaves();
        GenerateFloatingIslands();
        PlaceTrees();
        PlaceGiantTrees();

        // Render the world
        RenderWorld();

        Debug.Log($"World generation complete! Bounds: {groundTilemap.cellBounds}");
    }

    void GenerateTerrain()
    {
        // Generate base terrain with centered noise
        for (int x = 0; x < worldWidth; x++)
        {
            // Centered noise (-variation to +variation)
            float height1 = (Mathf.PerlinNoise(x * terrainFrequency, seed) - 0.5f) * terrainHeightVariation * 2;
            float height2 = (Mathf.PerlinNoise(x * terrainFrequency * 2, seed + 1000) - 0.5f) * (terrainHeightVariation * 0.5f) * 2;
            float height3 = (Mathf.PerlinNoise(x * terrainFrequency * 4, seed + 2000) - 0.5f) * (terrainHeightVariation * 0.25f) * 2;

            float totalHeight = height1 + height2 + height3;
            int surfaceY = groundLevel + Mathf.RoundToInt(totalHeight);

            // Ensure surfaceY stays within world bounds
            surfaceY = Mathf.Clamp(surfaceY, 5, worldHeight - 1);

            surfacePoints.Add(new Vector2Int(x, surfaceY));

            for (int y = 0; y < worldHeight; y++)
            {
                if (y < surfaceY)
                {
                    if (y < surfaceY - 5)
                    {
                        worldData[x, y] = 0; // Dirt
                    }
                    else if (y < surfaceY)
                    {
                        worldData[x, y] = 1; // Grass
                    }
                }
                else
                {
                    worldData[x, y] = -1; // Air
                }
            }
        }
    }

    void GenerateStoneLayer()
    {
        // Fill all blocks below terrain with stone
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                if (worldData[x, y] == -1 && y < GetSurfaceHeight(x))
                {
                    worldData[x, y] = 2; // Stone
                }
            }
        }

        // Add some variation to stone layer thickness
        for (int x = 0; x < worldWidth; x++)
        {
            float noise = Mathf.PerlinNoise(x * stoneNoiseFrequency, seed + 3000);
            int stoneThickness = stoneLayerThickness + Mathf.RoundToInt(noise * 20 - 10);

            for (int y = 0; y < stoneThickness; y++)
            {
                if (y < worldHeight)
                {
                    worldData[x, y] = 2; // Stone
                }
            }
        }
    }

    void GenerateMagmaLayer()
    {
        // Solid magma layer at the bottom
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < magmaLayerDepth; y++)
            {
                worldData[x, y] = 6; // Magma
            }
        }

        // Add magma lakes using noise
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = magmaLayerDepth; y < magmaLayerDepth * 3; y++)
            {
                float lakeNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                if (lakeNoise < magmaLakeFrequency && y < worldHeight)
                {
                    worldData[x, y] = 6; // Magma
                }
            }
        }
    }

    void GenerateCaves()
    {
        for (int x = 1; x < worldWidth - 1; x++)
        {
            for (int y = 1; y < worldHeight - 1; y++)
            {
                float cave1 = Mathf.PerlinNoise(x * caveFrequency, y * caveFrequency);
                float cave2 = Mathf.PerlinNoise(x * caveFrequency * 2 + 100, y * caveFrequency * 2 + 100);

                float caveValue = (cave1 + cave2) / 2f;

                if (caveValue < caveThreshold && worldData[x, y] == 2)
                {
                    worldData[x, y] = -1; // Air
                }
            }
        }
    }


    void GenerateFloatingIslands()
    {
        for (int i = 0; i < floatingIslandCount; i++)
        {
            int islandX = Random.Range(50, worldWidth - 50);
            int islandY = Random.Range(floatingIslandMinHeight, floatingIslandMaxHeight);
            int islandWidth = Random.Range(15, 40);
            int islandHeight = Random.Range(8, 15);

            CreateFloatingIsland(islandX, islandY, islandWidth, islandHeight);
        }
    }

    void CreateFloatingIsland(int centerX, int centerY, int width, int height)
    {
        // Create an elliptical island shape
        for (int x = -width/2; x <= width/2; x++)
        {
            for (int y = -height/2; y <= height/2; y++)
            {
                int worldX = centerX + x;
                int worldY = centerY + y;

                if (worldX >= 0 && worldX < worldWidth && worldY >= 0 && worldY < worldHeight)
                {
                    // Ellipse equation
                    float distX = (float)x / (width/2);
                    float distY = (float)y / (height/2);

                    if (distX * distX + distY * distY <= 1)
                    {
                        // Add some randomness to the edges
                        if (Random.Range(0f, 1f) < 0.9f || (distX * distX + distY * distY < 0.7f))
                        {
                            if (y > 0)
                                worldData[worldX, worldY] = 1; // Grass on top
                            else if (y > -2)
                                worldData[worldX, worldY] = 0; // Dirt
                            else
                                worldData[worldX, worldY] = cloudTile != null ? 5 : 2; // Cloud or stone
                        }
                    }
                }
            }
        }

        // Add a tree on top of the island
        if (Random.Range(0f, 1f) < 0.7f && woodTile != null)
        {
            PlaceTree(centerX, centerY + height/2 + 1, Random.Range(5, 10));
        }
    }

    void PlaceTrees()
    {
        if (woodTile == null || leafTile == null) return;

        for (int i = 0; i < normalTreeCount; i++)
        {
            int x = Random.Range(10, worldWidth - 10);

            // Find surface at this x position
            int surfaceY = GetSurfaceHeight(x);
            if (surfaceY > 0 && surfaceY < worldHeight - maxTreeHeight)
            {
                int treeHeight = Random.Range(minTreeHeight, maxTreeHeight);
                PlaceTree(x, surfaceY, treeHeight);
            }
        }
    }

    void PlaceGiantTrees()
    {
        if (woodTile == null || leafTile == null) return;

        for (int i = 0; i < giantTreeCount; i++)
        {
            int x = Random.Range(20, worldWidth - 20);

            // Find surface at this x position
            int surfaceY = GetSurfaceHeight(x);
            if (surfaceY > 0 && surfaceY < worldHeight - giantTreeMaxHeight)
            {
                int treeHeight = Random.Range(giantTreeMinHeight, giantTreeMaxHeight);
                PlaceGiantTree(x, surfaceY, treeHeight);
            }
        }
    }

    void PlaceTree(int x, int groundY, int height)
    {
        // Place trunk
        for (int y = groundY; y < groundY + height; y++)
        {
            if (y < worldHeight)
                worldData[x, y] = 3; // Wood
        }

        // Place leaves (crown)
        int crownSize = height / 3;
        for (int dx = -crownSize; dx <= crownSize; dx++)
        {
            for (int dy = -crownSize; dy <= crownSize; dy++)
            {
                int leafX = x + dx;
                int leafY = groundY + height + dy;

                if (leafX >= 0 && leafX < worldWidth && leafY >= 0 && leafY < worldHeight)
                {
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance <= crownSize && worldData[leafX, leafY] == -1)
                    {
                        worldData[leafX, leafY] = 4; // Leaves
                    }
                }
            }
        }
    }

    void PlaceGiantTree(int x, int groundY, int height)
    {
        // Giant trunk (3-5 blocks wide)
        int trunkWidth = Random.Range(3, 6);

        // Place thick trunk
        for (int dx = -trunkWidth/2; dx <= trunkWidth/2; dx++)
        {
            for (int y = groundY; y < groundY + height; y++)
            {
                if (x + dx >= 0 && x + dx < worldWidth && y < worldHeight)
                {
                    worldData[x + dx, y] = 3; // Wood
                }
            }
        }

        // Place roots
        for (int dx = -trunkWidth; dx <= trunkWidth; dx++)
        {
            int rootLength = Random.Range(3, 8);
            for (int dy = 0; dy < rootLength; dy++)
            {
                int rootX = x + dx * 2;
                int rootY = groundY - dy;
                if (rootX >= 0 && rootX < worldWidth && rootY >= 0)
                {
                    if (worldData[rootX, rootY] != -1)
                        worldData[rootX, rootY] = 3; // Wood roots
                }
            }
        }

        // Place massive crown
        int crownRadius = height / 2;
        for (int dx = -crownRadius; dx <= crownRadius; dx++)
        {
            for (int dy = -crownRadius/2; dy <= crownRadius; dy++)
            {
                int leafX = x + dx;
                int leafY = groundY + height + dy;

                if (leafX >= 0 && leafX < worldWidth && leafY >= 0 && leafY < worldHeight)
                {
                    // Create irregular crown shape
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float noise = Mathf.PerlinNoise(leafX * 0.1f, leafY * 0.1f);

                    if (distance <= crownRadius * (0.7f + noise * 0.3f) && worldData[leafX, leafY] == -1)
                    {
                        worldData[leafX, leafY] = 4; // Leaves
                    }
                }
            }
        }

        // Add branches
        int branchCount = Random.Range(3, 6);
        for (int i = 0; i < branchCount; i++)
        {
            int branchY = groundY + Random.Range(height/2, height - 5);
            int branchLength = Random.Range(5, 15);
            int branchDir = Random.Range(0, 2) == 0 ? -1 : 1;

            for (int b = 0; b < branchLength; b++)
            {
                int bx = x + (b * branchDir);
                int by = branchY + b/3;

                if (bx >= 0 && bx < worldWidth && by >= 0 && by < worldHeight)
                {
                    worldData[bx, by] = 3; // Wood branch

                    // Add leaves around branch
                    for (int lx = -2; lx <= 2; lx++)
                    {
                        for (int ly = -2; ly <= 2; ly++)
                        {
                            int leafX = bx + lx;
                            int leafY = by + ly;
                            if (leafX >= 0 && leafX < worldWidth && leafY >= 0 && leafY < worldHeight)
                            {
                                if (worldData[leafX, leafY] == -1 && Random.Range(0f, 1f) < 0.5f)
                                    worldData[leafX, leafY] = 4; // Leaves
                            }
                        }
                    }
                }
            }
        }
    }

    int GetSurfaceHeight(int x)
    {
        for (int y = worldHeight - 1; y >= 0; y--)
        {
            if (worldData[x, y] != -1)
            {
                return y + 1;
            }
        }
        return -1;
    }

    void RenderWorld()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                Vector3Int position = new Vector3Int(x - worldWidth/2, y - worldHeight/2, 0);
                TileBase tile = GetTile(worldData[x, y]);

                if (tile != null)
                {
                    groundTilemap.SetTile(position, tile);
                }
            }
        }
    }


    TileBase GetTile(int tileType)
    {
        switch (tileType)
        {
            case 0: return dirtTile;
            case 1: return grassTile;
            case 2: return stoneTile;
            case 3: return woodTile;
            case 4: return leafTile;
            case 5: return cloudTile != null ? cloudTile : stoneTile;
            case 6: return lavaTile;
            default: return null;
        }
    }
}
