#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

#ifndef COMBINED_MASK_DEFINED
#define COMBINED_MASK_DEFINED
TEXTURE2D(_CombinedMaskTex);
SAMPLER(sampler_CombinedMaskTex);
#endif

void SampleRevealMask_float(float4 ScreenPos, out float Mask)
{
    #if ( SHADERPASS == SHADERPASS_SHADOWCASTER )
        Mask = 0.0;
        return;
    #endif

    float2 screenUV = ScreenPos.xy;
    screenUV = saturate(screenUV); // Clamp to 0-1 range
    Mask = SAMPLE_TEXTURE2D(_CombinedMaskTex, sampler_CombinedMaskTex, screenUV).g;
}