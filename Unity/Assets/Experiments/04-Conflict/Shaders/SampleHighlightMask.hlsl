TEXTURE2D(_CombinedMaskTex);
SAMPLER(sampler_CombinedMaskTex);

void SampleHighlightMask_float(float4 ScreenPos, out float Mask)
{
    // Use the xy components directly as UVs (0 to 1 range)
    float2 screenUV = ScreenPos.xy; 
    // Sometimes a slight adjustment is needed, e.g., if it's already divided by w but needs a sign flip
    // float2 screenUV = ScreenPos.xy * 0.5f + 0.5f; 
    // For now, try this simplest change first:
    
    // Sample from R channel of combined mask texture
    Mask = SAMPLE_TEXTURE2D(_CombinedMaskTex, sampler_CombinedMaskTex, screenUV).r;
}