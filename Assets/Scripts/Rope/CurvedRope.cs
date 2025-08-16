using UnityEngine;

public class CurvedRope : MonoBehaviour
{
    public Transform AnchorPoint;
    public ControlPoint ControlPoint;
    public Transform PlayerTransform;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 50; // Sample 50 points for smoothness
    }

    void Update()
    {
        Vector2 p0 = AnchorPoint.position;
        Vector2 p1 = ControlPoint.transform.position;
        Vector2 p2 = PlayerTransform.position;

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float t = i / (float)(lineRenderer.positionCount - 1);
            lineRenderer.SetPosition(i, BezierPoint(p0, p1, p2, t));
        }
    }

    // Quadratic Bézier curve calculation
    Vector2 BezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }
}