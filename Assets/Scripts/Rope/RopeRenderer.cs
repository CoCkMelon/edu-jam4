using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Transform[] ropeSegments;
    
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Initialize your rope segments here
    }
    
    void Update()
    {
        if (ropeSegments != null)
        {
            lineRenderer.positionCount = ropeSegments.Length;
            for (int i = 0; i < ropeSegments.Length; i++)
            {
                lineRenderer.SetPosition(i, ropeSegments[i].position);
            }
        }
    }
}