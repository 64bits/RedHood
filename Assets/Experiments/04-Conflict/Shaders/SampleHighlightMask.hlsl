TEXTURE2D(_HighlightMaskTex);
SAMPLER(sampler_HighlightMaskTex);
TEXTURE2D(_HighlightDepthTex);
SAMPLER(sampler_HighlightDepthTex);

void SampleHighlightMask_float(float4 ScreenPos, float FragDepth, out float Mask)
{
    float2 screenUV = ScreenPos.xy / ScreenPos.w;
    
    // Sample the mask
    float maskValue = SAMPLE_TEXTURE2D(_HighlightMaskTex, sampler_HighlightMaskTex, screenUV).r;
    
    // Sample the Layer 6 depth
    float highlightDepth = SAMPLE_TEXTURE2D(_HighlightDepthTex, sampler_HighlightDepthTex, screenUV).r;
    
    // Only highlight if:
    // 1. Mask exists (Layer 6 object present)
    // 2. Current fragment is CLOSER than Layer 6 (occluding it)
    Mask = (maskValue > 0.5 && FragDepth < highlightDepth) ? 1.0 : 0.0;
}