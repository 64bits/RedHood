Shader "Hidden/MixRevealMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        
        Pass
        {
            Name "MixRevealMask"
            
            // Write to G channel only
            ColorMask G
            
            // No depth test - we want silhouettes
            ZTest Always
            ZWrite Off
            
            // Additive blend to preserve R channel from previous pass
            Blend One Zero
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Cutoff;
            CBUFFER_END
            
            // Global bend parameter
            float _CelBendAmount;

            float3 CalculateBentPosition(float3 positionOS)
            {
                float3 worldPos = TransformObjectToWorld(positionOS);
                float3 camPos = _WorldSpaceCameraPos;
                float3 relativePos = worldPos - camPos;
                float relX = relativePos.x;
                float relY = relativePos.y; // Note: This wire connects to the Top Add node
                float relZ = relativePos.z;
                float curveX = (relX * relX) * -_CelBendAmount; 
                float curveZ = (relZ * relZ) * -_CelBendAmount;
                float3 offset = float3(0, curveX + curveZ, 0);
                float3 bentWorldPos = worldPos + offset;
                float3 bentObjectPos = TransformWorldToObject(bentWorldPos);

                return bentObjectPos;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Call the function to get the new Object Space position
                float3 modifiedPositionOS = CalculateBentPosition(input.positionOS);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(modifiedPositionOS);
                output.positionCS = vertexInput.positionCS;

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample alpha for cutout support
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                
                // Write 0 to R channel, 1 to G
                return half4(0, 1, 0, 1);
            }
            ENDHLSL
        }
    }
}