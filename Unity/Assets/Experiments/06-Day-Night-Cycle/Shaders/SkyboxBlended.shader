Shader "Custom/Skybox/Blended"
{
    Properties
    {
        _Tex ("Cubemap (Day)", Cube) = "grey" {}
        _Tex2 ("Cubemap (Night)", Cube) = "grey" {}
        _BlendFactor ("Blend Factor", Range(0,1)) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
    }
    
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            samplerCUBE _Tex;
            samplerCUBE _Tex2;
            float _BlendFactor;
            float4 _Tint;
            float _Exposure;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample both cubemaps
                fixed4 dayColor = texCUBE(_Tex, i.texcoord);
                fixed4 nightColor = texCUBE(_Tex2, i.texcoord);
                
                // Blend between them
                fixed4 blendedColor = lerp(dayColor, nightColor, _BlendFactor);
                
                // Apply tint and exposure
                blendedColor.rgb *= _Tint.rgb * _Exposure;
                
                return fixed4(blendedColor.rgb, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback Off
    CustomEditor "SkyboxBlendedShaderGUI"
}