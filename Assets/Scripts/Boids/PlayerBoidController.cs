using UnityEngine;

public class PlayerBoidController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    
    private Vector2 movement;
    private Vector2 velocity;
    private Vector2 smoothVelocity;
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isDashing;
    
    private void Start()
    {
        // Set player tag
        gameObject.tag = "Player";
        
        // Set player color
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.cyan;
        }
    }
    
    private void Update()
    {
        HandleInput();
        HandleDash();
    }
    
    private void FixedUpdate()
    {
        MovePlayer();
    }
    
    private void HandleInput()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
        
        // Dash input
        if (Input.GetKeyDown(KeyCode.Space) && dashCooldownTimer <= 0 && !isDashing)
        {
            StartDash();
        }
    }
    
    private void MovePlayer()
    {
        float currentSpeed = isDashing ? dashSpeed : moveSpeed;
        Vector2 targetVelocity = movement * currentSpeed;
        
        velocity = Vector2.SmoothDamp(velocity, targetVelocity, ref smoothVelocity, smoothTime);
        
        Vector2 newPosition = (Vector2)transform.position + velocity * Time.fixedDeltaTime;
        GetComponent<Rigidbody2D>().MovePosition(newPosition);
        
        // Rotate to face movement direction
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * 15f);
        }
    }
    
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }
    
    private void HandleDash()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
        
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }
}