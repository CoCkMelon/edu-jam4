using UnityEngine;

public class RopeGenerator : MonoBehaviour
{
    public GameObject segmentPrefab;
    public int segmentCount = 20;
    public float segmentLength = 0.2f;
    
    private GameObject[] segments;
    
    void Start()
    {
        segments = new GameObject[segmentCount];
        Vector3 currentPos = transform.position;
        
        for (int i = 0; i < segmentCount; i++)
        {
            segments[i] = Instantiate(segmentPrefab, currentPos, Quaternion.identity);
            segments[i].transform.parent = transform;
            
            if (i > 0)
            {
                HingeJoint2D joint = segments[i].AddComponent<HingeJoint2D>();
                joint.connectedBody = segments[i - 1].GetComponent<Rigidbody2D>();
            }
            else
            {
                // First segment might be attached to a fixed point
                segments[i].GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            }
            
            currentPos += Vector3.down * segmentLength;
        }
    }
}