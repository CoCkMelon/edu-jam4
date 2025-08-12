using UnityEngine;

public class UltraSnappyCameraFollow : MonoBehaviour
{
    [Tooltip("The object the camera will follow")]
    public Transform followTarget;

    [Tooltip("If true, camera snaps instantly; if false, uses speed-based follow")]
    public bool instantSnap = true;

    [Tooltip("Follow speed (used only if instantSnap = false)")]
    public float followSpeed = 10f;

    [Tooltip("Maximum distance before camera teleports to target")]
    public float maxDistance = 5f;

    void LateUpdate()
    {
        if (followTarget == null) return;

        Vector3 targetPos = new Vector3(
            followTarget.position.x,
            followTarget.position.y,
            transform.position.z);

        // If target is too far away, snap to position (optional)
        if (Vector3.Distance(transform.position, targetPos) > maxDistance)
        {
            transform.position = targetPos;
            return;
        }

        // Option 1: Instant snapping (no smoothing)
        if (instantSnap)
        {
            transform.position = targetPos;
        }
        // Option 2: Speed-based follow (smoother)
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                followSpeed * Time.deltaTime);
        }
    }
}
