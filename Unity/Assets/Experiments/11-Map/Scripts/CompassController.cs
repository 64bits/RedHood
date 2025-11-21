using UnityEngine;
using UnityEngine.UI;

public class CompassController : MonoBehaviour
{
    public enum NorthAxis { PositiveZ, NegativeZ, PositiveX, NegativeX }
    
    [Header("References")]
    [Tooltip("The camera or object to read rotation from. Uses Main Camera if empty.")]
    public Transform target;
    
    [Tooltip("The UI Image with the compass shader material.")]
    public Image compassImage;
    
    [Header("Settings")]
    [Tooltip("Which world axis represents North")]
    public NorthAxis northDirection = NorthAxis.PositiveZ;
    
    private Material compassMat;
    private int headingID;

    void Start()
    {
        if (target == null)
            target = Camera.main?.transform;
        
        if (compassImage != null)
        {
            // Create instance to avoid modifying shared material
            compassMat = Instantiate(compassImage.material);
            compassImage.material = compassMat;
            headingID = Shader.PropertyToID("_Heading");
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
        
        compassMat.SetFloat(headingID, heading);
    }

    void OnDestroy()
    {
        if (compassMat != null)
            Destroy(compassMat);
    }
}