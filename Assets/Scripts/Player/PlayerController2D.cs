using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [System.Serializable]
    public struct InputState
    {
        public float moveX;        // -1 to 1
        public bool jumpPressed;   // true on the frame jump is triggered
        public bool jumpHeld;      // true while holding jump
        public bool dashPressed;   // true on the frame dash is triggered
    }

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 50f;
    [Range(0f, 1f)] public float airControlPercent = 0.8f;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 1.15f;
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpGravityMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    public LayerMask groundLayer;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("References")]
    public Rigidbody2D rb;            // main body
    public Animator animator;         // separate object
    public Collider2D heroCollider;   // optional

    // INPUT STATE - public for reading/writing from other scripts
    [Header("Runtime Input State")]
    public InputState input;

    // Animator hashes (Hero style)
    readonly int ALIVE_HASH = Animator.StringToHash("alive");
    readonly int MOVING_HASH = Animator.StringToHash("moving");
    readonly int DASH_READY_HASH = Animator.StringToHash("dash_ready");

    // Timers/state
    private bool isGrounded;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private float dashTimeLeft;
    private float lastDashTime;
    private bool isDashing;
    private bool facingRight = true;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        animator.SetBool(ALIVE_HASH, true);
    }

    private void OnEnable()
    {
        // MOVE: horizontal axis
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
        GroundCheck();
        UpdateTimers();
        HandleJump();
        HandleDash();
        UpdateAnimator();
        FlipSprite();

        // Reset single-frame presses so they are only consumed once
        input.jumpPressed = false;
        input.dashPressed = false;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    /* ---------------- MOVEMENT ---------------- */
    private void HandleMovement()
    {
        if (isDashing) return;

        float targetSpeed = input.moveX * moveSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f)
            ? acceleration
            : deceleration;

        if (!isGrounded)
            accelRate *= airControlPercent;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float force = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.AddForce(Vector2.right * force, ForceMode2D.Force);
    }

    /* ---------------- GRAVITY + TIMERS ---------------- */
    private void UpdateTimers()
    {
        if (isGrounded) {
            lastGroundedTime = coyoteTime;//print("grounded");
        } else
            lastGroundedTime -= Time.deltaTime;

        if (input.jumpPressed)
            lastJumpPressedTime = jumpBufferTime;
        else
            lastJumpPressedTime -= Time.deltaTime;

        if (!isDashing)
        {
            if (rb.linearVelocity.y < 0)
                rb.gravityScale = fallGravityMultiplier;
            else if (rb.linearVelocity.y > 0 && !input.jumpHeld)
                rb.gravityScale = lowJumpGravityMultiplier;
            else
                rb.gravityScale = 1f;
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                rb.gravityScale = 1f;
            }
        }
    }

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);
        // isGrounded = true;
    }

    /* ---------------- JUMP ---------------- */
    private void HandleJump()
    {

        if (lastJumpPressedTime > 0 && lastGroundedTime > 0)
        {
            print("jump");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            // rb.AddForce(new Vector2(0,jumpForce));
            lastGroundedTime = 0;
            lastJumpPressedTime = 0;
        }
    }

    /* ---------------- DASH ---------------- */
    private void HandleDash()
    {
        if (input.dashPressed && Time.time > lastDashTime + dashCooldown && !isDashing)
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;

            Vector2 dashDir = new Vector2(facingRight ? 1 : -1, 0);
            rb.linearVelocity = dashDir.normalized * dashForce;
            rb.gravityScale = 0f;
        }
    }

    /* ---------------- ANIM / SPRITE ---------------- */
    private void UpdateAnimator()
    {
        animator.SetBool(MOVING_HASH, Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        animator.SetBool(DASH_READY_HASH, Time.time > lastDashTime + dashCooldown);
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
}
