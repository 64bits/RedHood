TEXTURE2D(_HighlightMaskTex);
SAMPLER(sampler_HighlightMaskTex);

void SampleHighlightMask_float(float4 ScreenPos, out float Mask)
{
    float2 screenUV = ScreenPos.xy / ScreenPos.w;
    Mask = SAMPLE_TEXTURE2D(_HighlightMaskTex, sampler_HighlightMaskTex, screenUV).r;
}