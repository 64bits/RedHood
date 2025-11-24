using UnityEngine;
using System.Collections;

public class FogOfWar : MonoBehaviour
{
    [SerializeField] private Transform player;
    
    private Texture2D fogTexture;
    private Color[] fogPixels;
    private Vector3 lastPlayerPosition;
    
    private const int TEXTURE_SIZE = 2048;
    private const float WORLD_MIN_X = -117f;
    private const float WORLD_MAX_X = 113f;
    private const float WORLD_MIN_Z = -113f;
    private const float WORLD_MAX_Z = 117f;
    private const float WORLD_SIZE_X = WORLD_MAX_X - WORLD_MIN_X; // 230
    private const float WORLD_SIZE_Z = WORLD_MAX_Z - WORLD_MIN_Z; // 230
    private const int BRUSH_RADIUS = 40;
    private const float UPDATE_INTERVAL = 0.25f;
    
    private void Start()
    {
        InitializeFogTexture();
        lastPlayerPosition = player.position;
        StartCoroutine(UpdateFogCoroutine());
    }
    
    private void InitializeFogTexture()
    {
        fogTexture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Fill with pure black
        fogPixels = new Color[TEXTURE_SIZE * TEXTURE_SIZE];
        for (int i = 0; i < fogPixels.Length; i++)
        {
            fogPixels[i] = Color.black;
        }
        
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
        
        // Set as shader global
        Shader.SetGlobalTexture("_FogOfWarTexture", fogTexture);
    }
    
    private IEnumerator UpdateFogCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(UPDATE_INTERVAL);
            
            // Check if player moved
            if (Vector3.Distance(player.position, lastPlayerPosition) > 0.01f)
            {
                RevealFogAtPlayerPosition();
                lastPlayerPosition = player.position;
            }
        }
    }
    
    private void RevealFogAtPlayerPosition()
    {
        // Convert world position to texture coordinates
        Vector2 normalizedPos = new Vector2(
            (player.position.x - WORLD_MIN_X) / WORLD_SIZE_X,
            (player.position.z - WORLD_MIN_Z) / WORLD_SIZE_Z
        );
        
        int texX = Mathf.RoundToInt(normalizedPos.x * TEXTURE_SIZE);
        int texY = Mathf.RoundToInt(normalizedPos.y * TEXTURE_SIZE);
        
        // Erase a circle around the player position
        for (int y = -BRUSH_RADIUS; y <= BRUSH_RADIUS; y++)
        {
            for (int x = -BRUSH_RADIUS; x <= BRUSH_RADIUS; x++)
            {
                // Check if within circle
                if (x * x + y * y <= BRUSH_RADIUS * BRUSH_RADIUS)
                {
                    int pixelX = texX + x;
                    int pixelY = texY + y;
                    
                    // Check bounds
                    if (pixelX >= 0 && pixelX < TEXTURE_SIZE && pixelY >= 0 && pixelY < TEXTURE_SIZE)
                    {
                        int index = pixelY * TEXTURE_SIZE + pixelX;
                        fogPixels[index] = Color.clear; // Transparent = revealed
                    }
                }
            }
        }
        
        fogTexture.SetPixels(fogPixels);
        fogTexture.Apply();
    }
}