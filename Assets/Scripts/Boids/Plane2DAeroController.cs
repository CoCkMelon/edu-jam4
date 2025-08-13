using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Plane2DAeroController : MonoBehaviour
{
    public enum ForwardAxis2D { Right, Up }

    [Header("References")]
    public Rigidbody2D rb;

    [Header("Orientation")]
    public ForwardAxis2D forwardAxis = ForwardAxis2D.Right; // Sprite points along this axis
    [Tooltip("Local offset (meters) of the aerodynamic center/center of pressure from the CoM.")]
    public Vector2 centerOfPressureLocal = new Vector2(0.2f, 0f);
    [Tooltip("Local offset of the thrust point (engine) from the CoM.")]
    public Vector2 thrustOffsetLocal = Vector2.zero;

    [Header("Environment")]
    [Tooltip("Air density (kg/m^3). Sea level ~1.225")]
    public float airDensity = 1.225f;
    [Tooltip("Constant wind velocity in world space (m/s).")]
    public Vector2 windVelocity = Vector2.zero;

    [Header("Aerodynamics")]
    [Tooltip("Reference wing area (m^2). Scales all aero forces.")]
    public float wingArea = 1.5f;
    [Tooltip("Reference length (m) used for control torque scaling.")]
    public float referenceLength = 1.0f;

    [Tooltip("Lift coefficient vs AoA (degrees). Cl should be roughly linear near 0° and stall at high AoA.")]
    public AnimationCurve liftCurve;
    [Tooltip("Drag coefficient vs AoA (degrees). Cd should rise with |AoA| and stall.")]
    public AnimationCurve dragCurve;

    [Range(0f, 1f)]
    [Tooltip("Flap deployment [0..1]. Increases lift and drag.")]
    public float flapInput = 0f;
    [Tooltip("Additional Cl at flaps=1.")]
    public float flapLiftGain = 0.4f;
    [Tooltip("Additional Cd at flaps=1.")]
    public float flapDragGain = 0.06f;

    [Tooltip("Extra Cd when airbrake is active.")]
    public float airbrakeCd = 0.3f;

    [Header("Propulsion & Controls")]
    [Tooltip("Max thrust (N).")]
    public float maxThrust = 60f;
    [Tooltip("Throttle change speed (1/sec). Higher = faster response.")]
    public float throttleChangeRate = 1.5f;
    [Range(0f, 1f)]
    [Tooltip("Current throttle [0..1].")]
    public float throttle = 0f;

    [Tooltip("Control torque coefficient. Scales with dynamic pressure (q) and reference length.")]
    public float controlAuthority = 0.006f;
    [Tooltip("Limits how much q contributes to control (helps avoid crazy torque at very high speeds).")]
    public float controlQClamp = 150f;
    [Tooltip("Minimum speed for full control authority. Below this, controls are weaker.")]
    public float minControlSpeed = 3f;

    [Header("Input (optional)")]
    public bool useUnityInput = true;
    public string pitchAxis = "Vertical";
    public KeyCode throttleUpKey = KeyCode.W;
    public KeyCode throttleDownKey = KeyCode.S;
    public KeyCode airbrakeKey = KeyCode.LeftShift;

    [Header("Assist")]
    [Tooltip("Small auto-level that nudges AoA toward 0 when there's little input.")]
    public bool autoLevel = false;
    [Range(0f, 1f)]
    public float autoLevelStrength = 0.2f;
    [Tooltip("How much input disables auto-level.")]
    public float autoLevelDeadzone = 0.15f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public float gizmoScale = 1.0f;

    // Public for external control if not using Unity input
    [HideInInspector] public float pitchInput = 0f;
    [HideInInspector] public bool airbrake = false;

    // Internals
    float _pitchSmoothed = 0f;

    Vector2 Forward2D => (forwardAxis == ForwardAxis2D.Right) ? (Vector2)transform.right : (Vector2)transform.up;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        BuildDefaultAeroCurves();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (liftCurve == null || liftCurve.length == 0 || dragCurve == null || dragCurve.length == 0)
            BuildDefaultAeroCurves();

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (useUnityInput)
        {
            // Pitch: Up/W = pitch up (counterclockwise when pointing right)
            pitchInput = Input.GetAxisRaw(pitchAxis);

            // Throttle up/down
            float tDelta = 0f;
            if (Input.GetKey(throttleUpKey)) tDelta += 1f;
            if (Input.GetKey(throttleDownKey)) tDelta -= 1f;
            throttle = Mathf.Clamp01(throttle + tDelta * throttleChangeRate * Time.deltaTime);

            // Airbrake
            airbrake = Input.GetKey(airbrakeKey);
        }

        // Simple input smoothing on pitch
        _pitchSmoothed = -pitchInput;//Mathf.Lerp(_pitchSmoothed, Mathf.Clamp(pitchInput, -1f, 1f), 10f * Time.deltaTime);
    }

    void FixedUpdate()
    {
        Vector2 fwd = Forward2D.normalized;

        // Air-relative velocity
        Vector2 vAir = rb.linearVelocity - windVelocity;
        float speed = vAir.magnitude;
        Vector2 vHat = speed > 0.0001f ? vAir / Mathf.Max(speed, 1e-6f) : fwd;

        // Angle of Attack (signed angle from forward to velocity)
        float dot = Mathf.Clamp(Vector2.Dot(fwd, vHat), -1f, 1f);
        float crossZ = fwd.x * vHat.y - fwd.y * vHat.x;
        float aoaRad = Mathf.Atan2(crossZ, dot);
        float aoaDeg = aoaRad * Mathf.Rad2Deg;

        // Dynamic pressure
        float q = 0.5f * airDensity * speed * speed;

        // Aero coefficients
        float Cl = liftCurve.Evaluate(aoaDeg) + flapInput * flapLiftGain;
        float Cd = dragCurve.Evaluate(aoaDeg) + flapInput * flapDragGain + (airbrake ? airbrakeCd : 0f);
        Cd = Mathf.Max(0f, Cd);

        // Lift: perpendicular to airflow, scaled by Cl
        Vector2 liftDir = new Vector2(-vHat.y, vHat.x); // 90° CCW from airflow
        Vector2 lift = liftDir * (q * wingArea * Cl);

        // Drag: opposite airflow
        Vector2 drag = -vHat * (q * wingArea * Cd);

        // Apply aerodynamic forces at center of pressure
        Vector2 copWorld = rb.worldCenterOfMass + (Vector2)transform.TransformVector(centerOfPressureLocal);
        Vector2 aeroForce = lift + drag;
        rb.AddForceAtPosition(aeroForce, copWorld, ForceMode2D.Force);

        // Thrust along forward, at engine offset
        Vector2 thrust = fwd * (throttle * maxThrust);
        Vector2 thrustPoint = rb.worldCenterOfMass + (Vector2)transform.TransformVector(thrustOffsetLocal);
        rb.AddForceAtPosition(thrust, thrustPoint, ForceMode2D.Force);

        // Control torque (elevator-like): scales with q and ref length, with clamp
        float controlQ = Mathf.Min(q, controlQClamp);
        float controlSpeedFactor = Mathf.Clamp01(speed / Mathf.Max(minControlSpeed, 0.001f));
        float controlTorque = _pitchSmoothed * controlAuthority * controlQ * referenceLength * controlSpeedFactor;

        // Auto-level assist nudges AoA toward 0 if little input
        if (autoLevel && Mathf.Abs(_pitchSmoothed) < autoLevelDeadzone && speed > 0.5f)
        {
            // Desired AoA is ~0; reduce current AoA using a proportional torque
            float aoaError = aoaRad; // radians
            float assistTorque = -aoaError * autoLevelStrength * (controlQ * referenceLength);
            rb.AddTorque(assistTorque, ForceMode2D.Force);
        }

        rb.AddTorque(controlTorque, ForceMode2D.Force);
        if (Time.frameCount % 30 == 0) {
            Debug.Log($"spd:{rb.linearVelocity.magnitude:F2} q:{q:F3} pitch:{_pitchSmoothed:F2} ctrlAuth:{controlAuthority:F6} ctrlQClamp:{controlQClamp} inertia:{rb.inertia:F3} angVel:{rb.angularVelocity:F2} angDrag:{rb.angularDamping:F3}");
        }
    }

    [ContextMenu("Build default lift/drag curves")]
    public void BuildDefaultAeroCurves()
    {
        // A simple, symmetric airfoil approximation:
        // Cl ~ linear near 0°, stalls ~ +/- 15-20°
        liftCurve = new AnimationCurve(
            new Keyframe(-30f, -0.2f),
            new Keyframe(-20f, -1.2f),
            new Keyframe(-15f, -1.6f),
            new Keyframe(0f,   0.0f),
            new Keyframe(15f,  1.6f),
            new Keyframe(20f,  1.2f),
            new Keyframe(30f,  0.2f)
        );

        // Cd rises with |AoA| and increases a lot in stall
        dragCurve = new AnimationCurve(
            new Keyframe(-30f, 0.6f),
            new Keyframe(-20f, 0.22f),
            new Keyframe(-15f, 0.12f),
            new Keyframe(0f,   0.05f),
            new Keyframe(15f,  0.12f),
            new Keyframe(20f,  0.22f),
            new Keyframe(30f,  0.6f)
        );

        // Smooth tangents
        for (int i = 0; i < liftCurve.length; i++) AnimationUtility.SetKeyBroken(liftCurve, i, false);
        for (int i = 0; i < dragCurve.length; i++) AnimationUtility.SetKeyBroken(dragCurve, i, false);
    }

