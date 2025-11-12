Shader "Hidden/HighlightMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        // No depth testing needed - just render silhouettes
        ZWrite Off
        ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // Sample texture for alpha cutout support
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Discard transparent pixels
                clip(col.a - _Cutoff);
                
                // Output white (1.0) to mark Layer 6 objects
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
