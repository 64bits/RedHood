using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HarvestPopulation : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float radiusOffset = 0f;
    [Tooltip("Amount subtracted from radius at the poles (max height), 0 at the equator.")]
    [SerializeField] private float insetFactor = 0f;

    [Header("Height Settings")]
    [SerializeField] private float cardinalHeight = 0f;
    [SerializeField] private float intermediateHeight = 0f;
    [SerializeField] private float ultraIntermediateHeight = 0f;
    
    private SphereCollider sphereCollider;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        if (prefabToSpawn == null) return;
        SpawnAllPoints();
    }

    private void SpawnAllPoints()
    {
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float baseRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float totalRadius = baseRadius + radiusOffset;

        SpawnGroup(center, totalRadius, GetCardinalDirections(), cardinalHeight);
        SpawnGroup(center, totalRadius, GetIntermediateDirections(), intermediateHeight);
        SpawnGroup(center, totalRadius, GetUltraIntermediateDirections(), ultraIntermediateHeight);
    }

    private void SpawnGroup(Vector3 center, float totalRadius, Vector3[] directions, float heightOffset)
    {
        foreach (Vector3 dir in directions)
        {
            Vector3 spawnPos = CalculatePosition(center, totalRadius, dir, heightOffset);
            
            // Rotation: Forward faces directly away from the sphere's center
            Vector3 lookDirection = (spawnPos - center).normalized;
            Quaternion rotation = (lookDirection != Vector3.zero) ? Quaternion.LookRotation(lookDirection) : Quaternion.identity;

            Instantiate(prefabToSpawn, spawnPos, rotation, transform);
        }
    }

    private Vector3 CalculatePosition(Vector3 center, float totalRadius, Vector3 direction, float heightOffset)
    {
        // 1. Calculate the interpolation factor (0 at equator, 1 at poles)
        // We use the absolute height relative to the total radius
        float t = Mathf.Abs(heightOffset) / totalRadius;
        t = Mathf.Clamp01(t); 

        // 2. Calculate the effective radius for this specific height
        float effectiveRadius = totalRadius - (t * insetFactor);

        // 3. Ensure the height doesn't exceed the effective radius (Pythagorean safety)
        float clampedHeight = Mathf.Clamp(heightOffset, -effectiveRadius, effectiveRadius);
        
        // 4. Solve for horizontal distance: r^2 = h^2 + xz^2 -> xz = sqrt(r^2 - h^2)
        float horizontalRadius = Mathf.Sqrt(Mathf.Max(0, (effectiveRadius * effectiveRadius) - (clampedHeight * clampedHeight)));

        return center + (direction * horizontalRadius) + (Vector3.up * clampedHeight);
    }

    #region Direction Definitions
    private Vector3[] GetCardinalDirections() => new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    private Vector3[] GetIntermediateDirections() => new Vector3[] {
        (Vector3.forward + Vector3.right).normalized, (Vector3.back + Vector3.right).normalized,
        (Vector3.back + Vector3.left).normalized, (Vector3.forward + Vector3.left).normalized
    };

    private Vector3[] GetUltraIntermediateDirections()
    {
        Vector3 n = Vector3.forward; Vector3 e = Vector3.right;
        Vector3 s = Vector3.back; Vector3 w = Vector3.left;
        Vector3 ne = (n + e).normalized; Vector3 se = (s + e).normalized;
        Vector3 sw = (s + w).normalized; Vector3 nw = (n + w).normalized;
        return new Vector3[] {
            (n + ne).normalized, (e + ne).normalized, (e + se).normalized, (s + se).normalized,
            (s + sw).normalized, (w + sw).normalized, (w + nw).normalized, (n + nw).normalized
        };
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null) return;

        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float baseRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        float totalRadius = baseRadius + radiusOffset;

        Gizmos.color = Color.red;
        DrawDirectionalGizmos(center, totalRadius, GetCardinalDirections(), cardinalHeight);

        Gizmos.color = Color.yellow;
        DrawDirectionalGizmos(center, totalRadius, GetIntermediateDirections(), intermediateHeight);

        Gizmos.color = Color.cyan;
        DrawDirectionalGizmos(center, totalRadius, GetUltraIntermediateDirections(), ultraIntermediateHeight);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, totalRadius);
    }

    private void DrawDirectionalGizmos(Vector3 center, float totalRadius, Vector3[] directions, float heightOffset)
    {
        foreach (Vector3 dir in directions)
        {
            Gizmos.DrawSphere(CalculatePosition(center, totalRadius, dir, heightOffset), 0.1f);
        }
    }
}