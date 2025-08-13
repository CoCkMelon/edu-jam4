using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BoidFlockManager : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private int flockSize = 30;
    [SerializeField] private Vector2 spawnBoundsMin = new Vector2(-10, -10);
    [SerializeField] private Vector2 spawnBoundsMax = new Vector2(10, 10);
    
    [Header("Flock Boundaries")]
    [SerializeField] private Vector2 flockBoundsMin = new Vector2(-15, -15);
    [SerializeField] private Vector2 flockBoundsMax = new Vector2(15, 15);
    
    private List<BoidEntity> allBoids = new List<BoidEntity>();
    private Bounds flockBounds;
    
    private void Start()
    {
        flockBounds = new Bounds();
        flockBounds.SetMinMax(flockBoundsMin, flockBoundsMax);
        
        SpawnFlock();
        DrawBounds();
    }
    
    private void SpawnFlock()
    {
        for (int i = 0; i < flockSize; i++)
        {
            Vector2 spawnPosition = new Vector2(
                Random.Range(spawnBoundsMin.x, spawnBoundsMax.x),
                Random.Range(spawnBoundsMin.y, spawnBoundsMax.y)
            );
            
            GameObject boidObj = Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
            boidObj.name = $"Boid_{i}";
            
            BoidEntity boid = boidObj.GetComponent<BoidEntity>();
            if (boid != null)
            {
                allBoids.Add(boid);
            }
        }
    }
    
    public BoidEntity[] GetNearbyBoids(BoidEntity boid, float radius)
    {
        return allBoids
            .Where(b => b != boid && Vector2.Distance(boid.transform.position, b.transform.position) < radius)
            .ToArray();
    }
    
    public Bounds GetFlockBounds()
    {
        return flockBounds;
    }
    
    private void DrawBounds()
    {
        // Create boundary visualization
        GameObject boundaryHolder = new GameObject("BoundaryVisualizer");
        
        // Top boundary
        CreateBoundaryLine(
            new Vector2(flockBoundsMin.x, flockBoundsMax.y),
            new Vector2(flockBoundsMax.x, flockBoundsMax.y),
            boundaryHolder.transform
        );
        
        // Bottom boundary
        CreateBoundaryLine(
            new Vector2(flockBoundsMin.x, flockBoundsMin.y),
            new Vector2(flockBoundsMax.x, flockBoundsMin.y),
            boundaryHolder.transform
        );
        
        // Left boundary
        CreateBoundaryLine(
            new Vector2(flockBoundsMin.x, flockBoundsMin.y),
            new Vector2(flockBoundsMin.x, flockBoundsMax.y),
            boundaryHolder.transform
        );
        
        // Right boundary
        CreateBoundaryLine(
            new Vector2(flockBoundsMax.x, flockBoundsMin.y),
            new Vector2(flockBoundsMax.x, flockBoundsMax.y),
            boundaryHolder.transform
        );
    }
    
    private void CreateBoundaryLine(Vector2 start, Vector2 end, Transform parent)
    {
        GameObject line = new GameObject("BoundaryLine");
        line.transform.parent = parent;
        
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 1f, 0.3f);
        lr.endColor = new Color(1f, 1f, 1f, 0.3f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = -1;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            (flockBoundsMin + flockBoundsMax) / 2,
            flockBoundsMax - flockBoundsMin
        );
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            (spawnBoundsMin + spawnBoundsMax) / 2,
            spawnBoundsMax - spawnBoundsMin
        );
    }
}