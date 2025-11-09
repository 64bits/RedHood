using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform cameraTarget; // Assign in inspector
    
    void Update()
    {
        // Only follow X and Z position, maintain Y position
        Vector3 newPos = new Vector3(transform.position.x, cameraTarget.position.y, transform.position.z);
        cameraTarget.position = newPos;
    }
}