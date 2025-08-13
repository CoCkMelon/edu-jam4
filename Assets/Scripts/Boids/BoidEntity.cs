using UnityEngine;

public class BoidEntity : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 3f;
    
    [Header("Boid Behavior Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1.0f;
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float avoidPlayerWeight = 2.0f;
    
    [Header("Perception Ranges")]
    [SerializeField] private float perceptionRadius = 2.5f;
    [SerializeField] private float separationRadius = 1.0f;
    [SerializeField] private float playerAvoidRadius = 3.0f;
    
    private Vector2 velocity;
    private Vector2 acceleration;
    private BoidFlockManager flockManager;
    private Transform playerTransform;
    
    private void Start()
    {
        flockManager = FindObjectOfType<BoidFlockManager>();
        
        // Initialize with random velocity
        velocity = Random.insideUnitCircle * maxSpeed;
        
        // Set random color for visual variety
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(0.5f, 1f)
            );
        }
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }
    
    private void Update()
    {
        if (flockManager == null) return;
        
        // Get nearby boids
        var nearbyBoids = flockManager.GetNearbyBoids(this, perceptionRadius);
        
        // Calculate forces
        Vector2 separation = CalculateSeparation(nearbyBoids) * separationWeight;
        Vector2 alignment = CalculateAlignment(nearbyBoids) * alignmentWeight;
        Vector2 cohesion = CalculateCohesion(nearbyBoids) * cohesionWeight;
        Vector2 bounds = CalculateBounds() * 2f;
        Vector2 avoidPlayer = CalculatePlayerAvoidance() * avoidPlayerWeight;
        
        // Apply forces
        acceleration = separation + alignment + cohesion + bounds + avoidPlayer;
        
        // Update physics
        velocity += acceleration * Time.deltaTime;
        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        
        transform.position += (Vector3)velocity * Time.deltaTime;
        
        // Rotate to face direction
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 10f);
        }
        
        // Reset acceleration
        acceleration = Vector2.zero;
    }
    
    private Vector2 CalculateSeparation(BoidEntity[] nearbyBoids)
    {
        Vector2 steer = Vector2.zero;
        int count = 0;
        
        foreach (var boid in nearbyBoids)
        {
            float distance = Vector2.Distance(transform.position, boid.transform.position);
            if (distance > 0 && distance < separationRadius)
            {
                Vector2 diff = (Vector2)(transform.position - boid.transform.position);
                diff = diff.normalized / distance; // Weight by distance
                steer += diff;
                count++;
            }
        }
        
        if (count > 0)
        {
            steer /= count;
            steer = steer.normalized * maxSpeed;
            steer -= velocity;
            steer = Vector2.ClampMagnitude(steer, maxForce);
        }
        
        return steer;
    }
    
    private Vector2 CalculateAlignment(BoidEntity[] nearbyBoids)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        
        foreach (var boid in nearbyBoids)
        {
            sum += boid.velocity;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            sum = sum.normalized * maxSpeed;
            Vector2 steer = sum - velocity;
            return Vector2.ClampMagnitude(steer, maxForce);
        }
        
        return Vector2.zero;
    }
    
    private Vector2 CalculateCohesion(BoidEntity[] nearbyBoids)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        
        foreach (var boid in nearbyBoids)
        {
            sum += (Vector2)boid.transform.position;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            return Seek(sum);
        }
        
        return Vector2.zero;
    }
    
    private Vector2 CalculateBounds()
    {
        if (flockManager == null) return Vector2.zero;
        
        Vector2 steer = Vector2.zero;
        Vector2 pos = transform.position;
        Bounds bounds = flockManager.GetFlockBounds();
        
        if (pos.x < bounds.min.x + 1f)
            steer.x = maxSpeed;
        else if (pos.x > bounds.max.x - 1f)
            steer.x = -maxSpeed;
            
        if (pos.y < bounds.min.y + 1f)
            steer.y = maxSpeed;
        else if (pos.y > bounds.max.y - 1f)
            steer.y = -maxSpeed;
            
        return steer;
    }
    
    private Vector2 CalculatePlayerAvoidance()
    {
        
        if (playerTransform == null) return Vector2.zero;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        
        if (distance < playerAvoidRadius)
        {
            Vector2 flee = (Vector2)(transform.position - playerTransform.position);
            flee = flee.normalized * maxSpeed;
            Vector2 steer = flee - velocity;
            float weight = 1f - (distance / playerAvoidRadius); // Stronger avoidance when closer
            return Vector2.ClampMagnitude(steer, maxForce) * weight;
        }
        
        return Vector2.zero;
    }
    
    private Vector2 Seek(Vector2 target)
    {
        Vector2 desired = target - (Vector2)transform.position;
        desired = desired.normalized * maxSpeed;
        Vector2 steer = desired - velocity;
        return Vector2.ClampMagnitude(steer, maxForce);
    }
    
    public Vector2 GetVelocity()
    {
        return velocity;
    }
}
