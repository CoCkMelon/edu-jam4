using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
// ChunkedWorldGenerator.cs
public class ChunkedWorldGenerator : MonoBehaviour
{
    public Tilemap groundTilemap;
    public TileBase dirtTile;
    public TileBase stoneTile;
    public int chunkSize = 100; // Process in 100x100 chunks

    private Texture2D noiseTexture;
    private NoiseGenerator noiseGenerator;

    void Start()
    {
        noiseGenerator = FindObjectOfType<NoiseGenerator>();

        noiseTexture = noiseGenerator.ReadNoiseTexture();
        GenerateWorldInChunks();
    }

    void GenerateWorldInChunks()
    {
        int chunksX = Mathf.CeilToInt(noiseTexture.width / (float)chunkSize);
        int chunksY = Mathf.CeilToInt(noiseTexture.height / (float)chunkSize);

        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                GenerateChunk(cx, cy);
                // Yield for WebGL (simulate async)
                StartCoroutine(ChunkCoroutine(cx, cy));
            }
        }
    }

    IEnumerator ChunkCoroutine(int cx, int cy)
    {
        // Simulate async processing
        yield return new WaitForSeconds(0.01f);
        GenerateChunk(cx, cy);
    }

    void GenerateChunk(int cx, int cy)
    {
        int startX = cx * chunkSize;
        int startY = cy * chunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = startX + x;
                int worldY = startY + y;

                if (worldX < noiseTexture.width && worldY < noiseTexture.height)
                {
                    float noiseValue = noiseTexture.GetPixel(worldX, worldY).r;
                    Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);

                    if (noiseValue > 0.5f)
                    {
                        groundTilemap.SetTile(tilePos, dirtTile);
                    }
                    else
                    {
                        groundTilemap.SetTile(tilePos, stoneTile);
                    }
                }
            }
        }
    }
}
