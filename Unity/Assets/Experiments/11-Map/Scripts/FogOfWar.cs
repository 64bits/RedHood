using UnityEngine;
using System.Collections;

public class FogOfWar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("World Mapping")]
    // Default values based on your empirical data
    [SerializeField] private Vector2 worldCenter = new Vector2(-1.1f, 13.7f); 
    [SerializeField] private Vector2 worldSize = new Vector2(522.4f, 522.4f);

    [Header("Fog Settings")]
    [SerializeField] private int brushRadius = 40;
    [SerializeField] private float updateInterval = 0.25f;
    
    private Texture2D fogTexture;
    private Color[] fogPixels;
    private Vector3 lastPlayerPosition;
    private const int TEXTURE_SIZE = 2048;
    
    private void Start()
    {
        InitializeFogTexture();
        lastPlayerPosition = player.position;
        StartCoroutine(UpdateFogCoroutine());
        
        // Initial reveal
        RevealFogAtPlayerPosition();
    }
    
    private void InitializeFogTexture()
    {
        fogTexture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Fill with pure black (Covered)
        fogPixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
        for (int i = 0; i < fogPixels.Length; i++)
        {
            fogPixels[i] = Color.black;
        }
        
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
        
        Shader.SetGlobalTexture("_FogOfWarTexture", fogTexture);
        
        // Pass the bounds to the shader as well, in case your shader calculates UVs manually
        Shader.SetGlobalVector("_FogWorldMin", new Vector4(
            worldCenter.x - worldSize.x / 2f, 
            worldCenter.y - worldSize.y / 2f, 
            worldSize.x, 
            worldSize.y));
    }
    
    private IEnumerator UpdateFogCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (Vector3.Distance(player.position, lastPlayerPosition) > 0.1f) // Increased threshold slightly for perf
            {
                RevealFogAtPlayerPosition();
                lastPlayerPosition = player.position;
            }
        }
    }
    
    private void RevealFogAtPlayerPosition()
    {
        // 1. Calculate World Min (Bottom-Left)
        float worldMinX = worldCenter.x - (worldSize.x / 2f);
        float worldMinZ = worldCenter.y - (worldSize.y / 2f); // Using Y as Z for Vector2

        // 2. Normalize Player Position (0 to 1)
        // Formula: (PlayerPos - WorldMin) / WorldSize
        float u = (player.position.x - worldMinX) / worldSize.x;
        float v = (player.position.z - worldMinZ) / worldSize.y;

        // 3. Check if player is actually inside the fog area
        if (u < 0 || u > 1 || v < 0 || v > 1) return;

        // 4. Convert to Texture Coordinates
        int centerX = Mathf.RoundToInt(u * TEXTURE_SIZE);
        int centerY = Mathf.RoundToInt(v * TEXTURE_SIZE);
        
        int rSquared = brushRadius * brushRadius;
        bool pixelsChanged = false;

        // 5. Paint
        // Optimization: Only loop through the bounding box of the brush
        for (int y = -brushRadius; y <= brushRadius; y++)
        {
            for (int x = -brushRadius; x <= brushRadius; x++)
            {
                if (x * x + y * y <= rSquared)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;
                    
                    if (pixelX >= 0 && pixelX < TEXTURE_SIZE && pixelY >= 0 && pixelY < TEXTURE_SIZE)
                    {
                        int index = pixelY * TEXTURE_SIZE + pixelX;
                        
                        // Only write if not already cleared (micro-optimization)
                        if (fogPixels[index].a != 0)
                        {
                            fogPixels[index] = Color.clear;
                            pixelsChanged = true;
                        }
                    }
                }
            }
        }
        
        if (pixelsChanged)
        {
            fogTexture.SetPixels(fogPixels);
            fogTexture.Apply();
        }
    }

    // VISUAL DEBUGGING
    // This draws a Cyan wireframe box in the Scene view showing exactly where the code thinks the fog is.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center3D = new Vector3(worldCenter.x, 0, worldCenter.y);
        Vector3 size3D = new Vector3(worldSize.x, 1, worldSize.y);
        Gizmos.DrawWireCube(center3D, size3D);
    }
}