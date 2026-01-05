using UnityEngine;

public class HarvestPopulation : MonoBehaviour
{
    [SerializeField] private float cardinalHeight = 0f;
    [SerializeField] private float intermediateHeight = 0f;
    [SerializeField] private float ultraIntermediateHeight = 0f;
    
    private SphereCollider sphereCollider;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            Debug.LogError("SphereCollider component not found on " + gameObject.name);
        }
    }

    private void OnDrawGizmos()
    {
        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null) return;
        }

        // Get the center of the sphere collider in world space
        Vector3 center = transform.TransformPoint(sphereCollider.center);
        float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);

        // Define the 16 cardinal and intermediate directions on the X-Z plane
        // Primary directions (N, E, S, W)
        Vector3 n = Vector3.forward;
        Vector3 e = Vector3.right;
        Vector3 s = Vector3.back;
        Vector3 w = Vector3.left;

        // First-level intermediates (NE, SE, SW, NW)
        Vector3 ne = (n + e).normalized;
        Vector3 se = (s + e).normalized;
        Vector3 sw = (s + w).normalized;
        Vector3 nw = (n + w).normalized;

        // Second-level intermediates (NNE, ENE, ESE, SSE, SSW, WSW, WNW, NNW)
        Vector3 nne = (n + ne).normalized;
        Vector3 ene = (e + ne).normalized;
        Vector3 ese = (e + se).normalized;
        Vector3 sse = (s + se).normalized;
        Vector3 ssw = (s + sw).normalized;
        Vector3 wsw = (w + sw).normalized;
        Vector3 wnw = (w + nw).normalized;
        Vector3 nnw = (n + nw).normalized;

        // Draw cardinal directions (N, E, S, W)
        Gizmos.color = Color.red;
        Vector3[] cardinals = new Vector3[] { n, e, s, w };
        DrawDirectionalGizmos(center, radius, cardinals, cardinalHeight);

        // Draw intermediate directions (NE, SE, SW, NW)
        Gizmos.color = Color.yellow;
        Vector3[] intermediates = new Vector3[] { ne, se, sw, nw };
        DrawDirectionalGizmos(center, radius, intermediates, intermediateHeight);

        // Draw ultra-intermediate directions (NNE, ENE, ESE, SSE, SSW, WSW, WNW, NNW)
        Gizmos.color = Color.cyan;
        Vector3[] ultraIntermediates = new Vector3[] { nne, ene, ese, sse, ssw, wsw, wnw, nnw };
        DrawDirectionalGizmos(center, radius, ultraIntermediates, ultraIntermediateHeight);

        // Optional: Draw the sphere collider outline for reference
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, radius);
        
        // Optional: Draw circles at each height to show the contours
        DrawContourCircle(center, radius, cardinalHeight, new Color(1f, 0f, 0f, 0.3f));
        DrawContourCircle(center, radius, intermediateHeight, new Color(1f, 1f, 0f, 0.3f));
        DrawContourCircle(center, radius, ultraIntermediateHeight, new Color(0f, 1f, 1f, 0.3f));
    }

    private void DrawDirectionalGizmos(Vector3 center, float radius, Vector3[] directions, float heightOffset)
    {
        float absHeightOffset = Mathf.Abs(heightOffset);
        
        // If height offset is greater than radius, clamp it
        if (absHeightOffset > radius)
        {
            heightOffset = Mathf.Sign(heightOffset) * radius;
            absHeightOffset = radius;
        }

        // Using Pythagorean theorem: horizontalRadius^2 + height^2 = radius^2
        float horizontalRadius = Mathf.Sqrt(radius * radius - absHeightOffset * absHeightOffset);

        // Draw gizmos at each direction
        foreach (Vector3 dir in directions)
        {
            Vector3 gizmoPosition = center + dir * horizontalRadius + Vector3.up * heightOffset;
            Gizmos.DrawSphere(gizmoPosition, 0.1f);
        }
    }

    private void DrawContourCircle(Vector3 center, float radius, float heightOffset, Color color)
    {
        float absHeightOffset = Mathf.Abs(heightOffset);
        
        // If height offset is greater than radius, don't draw
        if (absHeightOffset > radius) return;

        float horizontalRadius = Mathf.Sqrt(radius * radius - absHeightOffset * absHeightOffset);
        
        Gizmos.color = color;
        DrawCircle(center + Vector3.up * heightOffset, horizontalRadius, 32);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}