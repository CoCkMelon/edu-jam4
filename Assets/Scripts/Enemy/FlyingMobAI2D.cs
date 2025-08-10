using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class FlyingMobAI2D : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private float nodeReachDistance = 0.5f;
    [SerializeField] private LayerMask obstacleLayer = 1;
    [SerializeField] private Tilemap obstacleTilemap;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float hoverHeight = 1f;
    [SerializeField] private float avoidanceDistance = 2f;
    
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer = 8;
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private bool flipWithDirection = true;
    
    [Header("Debug")]
    [SerializeField] private bool showPathGizmos = true;
    [SerializeField] private Color pathColor = Color.red;
    
    // Components
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Transform player;
    
    // Pathfinding
    private List<Vector2> currentPath = new List<Vector2>();
    private int currentPathIndex = 0;
    private float lastPathUpdateTime;
    private Vector2 lastPlayerPosition;
    
    // State
    private enum AIState { Idle, Chasing, Attacking, Returning }
    private AIState currentState = AIState.Idle;
    private float lastAttackTime;
    private Vector2 startPosition;
    private bool facingRight = true;
    
    // Properties
    public bool IsChasing => currentState == AIState.Chasing;
    public bool IsAttacking => currentState == AIState.Attacking;
    public float DistanceToPlayer => player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
            
        startPosition = transform.position;
    }
    
    private void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("FlyingMobAI2D: Player not found! Make sure player has 'Player' tag.");
            
        // Auto-find obstacle tilemap if not assigned
        if (obstacleTilemap == null)
        {
            Grid grid = FindObjectOfType<Grid>();
            if (grid != null)
            {
                Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
                obstacleTilemap = tilemaps.FirstOrDefault(t => t.gameObject.layer == obstacleLayer);
            }
        }
    }
    
    private void Update()
    {
        if (player == null) return;
        
        UpdateAIState();
        HandleMovement();
        UpdateAnimations();
        FlipSprite();
    }
    
    private void UpdateAIState()
    {
        float distanceToPlayer = DistanceToPlayer;
        
        switch (currentState)
        {
            case AIState.Idle:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = AIState.Chasing;
                    UpdatePathToPlayer();
                }
                break;
                
            case AIState.Chasing:
                if (distanceToPlayer <= attackRange)
                {
                    currentState = AIState.Attacking;
                }
                else if (distanceToPlayer > detectionRange * 1.5f)
                {
                    currentState = AIState.Returning;
                    UpdatePathToStart();
                }
                else if (Time.time - lastPathUpdateTime > pathUpdateInterval || 
                         Vector2.Distance(player.position, lastPlayerPosition) > 2f)
                {
                    UpdatePathToPlayer();
                }
                break;
                
            case AIState.Attacking:
                if (distanceToPlayer > attackRange)
                {
                    currentState = AIState.Chasing;
                }
                else if (Time.time - lastAttackTime > attackCooldown)
                {
                    Attack();
                }
                break;
                
            case AIState.Returning:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = AIState.Chasing;
                    UpdatePathToPlayer();
                }
                else if (currentPath.Count == 0 || Vector2.Distance(transform.position, startPosition) < 1f)
                {
                    currentState = AIState.Idle;
                    currentPath.Clear();
                }
                break;
        }
    }
    
    private void HandleMovement()
    {
        if (currentState == AIState.Attacking || currentState == AIState.Idle)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, acceleration * Time.deltaTime);
            return;
        }
        
        Vector2 targetPosition = GetTargetPosition();
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        
        // Avoid obstacles
        direction = ApplyObstacleAvoidance(direction);
        
        // Apply movement
        Vector2 targetVelocity = direction * moveSpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        // Limit max speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        
        // Hover behavior
        if (currentState == AIState.Chasing && hoverHeight > 0)
        {
            ApplyHoverBehavior();
        }
    }
    
    private Vector2 GetTargetPosition()
    {
        if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            return currentPath[currentPathIndex];
        }
        
        return currentState == AIState.Returning ? startPosition : (Vector2)player.position;
    }
    
    private Vector2 ApplyObstacleAvoidance(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, 
            col.radius + avoidanceDistance, 
            direction, 
            avoidanceDistance, 
            obstacleLayer
        );
        
        if (hit.collider != null)
        {
            // Calculate avoidance vector
            Vector2 avoidanceVector = Vector2.Perpendicular(hit.normal).normalized;
            
            // Choose the direction that leads closer to target
            Vector2 targetPos = GetTargetPosition();
            float dot1 = Vector2.Dot(avoidanceVector, (targetPos - (Vector2)transform.position).normalized);
            float dot2 = Vector2.Dot(-avoidanceVector, (targetPos - (Vector2)transform.position).normalized);
            
            return dot1 > dot2 ? avoidanceVector : -avoidanceVector;
        }
        
        return direction;
    }
    
    private void ApplyHoverBehavior()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position, 
            Vector2.down, 
            hoverHeight + 1f, 
            obstacleLayer
        );
        
        if (groundHit.collider != null)
        {
            float distanceToGround = groundHit.distance;
            float hoverForce = (hoverHeight - distanceToGround) * 2f;
            rb.AddForce(Vector2.up * hoverForce, ForceMode2D.Force);
        }
    }
    
    private void UpdatePathToPlayer()
    {
        if (player == null) return;
        
        lastPlayerPosition = player.position;
        lastPathUpdateTime = Time.time;
        
        Vector2 start = transform.position;
        Vector2 end = player.position;
        
        // Simple A* pathfinding for 2D
        currentPath = FindPath(start, end);
        currentPathIndex = 0;
    }
    
    private void UpdatePathToStart()
    {
        Vector2 start = transform.position;
        Vector2 end = startPosition;
        
        currentPath = FindPath(start, end);
        currentPathIndex = 0;
    }
    
    private List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        List<Vector2> path = new List<Vector2>();
        
        // Simple straight line path with obstacle avoidance
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        
        // Check if direct path is clear
        RaycastHit2D hit = Physics2D.Linecast(start, end, obstacleLayer);
        if (hit.collider == null)
        {
            path.Add(end);
            return path;
        }
        
        // Generate waypoint path around obstacles
        int waypointCount = Mathf.Max(2, Mathf.CeilToInt(distance / 2f));
        for (int i = 0; i <= waypointCount; i++)
        {
            float t = (float)i / waypointCount;
            Vector2 waypoint = Vector2.Lerp(start, end, t);
            
            // Add some vertical offset to avoid ground obstacles
            waypoint.y += Mathf.Sin(t * Mathf.PI) * 2f;
            
            // Check if waypoint is valid
            if (!Physics2D.OverlapCircle(waypoint, col.radius, obstacleLayer))
            {
                path.Add(waypoint);
            }
        }
        
        return path;
    }
    
    private void Attack()
    {
        lastAttackTime = Time.time;
        
        // Deal damage to player
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider != null)
        {
            // Assuming player has health component
            // playerCollider.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Debug.Log($"Flying mob attacked player for {damage} damage!");
        }
        
        animator.SetTrigger("Attack");
    }
    
    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", rb.linearVelocity.magnitude);
            animator.SetBool("IsChasing", IsChasing);
            animator.SetBool("IsAttacking", IsAttacking);
        }
    }
    
    private void FlipSprite()
    {
        if (!flipWithDirection || spriteRenderer == null) return;
        
        float xVelocity = rb.linearVelocity.x;
        if (Mathf.Abs(xVelocity) > 0.1f)
        {
            bool shouldFaceRight = xVelocity > 0;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                spriteRenderer.flipX = !facingRight;
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Check if reached current waypoint
        if (currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            float distanceToWaypoint = Vector2.Distance(transform.position, currentPath[currentPathIndex]);
            if (distanceToWaypoint <= nodeReachDistance)
            {
                currentPathIndex++;
                
                // Reached end of path
                if (currentPathIndex >= currentPath.Count)
                {
                    currentPath.Clear();
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showPathGizmos) return;
        
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw current path
        if (currentPath.Count > 0)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.DrawWireSphere(currentPath[i], 0.2f);
            }
            Gizmos.DrawWireSphere(currentPath[currentPath.Count - 1], 0.2f);
        }
        
        // Draw obstacle avoidance
        Gizmos.color = Color.blue;
        // Gizmos.DrawWireSphere(transform.position, col.radius + avoidanceDistance);
    }
    
    // Public methods for external interaction
    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void TakeDamage(int damage)
    {
        // Handle damage - to be implemented with health system
        Debug.Log($"Flying mob took {damage} damage!");
        animator.SetTrigger("Hurt");
    }
    
    public void Stun(float duration)
    {
        StartCoroutine(StunCoroutine(duration));
    }
    
    private System.Collections.IEnumerator StunCoroutine(float duration)
    {
        float originalSpeed = moveSpeed;
        moveSpeed = 0;
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
    }
}
