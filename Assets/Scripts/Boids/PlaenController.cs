using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlaenController : MonoBehaviour
{
    [Header("Aerodynamics")]
    [SerializeField] private float maxThrust = 200f;
    [SerializeField] private float liftPower = 50f;
    [SerializeField] private float dragCoefficient = 0.5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float maxPitchAngle = 45f;
    [SerializeField] private float maxLiftCoefficient = 1f;

    [Header("Controls")]
    [SerializeField] private float pitchSpeed = 180f; // Degrees per second

    private Rigidbody2D rb;
    private float currentPitch = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Get input axes
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Update pitch angle
        currentPitch += verticalInput * pitchSpeed * Time.fixedDeltaTime;
        currentPitch = Mathf.Clamp(currentPitch, -maxPitchAngle, maxPitchAngle);

        // Convert to radians
        float pitchRad = currentPitch * Mathf.Deg2Rad;

        // Calculate thrust force
        float thrust = horizontalInput * maxThrust;
        Vector2 thrustDirection = new Vector2(Mathf.Cos(pitchRad), Mathf.Sin(pitchRad));
        Vector2 thrustForce = thrustDirection * thrust;

        // Calculate lift force
        float speed = rb.linearVelocity.magnitude;
        float liftCoefficient = maxLiftCoefficient * (currentPitch / maxPitchAngle + 0.5f); // Base lift at 0 pitch
        float liftMagnitude = 0.5f * speed * speed * liftPower * liftCoefficient;

        Vector2 liftDirection = new Vector2(-Mathf.Sin(pitchRad), Mathf.Cos(pitchRad));
        Vector2 liftForce = liftDirection * liftMagnitude;

        // Calculate drag force
        Vector2 dragForce = -rb.linearVelocity * dragCoefficient * Mathf.Max(speed, 0.1f);

        // Calculate weight force
        float mass = rb.mass;
        Vector2 weightForce = new Vector2(0f, -mass * gravity);

        // Apply all forces
        rb.AddForce(thrustForce + liftForce + dragForce + weightForce);

        // Rotate plane to match pitch angle
        transform.rotation = Quaternion.Euler(0f, 0f, -currentPitch);
    }

    private void OnDrawGizmos()
    {
        // Visual debug for thrust and lift directions
        if (Application.isPlaying)
        {
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            Vector2 thrustDir = new Vector2(Mathf.Cos(pitchRad), Mathf.Sin(pitchRad));
            Vector2 liftDir = new Vector2(-Mathf.Sin(pitchRad), Mathf.Cos(pitchRad));

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, thrustDir * 2f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, liftDir * 2f);
        }
    }
}
