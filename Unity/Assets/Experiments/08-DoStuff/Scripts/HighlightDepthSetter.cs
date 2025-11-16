using UnityEngine;

public class HighlightDepthSetter : MonoBehaviour
{
    // Shader property ID for performance
    private static readonly int HighlightMidpointDepthID = Shader.PropertyToID("_HighlightMidpointDepth");

    void LateUpdate()
    {
        // Set the global shader property to this object's world position
        Shader.SetGlobalVector(HighlightMidpointDepthID, transform.position);
    }
}
