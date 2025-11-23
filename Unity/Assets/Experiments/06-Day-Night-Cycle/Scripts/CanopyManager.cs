using UnityEngine;

public class CanopyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Settings")]
    [SerializeField] private float maxBeaconDistance = 10f;
    [SerializeField] private float canopyChangeThreshold = 0.01f; 
    
    // State
    private float currentDistance = 0f;
    [SerializeField] [Range(0f, 1f)] private float currentCanopy = 0f; // Normalized 0 to 1 (0 = no canopy, 1 = full canopy)
    private float previousCanopy = 0f;
    private int beaconLayer;

    // Events
    public System.Action<float> OnCanopyChanged; // Passes normalized canopy value (0-1)

    private void Awake()
    {
        beaconLayer = 1 << 9; // 9th layer (Beacon)
    }

    private void Update()
    {
        CalculateBeaconDistance();
        CalculateCanopy();
    }

    private void CalculateBeaconDistance()
    {
        if (playerTransform == null)
        {
            currentDistance = maxBeaconDistance;
            return;
        }

        // Perform overlap sphere check at player position
        Collider[] beacons = Physics.OverlapSphere(playerTransform.position, maxBeaconDistance, beaconLayer);

        if (beacons.Length == 0)
        {
            currentDistance = maxBeaconDistance;
            return;
        }

        // Find the closest beacon
        float closestDistance = float.MaxValue;

        foreach (Collider beacon in beacons)
        {
            // Get closest point on beacon collider to player
            Vector3 closestPoint = beacon.ClosestPoint(playerTransform.position);
            float distanceToEdge = Vector3.Distance(playerTransform.position, closestPoint);

            if (distanceToEdge < closestDistance)
            {
                closestDistance = distanceToEdge;
            }
        }

        currentDistance = Mathf.Clamp(closestDistance, 0f, maxBeaconDistance);
    }

    private void CalculateCanopy()
    {
        // Normalize distance inverted: 
        // Distance 0 = Canopy 1.0
        // Distance Max = Canopy 0.0
        float rawCanopy = 1f - (currentDistance / maxBeaconDistance);
        currentCanopy = Mathf.Clamp01(rawCanopy);

        // Check for changes
        if (Mathf.Abs(currentCanopy - previousCanopy) >= canopyChangeThreshold)
        {
            previousCanopy = currentCanopy;
            NotifyCanopyChange();
        }
    }

    private void NotifyCanopyChange()
    {
        OnCanopyChanged?.Invoke(currentCanopy);
        UpdateVignette();
    }

    private void UpdateVignette()
    {
        // Using the calculated Canopy value to drive the vignette
        // Canopy 1 (Safe) = No Vignette
        // Canopy 0 (Far) = heavy Vignette
        // The original formula was roughly: 1f - (0.1f * (distance / 2f - 1f))
        // We can simplify this based on Canopy now, or adapt the old logic:
        
        // If we translate the old math directly:
        // distance = (1 - canopy) * maxBeaconDistance
        float dist = (1f - currentCanopy) * maxBeaconDistance;
        Shader.SetGlobalFloat("_VignetteSize", 1f - (0.1f * (dist / 2f - 1f)));
    }

    public float GetCurrentCanopy()
    {
        return currentCanopy;
    }

    public float GetDistanceToNearestBeacon()
    {
        return currentDistance;
    }
}