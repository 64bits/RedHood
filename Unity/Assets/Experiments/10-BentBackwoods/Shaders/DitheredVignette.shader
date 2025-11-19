Shader "Custom/DitheredVignette"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off
        
        Pass
        {
            Name "DitheredVignette"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            
            // -------------------------------------------------------
            // FIX: Core.hlsl MUST be included before Blit.hlsl
            // This defines TEXTURE2D_X and other required macros.
            // -------------------------------------------------------
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _VignetteIntensity;
            float _VignetteSize;
            
            static const float4x4 BayerMatrix = float4x4(
                0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
            );
            
            float GetBayerValue(float2 screenPos)
            {
                int x = int(screenPos.x) % 4;
                int y = int(screenPos.y) % 4;
                return BayerMatrix[y][x];
            }
            
            float DitherVignette(float2 uv, float2 screenPos)
            {
                float2 center = uv - 0.5;
                float dist = length(center);
                
                float vignetteValue = smoothstep(_VignetteSize, _VignetteSize + 0.3, dist);
                vignetteValue *= _VignetteIntensity;
                
                float bayerValue = GetBayerValue(screenPos);
                float ditheredVignette = step(bayerValue, vignetteValue);
                
                return 1.0 - ditheredVignette;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Note: We use _BlitTexture (provided by Blitter API)
                // and sampler_LinearClamp (standard URP sampler)
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                float vignetteMask = DitherVignette(input.texcoord, input.positionCS.xy);
                color.rgb *= vignetteMask;
                
                return color;
            }
            ENDHLSL
        }
    }
}