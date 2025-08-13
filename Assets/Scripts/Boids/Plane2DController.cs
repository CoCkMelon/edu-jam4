using UnityEngine;

public class Plane2DController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float moveSpeed = 5f;

    void Update()
    {
        // Rotation with A/D or arrow keys
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -rotation);

        // Forward/backward movement with W/S
        float movement = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        transform.Translate(0, movement, 0);

        // Optional: Look at mouse position
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = transform.position.z;

            Vector3 direction = mousePos - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
