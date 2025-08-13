using UnityEngine;

public class PositionOffset : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        transform.position = offset+target.transform.position;
    }
}
