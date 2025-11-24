using UnityEngine;

public class DiscoveryLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    
    [Header("Light Settings")]
    [SerializeField] private float maxDistance = 40f;
    [SerializeField] private float maxIntensity = 25f;
    [SerializeField] private float maxRange = 10f;
    
    private Light pointLight;

    void Start()
    {
        // Get the Light component on this GameObject
        pointLight = GetComponent<Light>();
        
        if (pointLight == null)
        {
            Debug.LogError("No Light component found on " + gameObject.name);
            enabled = false;
            return;
        }
        
        if (player == null)
        {
            Debug.LogWarning("Player transform not assigned on " + gameObject.name);
        }
    }

    void Update()
    {
        if (player == null || pointLight == null)
            return;
        
        // Calculate distance between this object and the player
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Normalize distance (0 to 1 range, clamped at maxDistance)
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
        
        // Apply r^2 formula
        float distanceSquared = normalizedDistance * normalizedDistance;
        
        // Set light intensity and range based on squared distance
        pointLight.intensity = distanceSquared * maxIntensity;
        pointLight.range = distanceSquared * maxRange;
    }
}