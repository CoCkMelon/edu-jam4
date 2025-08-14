using UnityEngine;

public class ControlPoint : MonoBehaviour
{
    public bool IsGrabbed { get; private set; }
    public Transform PlayerTransform;
    private Vector3 offset = new Vector3(0, 1f); // Offset to maintain curve when grabbed

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            IsGrabbed = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            IsGrabbed = false;
    }

    void Update()
    {
        if (IsGrabbed)
            transform.position = PlayerTransform.position + offset;
    }
}
