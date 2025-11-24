using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CompassController : MonoBehaviour
{
    public enum NorthAxis { PositiveZ, NegativeZ, PositiveX, NegativeX }
    
    [Header("References")]
    [Tooltip("The camera or object to read rotation from. Uses Main Camera if empty.")]
    public Transform target;
    
    [Tooltip("The UI Image with the compass shader material.")]
    public Image compassImage;
    
    [Tooltip("Icon atlas texture (grid of icons)")]
    public Texture2D iconAtlas;
    
    [Header("Settings")]
    [Tooltip("Which world axis represents North")]
    public NorthAxis northDirection = NorthAxis.PositiveZ;
    
    [Tooltip("Size of icons on compass (0.0 - 1.0 in UV space)")]
    [Range(0.05f, 0.5f)]
    public float iconSize = 0.5f;
    
    [Tooltip("Number of columns in icon atlas")]
    public int atlasColumns = 4;
    
    [Tooltip("Number of rows in icon atlas")]
    public int atlasRows = 4;
    
    [Header("Distance Fade")]
    [Tooltip("Distance at which icons become invisible")]
    public float maxDistance = 80f;
    
    [Tooltip("Distance at which icons start to fade")]
    public float fadeStartDistance = 50f;
    
    [Tooltip("Distance at which icons are fully opaque")]
    public float fullOpacityDistance = 20f;
    
    private Material compassMat;
    private int headingID;
    private int iconCountID;
    private int iconSizeID;
    private int atlasColumnsID;
    private int atlasRowsID;
    private int iconAtlasID;
    
    private List<CompassMarker> discoveredMarkers = new List<CompassMarker>();
    private Vector4[] iconDataArray = new Vector4[32]; // Max 32 icons
    
    void Start()
    {
        if (target == null)
            target = Camera.main?.transform;
            
        if (compassImage != null)
        {
            // Create instance to avoid modifying shared material
            compassMat = Instantiate(compassImage.material);
            compassImage.material = compassMat;
            
            // Cache shader property IDs
            headingID = Shader.PropertyToID("_Heading");
            iconCountID = Shader.PropertyToID("_IconCount");
            iconSizeID = Shader.PropertyToID("_IconSize");
            atlasColumnsID = Shader.PropertyToID("_AtlasColumns");
            atlasRowsID = Shader.PropertyToID("_AtlasRows");
            iconAtlasID = Shader.PropertyToID("_IconAtlas");
            
            // Set icon atlas properties
            if (iconAtlas != null)
            {
                compassMat.SetTexture(iconAtlasID, iconAtlas);
            }
            compassMat.SetFloat(iconSizeID, iconSize);
            compassMat.SetInt(atlasColumnsID, atlasColumns);
            compassMat.SetInt(atlasRowsID, atlasRows);
        }
        
        // Find all discoverable objects in scene
        DiscoverableObject[] discoverables = FindObjectsOfType<DiscoverableObject>();
        foreach (var obj in discoverables)
        {
            obj.compassController = this;
        }
    }
    
    void Update()
    {
        if (target == null || compassMat == null) return;
        
        // Get forward direction projected onto horizontal plane
        Vector3 forward = target.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Calculate heading based on chosen north axis
        float heading = CalculateHeading(forward);
        compassMat.SetFloat(headingID, heading);
        
        // Update icon data
        UpdateIconData();
    }
    
    private float CalculateHeading(Vector3 forward)
    {
        float heading = 0f;
        switch (northDirection)
        {
            case NorthAxis.PositiveZ:
                heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                break;
            case NorthAxis.NegativeZ:
                heading = Mathf.Atan2(-forward.x, -forward.z) * Mathf.Rad2Deg;
                break;
            case NorthAxis.PositiveX:
                heading = Mathf.Atan2(forward.z, -forward.x) * Mathf.Rad2Deg;
                break;
            case NorthAxis.NegativeX:
                heading = Mathf.Atan2(-forward.z, forward.x) * Mathf.Rad2Deg;
                break;
        }
        
        // Normalize to 0-360
        if (heading < 0) heading += 360f;
        return heading;
    }
    
    private void UpdateIconData()
    {
        int activeCount = Mathf.Min(discoveredMarkers.Count, 32);
        
        for (int i = 0; i < activeCount; i++)
        {
            CompassMarker marker = discoveredMarkers[i];
            
            if (marker == null || !marker.isDiscovered)
            {
                iconDataArray[i] = new Vector4(0, 0, 0, 0);
                continue;
            }
            
            // Calculate angle to marker
            Vector3 directionToMarker = marker.worldPosition - target.position;
            directionToMarker.y = 0;
            float distance = directionToMarker.magnitude;
            directionToMarker.Normalize();
            
            float angle = CalculateHeading(directionToMarker);
            
            // Pack data: x=angle, y=distance, z=iconIndex, w=enabled
            iconDataArray[i] = new Vector4(
                angle,
                distance,
                marker.iconIndex,
                marker.isDiscovered ? 1f : 0f
            );
        }
        
        // Clear remaining slots
        for (int i = activeCount; i < 32; i++)
        {
            iconDataArray[i] = Vector4.zero;
        }
        
        compassMat.SetInt(iconCountID, activeCount);
        compassMat.SetVectorArray("_IconData", iconDataArray);
    }
    
    public void RegisterMarker(CompassMarker marker)
    {
        if (!discoveredMarkers.Contains(marker))
        {
            discoveredMarkers.Add(marker);
        }
    }
    
    public void UnregisterMarker(CompassMarker marker)
    {
        discoveredMarkers.Remove(marker);
    }
    
    void OnDestroy()
    {
        if (compassMat != null)
            Destroy(compassMat);
    }
}

[System.Serializable]
public class CompassMarker
{
    public Vector3 worldPosition;
    public int iconIndex;
    public bool isDiscovered;
    
    public CompassMarker(Vector3 position, int icon)
    {
        worldPosition = position;
        iconIndex = icon;
        isDiscovered = false;
    }
}