TEXTURE2D(_CombinedMaskTex);
SAMPLER(sampler_CombinedMaskTex);

void SampleRevealMask_float(float4 ScreenPos, out float Mask)
{
    float2 screenUV = ScreenPos.xy;
    screenUV = saturate(screenUV); // Clamp to 0-1 range
    Mask = SAMPLE_TEXTURE2D(_CombinedMaskTex, sampler_CombinedMaskTex, screenUV).g;
}