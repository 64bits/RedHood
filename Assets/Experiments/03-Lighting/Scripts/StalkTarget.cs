using UnityEngine;
using System.Collections;

public class StalkTarget : MonoBehaviour
{
    public enum BehaviorMode
    {
        Orbit,
        Linear
    }

    [Header("Behavior Settings")]
    [SerializeField] private BehaviorMode behaviorMode = BehaviorMode.Orbit;

    [Header("Target Settings")]
    [SerializeField] private Transform target;  // The object to orbit around

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSpeed = 30f;  // Degrees per second
    [SerializeField] private bool clockwise = true;   // Orbit direction

    [Header("Linear Settings")]
    [SerializeField] private float linearSpeed = 5f;  // Units per second in XZ plane

    [Header("Look Settings")]
    [SerializeField] private bool lookAtTangent = true;  // Look along orbit path (orbit mode) or movement direction (linear mode)
    [SerializeField] private float lookSmoothing = 5f;   // Smooth rotation speed

    [Header("Alpha Clip Settings")]
    [SerializeField] private string alphaClipPropertyName = "_AlphaClipThreshold";  // Common property name, adjust if needed
    [SerializeField] private float lerpDuration = 0.5f;  // Duration to lerp alpha
    [SerializeField] private float orbitOscillationInterval = 2f;  // Time between oscillations in orbit mode

    // Orbit mode variables
    private float currentAngle = 0f;
    private float orbitRadius;
    private Vector3 orbitCenter;

    // Linear mode variables
    private Vector3 movementDirection;
    private bool hasPassedTarget = false;

    // Alpha clip variables
    private Material[] materialInstances;
    private Coroutine alphaCoroutine;
    private float currentAlpha = 1f;
    private float nextOrbitOscillationTime = 0f;

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

