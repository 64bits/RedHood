using UnityEngine;

public class StalkTarget : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;  // The object to orbit around

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSpeed = 30f;  // Degrees per second
    [SerializeField] private bool clockwise = true;   // Orbit direction

    [Header("Look Settings")]
    [SerializeField] private bool lookAtTangent = true;  // Look along orbit path
    [SerializeField] private float lookSmoothing = 5f;   // Smooth rotation speed

    private float currentAngle = 0f;
    private float orbitRadius;
    private Vector3 orbitCenter;

    void Start()
    {
        // If no target specified, try to find player tag
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Initialize position if target exists
        if (target != null)
        {
            orbitCenter = target.position;

            // --- FIX IS HERE ---
            // Calculate the vector from the target to this object
            Vector3 offset = transform.position - target.position;

            // Calculate initial orbit radius ignoring Y-axis
            Vector3 flatOffset = offset;
            flatOffset.y = 0f; // Ignore Y-axis
            orbitRadius = flatOffset.magnitude;

            // Calculate initial angle from the flat offset
            // We use Atan2(z, x) to get the angle in the XZ plane
            currentAngle = Mathf.Atan2(flatOffset.z, flatOffset.x) * Mathf.Rad2Deg;
            // --- END OF FIX ---
        }
        else
        {
            Debug.LogWarning("StalkTarget: No target assigned and no Player tag found!");
        }
    }

    void Update()
    {
        if (target == null) return;

        orbitCenter = target.position;

        // Update orbit angle
        float direction = clockwise ? -1f : 1f;
        currentAngle += direction * orbitSpeed * Time.deltaTime;

        // Keep angle between 0-360 degrees
        currentAngle %= 360f;

        // Update position
        UpdateOrbitPosition();

        // Update rotation
        UpdateRotation();
    }

    void UpdateOrbitPosition()
    {
        // Calculate position in X-Z plane
        float angleRad = currentAngle * Mathf.Deg2Rad;
        float x = orbitCenter.x + Mathf.Cos(angleRad) * orbitRadius;
        float z = orbitCenter.z + Mathf.Sin(angleRad) * orbitRadius;

        // Maintain current Y position
        Vector3 newPosition = new Vector3(x, transform.position.y, z);
        transform.position = newPosition;
    }

    void UpdateRotation()
    {
        if (lookAtTangent)
        {
            // Calculate tangent direction (perpendicular to radius)
            // Note: Vector from self to center is the opposite of the offset
            Vector3 toCenter = (orbitCenter - transform.position).normalized;
            Vector3 tangent = Vector3.Cross(toCenter, Vector3.up);

            // Reverse tangent if orbiting clockwise
            if (clockwise)
                tangent = -tangent;

            // Create rotation looking along tangent
            // Use -tangent to look "forward" along the path
            Quaternion targetRotation = Quaternion.LookRotation(-tangent, Vector3.up); 

            // Smoothly rotate towards tangent
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSmoothing * Time.deltaTime);
        }
        else
        {
            // Look directly at the target
            transform.LookAt(target);
        }
    }

    // Gizmos for visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            // Draw the gizmo at the target's position, not the (potentially outdated) orbitCenter
            Gizmos.DrawWireSphere(target.position, orbitRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}