using UnityEngine;

public class DiscoverableObject : MonoBehaviour
{
    [Header("Compass Icon")]
    [Tooltip("Index of icon in the atlas (0-based, left-to-right, top-to-bottom)")]
    public int iconIndex = 0;
    
    [Header("Discovery Settings")]
    [Tooltip("Automatically discover when entering this range")]
    public float discoveryRange = 30f;
    
    [Tooltip("Require line of sight to discover")]
    public bool requireLineOfSight = false;
    
    [Tooltip("Layer mask for line of sight checks")]
    public LayerMask lineOfSightMask = -1;
    
    [Tooltip("Start already discovered")]
    public bool startDiscovered = false;
    
    [HideInInspector]
    public CompassController compassController;
    
    private CompassMarker marker;
    private Transform playerTransform;
    private bool wasDiscovered = false;
    
    void Start()
    {
        // Create marker for this object
        marker = new CompassMarker(transform.position, iconIndex);
        marker.isDiscovered = startDiscovered;
        wasDiscovered = startDiscovered;
        
        // Find compass controller if not set
        if (compassController == null)
        {
            compassController = FindObjectOfType<CompassController>();
        }
        
        // Register with compass
        if (compassController != null)
        {
            compassController.RegisterMarker(marker);
            playerTransform = compassController.target;
        }
    }
    
    void Update()
    {
        // Update marker position if object moves
        marker.worldPosition = transform.position;
        
        // Auto-discovery logic
        if (!marker.isDiscovered && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance <= discoveryRange)
            {
                if (requireLineOfSight)
                {
                    Vector3 directionToPlayer = playerTransform.position - transform.position;
                    if (!Physics.Raycast(transform.position, directionToPlayer.normalized, 
                        distance, lineOfSightMask))
                    {
                        Discover();
                    }
                }
                else
                {
                    Discover();
                }
            }
        }
        
        // Optional: Handle un-discovery if object was destroyed/hidden
        if (wasDiscovered != marker.isDiscovered)
        {
            wasDiscovered = marker.isDiscovered;
            OnDiscoveryChanged(marker.isDiscovered);
        }
    }
    
    /// <summary>
    /// Manually discover this object
    /// </summary>
    public void Discover()
    {
        if (!marker.isDiscovered)
        {
            marker.isDiscovered = true;
            OnDiscovered();
        }
    }
    
    /// <summary>
    /// Manually hide this object from compass
    /// </summary>
    public void Hide()
    {
        marker.isDiscovered = false;
    }
    
    /// <summary>
    /// Called when object is first discovered
    /// </summary>
    protected virtual void OnDiscovered()
    {
        Debug.Log($"{gameObject.name} discovered!");
        // Add visual/audio feedback here
    }
    
    /// <summary>
    /// Called whenever discovery state changes
    /// </summary>
    protected virtual void OnDiscoveryChanged(bool discovered)
    {
        // Override in derived classes for custom behavior
    }
    
    void OnDestroy()
    {
        if (compassController != null)
        {
            compassController.UnregisterMarker(marker);
        }
    }
    
    // Visualize discovery range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = marker != null && marker.isDiscovered ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, discoveryRange);
    }
}