// Custom Function Node for Unity ShaderGraph
// Name: PlayerMarkerDepth
// 
// Inputs:
//   - PlayerMarker (Texture2D) - The marker texture to draw
//   - TreeDepthTexture (Texture2D) - The depth texture from your Python script
//   - PlayerPosition (Vector2) - Player world position (same coordinate space as CSV)
//   - MarkerSize (Vector2) - Size of the marker in UV space (e.g., 0.05, 0.05)
//   - WorldBoundsMin (Vector2) - Min X,Y from your coordinate space
//   - WorldBoundsMax (Vector2) - Max X,Y from your coordinate space
//   - UV (Vector2) - Current UV coordinates
//
// Outputs:
//   - Out (Vector4) - The final RGBA color

SamplerState sampler_linear_clamp;

void PlayerMarkerDepth_float(
    UnityTexture2D PlayerMarker,
    UnityTexture2D TreeDepthTexture,
    float2 PlayerPosition,
    float2 MarkerSize,
    float2 WorldBoundsMin,
    float2 WorldBoundsMax,
    float2 UV,
    out float4 Out)
{
    // Account for padding in the Python script (100px on 2048 texture)
    float padding = 100.0 / 2048.0;
    float usableRange = 1.0 - 2.0 * padding;
    
    // Normalize player position to 0-1, then map to padded UV space
    float2 worldRange = WorldBoundsMax - WorldBoundsMin;
    float2 playerNormalized = (PlayerPosition - WorldBoundsMin) / worldRange;
    float2 playerUV = padding + playerNormalized * usableRange;
    
    // Calculate player depth from Y position (normalized 0-1)
    float normalizedPlayerDepth = 1 - (playerNormalized.y) - padding * 2;
    
    // Calculate UV offset from player position
    float2 offsetFromPlayer = UV - playerUV;
    
    // Check if current UV is within marker bounds
    float2 halfSize = MarkerSize * 0.5;
    bool withinMarker = abs(offsetFromPlayer.x) < halfSize.x && 
                        abs(offsetFromPlayer.y) < halfSize.y;
    
    // Default output is transparent
    Out = float4(0, 0, 0, 0);
    
    if (withinMarker)
    {
        // Sample marker texture (remap offset to 0-1 for marker UV)
        float2 markerUV = (offsetFromPlayer / MarkerSize) + 0.5;
        float4 markerColor = SAMPLE_TEXTURE2D(PlayerMarker.tex, sampler_linear_clamp, markerUV);
        
        // Sample depth texture at current UV position (RGBA - check alpha for tree presence)
        float4 depthSample = SAMPLE_TEXTURE2D(TreeDepthTexture.tex, sampler_linear_clamp, UV);
        float treeDepth = depthSample.r;
        float treeAlpha = depthSample.a;
        
        // Only draw marker where:
        // 1. No tree exists (alpha == 0), OR
        // 2. Tree depth is less than player depth (tree is behind player)
        bool noTree = treeAlpha < 0.01;
        bool treeBehindPlayer = treeDepth < normalizedPlayerDepth;
        
        // For debugging
        // Out = float4(normalizedPlayerDepth, normalizedPlayerDepth, normalizedPlayerDepth, 1.0);

        if (noTree || treeBehindPlayer)
        {
            Out = markerColor;
            // Out = float4(normalizedPlayerDepth, normalizedPlayerDepth, normalizedPlayerDepth, 1.0);
        }
    }
}