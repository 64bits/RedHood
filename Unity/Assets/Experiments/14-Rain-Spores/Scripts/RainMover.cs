using UnityEngine;

public class RainMover : MonoBehaviour
{
    public Transform rainFX; // Assign in inspector
    
    void Update()
    {
        // Only follow X and Z position, maintain Y position
        Vector3 newPos = new Vector3(transform.position.x, rainFX.position.y, transform.position.z);
        rainFX.position = newPos;
    }
}