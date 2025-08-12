using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [System.Serializable]
    public struct InputState
    {
        public float moveX;
        public bool jumpPressed;
        public bool jumpHeld;
        public bool dashPressed;
    }

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float acceleration = 80f;
    public float deceleration = 100f;
    [Range(0f, 1f)] public float airControlPercent = 0.65f;
    [Range(0f, 1f)] public float turnAroundBoost = 1.5f; // Arcade feel when changing direction

    [Header("Jump Settings")]
    public float jumpForce = 15f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;
    public float fallGravityMultiplier = 3f;
    public float lowJumpGravityMultiplier = 2.5f;
    public float maxFallSpeed = 20f;
    public int maxAirJumps = 1; // Double jump capability

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    [Header("Dash Settings")]
    public float dashForce = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Smooth dash feel

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip dashSound;
    public AudioClip footstepSound;
    [Range(0f, 1f)] public float minLandVolume = 0.3f;
    [Range(0f, 1f)] public float maxLandVolume = 1f;
    public float landVelocityThreshold = 5f; // Min velocity for land sound
    public float maxLandVelocity = 25f; // Velocity for max volume

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public Collider2D heroCollider;

    [Header("Runtime Input State")]
    public InputState input;

    // Animator hashes
    readonly int ALIVE_HASH = Animator.StringToHash("alive");
    readonly int MOVING_HASH = Animator.StringToHash("moving");
    readonly int DASH_READY_HASH = Animator.StringToHash("dash_ready");
    readonly int GROUNDED_HASH = Animator.StringToHash("grounded");
    readonly int VELOCITY_Y_HASH = Animator.StringToHash("velocity_y");

    // State tracking
    private bool isGrounded;
    private bool wasGrounded;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private float dashTimeLeft;
    private float lastDashTime;
    private bool isDashing;
    private bool facingRight = true;
    private int airJumpsRemaining;
    private float lastFootstepTime;
    private float footstepInterval = 0.3f;
    private float previousYVelocity;
    private Vector2 dashDirection;
    private float dashStartTime;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        animator.SetBool(ALIVE_HASH, true);
    }

    private void OnEnable()
    {
        // MOVE
        var moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("negative", "<Keyboard>/a").With("negative", "<Keyboard>/leftArrow")
            .With("positive", "<Keyboard>/d").With("positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");
        moveAction.performed += ctx => input.moveX = ctx.ReadValue<float>();
        moveAction.canceled += ctx => input.moveX = 0f;
        moveAction.Enable();

        // JUMP
        var jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += ctx => { input.jumpPressed = true; input.jumpHeld = true; };
        jumpAction.canceled += ctx => input.jumpHeld = false;
        jumpAction.Enable();

        // DASH
        var dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/rightShoulder");
        dashAction.performed += ctx => input.dashPressed = true;
        dashAction.Enable();
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        GroundCheck();
        UpdateTimers();
        HandleJump();
        HandleDash();
        UpdateAnimator();
        FlipSprite();
        HandleSounds();

        // Track previous Y velocity for landing detection
        previousYVelocity = rb.linearVelocity.y;

        // Reset single-frame inputs
        input.jumpPressed = false;
        input.dashPressed = false;
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplyGravityModifiers();
        ClampFallSpeed();
    }

    private void HandleMovement()
    {
        if (isDashing) return;

        float targetSpeed = input.moveX * moveSpeed;

        // Arcade-style instant direction change boost
        bool changingDirection = (input.moveX > 0 && rb.linearVelocity.x < 0) ||
                                (input.moveX < 0 && rb.linearVelocity.x > 0);

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        if (changingDirection && isGrounded)
            accelRate *= turnAroundBoost;

        if (!isGrounded)
            accelRate *= airControlPercent;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float force = speedDiff * accelRate;

        rb.AddForce(Vector2.right * force, ForceMode2D.Force);
    }

    private void ApplyGravityModifiers()
    {
        if (isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        if (rb.linearVelocity.y < 0)
            rb.gravityScale = fallGravityMultiplier;
        else if (rb.linearVelocity.y > 0 && !input.jumpHeld)
            rb.gravityScale = lowJumpGravityMultiplier;
        else
            rb.gravityScale = 1f;
    }

    private void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

    private void UpdateTimers()
    {
        if (isGrounded)
        {
            lastGroundedTime = coyoteTime;
            airJumpsRemaining = maxAirJumps;
        }
        else
            lastGroundedTime -= Time.deltaTime;

        if (input.jumpPressed)
            lastJumpPressedTime = jumpBufferTime;
        else
            lastJumpPressedTime -= Time.deltaTime;

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                rb.gravityScale = 1f;
            }
            else
            {
                // Apply dash curve for smooth acceleration/deceleration
                float dashProgress = 1f - (dashTimeLeft / dashDuration);
                float curveValue = dashCurve.Evaluate(dashProgress);
                rb.linearVelocity = dashDirection * dashForce * curveValue;
            }
        }
    }

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);
    }

    private void HandleJump()
    {
        bool canGroundJump = lastJumpPressedTime > 0 && lastGroundedTime > 0;
        bool canAirJump = lastJumpPressedTime > 0 && !isGrounded && airJumpsRemaining > 0;

        if (canGroundJump || canAirJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (canAirJump)
                airJumpsRemaining--;

            lastGroundedTime = 0;
            lastJumpPressedTime = 0;

            PlaySound(jumpSound, 0.7f);
        }
    }

    private void HandleDash()
    {
        if (input.dashPressed && Time.time > lastDashTime + dashCooldown && !isDashing)
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;
            dashStartTime = Time.time;

            // Dash in input direction or facing direction
            float dashX = input.moveX != 0 ? Mathf.Sign(input.moveX) : (facingRight ? 1 : -1);
            dashDirection = new Vector2(dashX, 0).normalized;

            PlaySound(dashSound, 0.8f);
        }
    }

    private void HandleSounds()
    {
        // Landing sound with force-based volume
        if (!wasGrounded && isGrounded && previousYVelocity < -landVelocityThreshold)
        {
            float landForce = Mathf.Abs(previousYVelocity);
            float volumePercent = Mathf.InverseLerp(landVelocityThreshold, maxLandVelocity, landForce);
            float volume = Mathf.Lerp(minLandVolume, maxLandVolume, volumePercent);

            PlaySound(landSound, volume);
        }

        // Footstep sounds while moving on ground
        if (isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f && Time.time > lastFootstepTime + footstepInterval)
        {
            float speedPercent = Mathf.Abs(rb.linearVelocity.x) / moveSpeed;
            float volume = Mathf.Lerp(0.3f, 0.6f, speedPercent);

            PlaySound(footstepSound, volume);
            lastFootstepTime = Time.time;

            // Adjust footstep interval based on speed
            footstepInterval = Mathf.Lerp(0.4f, 0.2f, speedPercent);
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void UpdateAnimator()
    {
        animator.SetBool(MOVING_HASH, Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        animator.SetBool(DASH_READY_HASH, Time.time > lastDashTime + dashCooldown);
        animator.SetBool(GROUNDED_HASH, isGrounded);
        animator.SetFloat(VELOCITY_Y_HASH, rb.linearVelocity.y);
    }

    private void FlipSprite()
    {
        if (input.moveX > 0 && !facingRight)
        {
            facingRight = true;
            animator.transform.localScale = Vector3.one;
        }
        else if (input.moveX < 0 && facingRight)
        {
            facingRight = false;
            animator.transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