#if UNITY_EDITOR
    // Needed for AnimationUtility
    // Wrap in editor for builds
    static class AnimationUtility
    {
        public static void SetKeyBroken(AnimationCurve curve, int index, bool broken)
        {
            var key = curve[index];
            UnityEditor.AnimationUtility.SetKeyBroken(curve, index, broken);
            curve.MoveKey(index, key);
        }
    }
#endif

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (!rb) rb = GetComponent<Rigidbody2D>();

        // Preview vectors in play mode (or approximate in edit mode)
        Vector2 pos = rb ? rb.worldCenterOfMass : (Vector2)transform.position;
        Vector2 fwd = Forward2D.normalized;

        Vector2 vAir = (rb ? rb.linearVelocity : Vector2.zero) - windVelocity;
        float speed = vAir.magnitude;
        Vector2 vHat = speed > 0.0001f ? vAir / Mathf.Max(speed, 1e-6f) : fwd;

        float dot = Mathf.Clamp(Vector2.Dot(fwd, vHat), -1f, 1f);
        float crossZ = fwd.x * vHat.y - fwd.y * vHat.x;
        float aoaDeg = Mathf.Atan2(crossZ, dot) * Mathf.Rad2Deg;

        float q = 0.5f * airDensity * speed * speed;
        float Cl = (liftCurve != null && liftCurve.length > 0) ? liftCurve.Evaluate(aoaDeg) + flapInput * flapLiftGain : 0f;
        float Cd = (dragCurve != null && dragCurve.length > 0) ? dragCurve.Evaluate(aoaDeg) + flapInput * flapDragGain + (airbrake ? airbrakeCd : 0f) : 0f;
        Vector2 lift = new Vector2(-vHat.y, vHat.x) * (q * wingArea * Cl);
        Vector2 drag = -vHat * (q * wingArea * Cd);
        Vector2 thrust = fwd * (throttle * maxThrust);

        Gizmos.color = Color.cyan; // airflow
        Gizmos.DrawLine(pos, pos + vHat * gizmoScale * Mathf.Clamp(speed * 0.3f, 0.5f, 4f));

        Vector2 copWorld = pos + (Vector2)transform.TransformVector(centerOfPressureLocal);
        Gizmos.color = Color.green; // lift
        Gizmos.DrawLine(copWorld, copWorld + lift * (0.01f * gizmoScale));
        Gizmos.color = Color.red;   // drag
        Gizmos.DrawLine(copWorld, copWorld + drag * (0.01f * gizmoScale));

        Vector2 thrWorld = pos + (Vector2)transform.TransformVector(thrustOffsetLocal);
        Gizmos.color = Color.yellow; // thrust
        Gizmos.DrawLine(thrWorld, thrWorld + thrust * (0.01f * gizmoScale));

        Gizmos.color = Color.white; // forward
        Gizmos.DrawLine(pos, pos + fwd * (1.0f * gizmoScale));
    }
}
