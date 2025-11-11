Shader "Hidden/OcclusionHighlight"
{
    Properties
    {
        _OccluderMaskTex ("Mask", 2D) = "white" {}
        _HighlightColor ("Color", Color) = (1,1,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _OccluderMaskTex;
            float4 _HighlightColor;
            
            struct v2f {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };
            
            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float mask = tex2D(_OccluderMaskTex, uv).r;
                return mask > 0.5 ? _HighlightColor : float4(0,0,0,0); // Only draw if occluded
            }
            ENDCG
        }
    }
}