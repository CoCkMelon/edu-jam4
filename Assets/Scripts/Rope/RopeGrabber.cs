using UnityEngine;

public class RopeGrabber : MonoBehaviour
{
    public float grabRadius = 1f;
    public LayerMask ropeLayer;
    
    private HingeJoint2D grabJoint;
    private Rigidbody2D playerRb;
    
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grabJoint == null)
        {
            TryGrabRope();
        }
        else if (Input.GetKeyUp(KeyCode.Space) && grabJoint != null)
        {
            ReleaseRope();
        }
    }
    
    void TryGrabRope()
    {
        Collider2D ropeCollider = Physics2D.OverlapCircle(transform.position, grabRadius, ropeLayer);
        
        if (ropeCollider != null)
        {
            grabJoint = gameObject.AddComponent<HingeJoint2D>();
            grabJoint.connectedBody = ropeCollider.GetComponent<Rigidbody2D>();
            grabJoint.autoConfigureConnectedAnchor = false;
            grabJoint.connectedAnchor = Vector2.zero;
        }
    }
    
    void ReleaseRope()
    {
        if (grabJoint != null)
        {
            Destroy(grabJoint);
            grabJoint = null;
        }
    }
}