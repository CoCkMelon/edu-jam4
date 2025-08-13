using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlaneArcade2D : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 8f;          // constant X speed
    public float maxVerticalSpeed = 8f;      // max up/down speed
    public float verticalAccel = 30f;        // how fast we reach target Vy
    public float maxTilt = 25f;              // degrees to bank with vertical speed
    public float verticalDeadzone = 0.05f;   // ignore tiny inputs

    [Header("Screen Clamp")]
    public bool clampToCamera = true;
    public float verticalMargin = 0.3f;      // world units from top/bottom

    [Header("Shooting")]
    public bool autoFire = true;
    public float fireRate = 8f;              // shots per second
    public float bulletSpeed = 18f;
    public Transform firePoint;              // set to a child at the plane's nose
    public GameObject bulletPrefab;          // prefab with Rigidbody2D (+ optional Collider2D)

    Rigidbody2D rb;
    Camera cam;
    float nextShotTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;      // we control tilt manually
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (Camera.main != null) cam = Camera.main;
    }

    void Update()
    {
        HandleShooting();
    }

    void FixedUpdate()
    {
        float inputY = GetVerticalInput();

        // target vertical speed from input
        float targetVy = Mathf.Abs(inputY) < verticalDeadzone ? 0f : inputY * maxVerticalSpeed;

        // accelerate towards target vertical speed
        float newVy = Mathf.MoveTowards(rb.linearVelocity.y, targetVy, verticalAccel * Time.fixedDeltaTime);

        // apply velocity (constant forward + vertical)
        rb.linearVelocity = new Vector2(forwardSpeed, Mathf.Clamp(newVy, -maxVerticalSpeed, maxVerticalSpeed));

        // optional: clamp within camera vertical bounds
        if (clampToCamera && cam)
        {
            float halfHeight = cam.orthographicSize;
            float yMin = cam.transform.position.y - halfHeight + verticalMargin;
            float yMax = cam.transform.position.y + halfHeight - verticalMargin;
            rb.position = new Vector2(rb.position.x, Mathf.Clamp(rb.position.y, yMin, yMax));
        }

        // bank/tilt based on vertical speed
        float tiltZ = -Mathf.Clamp(rb.linearVelocity.y / Mathf.Max(0.001f, maxVerticalSpeed), -1f, 1f) * maxTilt;
        float smoothTilt = Mathf.LerpAngle(rb.rotation, tiltZ, 10f * Time.fixedDeltaTime);
        rb.MoveRotation(smoothTilt);
    }

    float GetVerticalInput()
    {
        // Keyboard/Controller (old Input Manager)
        float inputY = Input.GetAxisRaw("Vertical");

        // Touch: move toward finger's Y
        if (Input.touchCount > 0 && cam != null)
        {
            Vector3 touchWorld = cam.ScreenToWorldPoint(Input.GetTouch(0).position);
            float delta = Mathf.Clamp(touchWorld.y - rb.position.y, -1f, 1f);
            inputY = Mathf.Abs(delta) > 0.02f ? Mathf.Sign(delta) * Mathf.Min(1f, Mathf.Abs(delta)) : 0f;
        }
        return Mathf.Clamp(inputY, -1f, 1f);
    }

    void HandleShooting()
    {
        if (!bulletPrefab || !firePoint) return;

        bool wantsFire = autoFire || Input.GetButton("Fire1");

        if (wantsFire && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            // Ensure bullet has a Rigidbody2D
            var brb = bullet.GetComponent<Rigidbody2D>();
            if (!brb) brb = bullet.AddComponent<Rigidbody2D>();
            brb.gravityScale = 0f;
            brb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Fire to the plane's right (+X if sprite faces right)
            Vector2 dir = (Vector2)transform.right; // in 2D, right is forward for a side-scroller
            brb.linearVelocity = dir.normalized * bulletSpeed;

            Destroy(bullet, 3f); // cleanup
        }
    }
}