        // Get material instances from SkinnedMeshRenderer in children
        SkinnedMeshRenderer skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderer != null && skinnedRenderer.materials != null && skinnedRenderer.materials.Length > 0)
        {
            materialInstances = skinnedRenderer.materials;  // Creates instances automatically
            
            // Get initial alpha from first material
            if (materialInstances[0].HasProperty(alphaClipPropertyName))
            {
                currentAlpha = materialInstances[0].GetFloat(alphaClipPropertyName);
            }
        }
        else
        {
            Debug.LogWarning("StalkTarget: No SkinnedMeshRenderer or Materials found in children!");
        }

        // Initialize based on behavior mode
        if (target != null)
        {
            if (behaviorMode == BehaviorMode.Orbit)
            {
                InitializeOrbitMode();
            }
            else if (behaviorMode == BehaviorMode.Linear)
            {
                InitializeLinearMode();
            }
        }
        else
        {
            Debug.LogWarning("StalkTarget: No target assigned and no Player tag found!");
        }
    }

    void InitializeOrbitMode()
    {
        orbitCenter = target.position;

        // Calculate the vector from the target to this object
        Vector3 offset = transform.position - target.position;

        // Calculate initial orbit radius ignoring Y-axis
        Vector3 flatOffset = offset;
        flatOffset.y = 0f; // Ignore Y-axis
        orbitRadius = flatOffset.magnitude;

        // Calculate initial angle from the flat offset
        currentAngle = Mathf.Atan2(flatOffset.z, flatOffset.x) * Mathf.Rad2Deg;

        // Start oscillation timer
        nextOrbitOscillationTime = Time.time + orbitOscillationInterval;
    }

    void InitializeLinearMode()
    {
        // Calculate direction to target in XZ plane
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        movementDirection = toTarget.normalized;
        hasPassedTarget = false;

        // Start alpha fade from 1 to 0
        if (materialInstances != null && materialInstances.Length > 0)
        {
            alphaCoroutine = StartCoroutine(LerpToTargetAlpha(0f));
        }
    }

    void Update()
    {
        if (target == null) return;

        if (behaviorMode == BehaviorMode.Orbit)
        {
            UpdateOrbitMode();
        }
        else if (behaviorMode == BehaviorMode.Linear)
        {
            UpdateLinearMode();
        }
    }

    void UpdateOrbitMode()
    {
        orbitCenter = target.position;

        // Update orbit angle
        float direction = clockwise ? -1f : 1f;
        currentAngle += direction * orbitSpeed * Time.deltaTime;

        // Keep angle between 0-360 degrees
        currentAngle %= 360f;

        // Update position
        UpdateOrbitPosition();

        // Update rotation
        UpdateOrbitRotation();

        // Handle alpha oscillation
        if (materialInstances != null && materialInstances.Length > 0 && Time.time >= nextOrbitOscillationTime)
        {
            // Toggle between 0 and 1
            float targetAlpha = (currentAlpha < 0.5f) ? 1f : 0f;
            
            // Stop any existing coroutine and start new one
            if (alphaCoroutine != null)
            {
                StopCoroutine(alphaCoroutine);
            }
            alphaCoroutine = StartCoroutine(LerpToTargetAlpha(targetAlpha));
            
            // Schedule next oscillation
            nextOrbitOscillationTime = Time.time + orbitOscillationInterval;
        }
    }

    void UpdateLinearMode()
    {
        // Check if we've passed the target
        if (!hasPassedTarget)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            // Check if we're very close or if the dot product shows we've passed
            float distanceToTarget = toTarget.magnitude;
            float dotProduct = Vector3.Dot(movementDirection, toTarget.normalized);

            if (distanceToTarget < 0.1f || dotProduct < 0f)
            {
                hasPassedTarget = true;
            }
        }

        // Move in the direction (same whether we've passed target or not)
        Vector3 movement = movementDirection * linearSpeed * Time.deltaTime;
        transform.position += movement;

        // Update rotation
        UpdateLinearRotation();
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

    void UpdateOrbitRotation()
    {
        if (lookAtTangent)
        {
            // Calculate tangent direction (perpendicular to radius)
            Vector3 toCenter = (orbitCenter - transform.position).normalized;
            Vector3 tangent = Vector3.Cross(toCenter, Vector3.up);

            // Reverse tangent if orbiting clockwise
            if (clockwise)
                tangent = -tangent;

            // Create rotation looking along tangent
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

    void UpdateLinearRotation()
    {
        if (lookAtTangent)
        {
            // Look in the direction of movement
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSmoothing * Time.deltaTime);
        }
        else
        {
            // Look at the target (even after passing it)
            Vector3 lookDirection = target.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookSmoothing * Time.deltaTime);
            }
        }
    }

    IEnumerator LerpToTargetAlpha(float targetAlpha)
    {
        if (materialInstances == null || materialInstances.Length == 0) yield break;

        float startAlpha = currentAlpha;
        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lerpDuration;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            // Update all material instances
            foreach (Material mat in materialInstances)
            {
                if (mat != null && mat.HasProperty(alphaClipPropertyName))
                {
                    mat.SetFloat(alphaClipPropertyName, currentAlpha);
                }
            }
            
            yield return null;
        }

        // Ensure we reach exact target value
        currentAlpha = targetAlpha;
        foreach (Material mat in materialInstances)
        {
            if (mat != null && mat.HasProperty(alphaClipPropertyName))
            {
                mat.SetFloat(alphaClipPropertyName, currentAlpha);
            }
        }
    }

    // Gizmos for visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            if (behaviorMode == BehaviorMode.Orbit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, orbitRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
            else if (behaviorMode == BehaviorMode.Linear)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);
                
                if (Application.isPlaying)
                {
                    // Draw movement direction
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(transform.position, movementDirection * 3f);
                }
            }
        }
    }

    public void JumpOffsetOn() {
      transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
    }

    public void JumpOffsetOff() {
      transform.position = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
    }

    void OnDestroy()
    {
        // Clean up material instances
        if (materialInstances != null)
        {
            foreach (Material mat in materialInstances)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}