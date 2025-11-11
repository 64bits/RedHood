Shader "Hidden/HighlightBlit"
{
    Properties
    {
        _MaskTex ("Mask", 2D) = "black" {}  // Default to black, not white!
        _HighlightColor ("Color", Color) = (1,1,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MaskTex;
            float4 _HighlightColor;
            
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(uint vid : SV_VertexID) {
                v2f o;
                o.pos = float4(vid == 1 ? 3 : -1, vid == 2 ? 3 : -1, 0, 1);
                o.uv = float2(vid == 1 ? 2 : 0, vid == 2 ? 2 : 0);
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1.0 - o.uv.y;  // Flip UV if needed
                #endif
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                float mask = tex2D(_MaskTex, i.uv).r;
                // Only draw yellow where mask is white (occluded areas)
                if (mask < 0.01) discard;  // Discard pixels where there's no occlusion
                return _HighlightColor;
            }
            ENDCG
        }
    }
}