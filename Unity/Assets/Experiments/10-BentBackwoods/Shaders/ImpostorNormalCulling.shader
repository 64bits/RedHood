Shader "Custom/ImpostorNormalCulling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        
        [Header(Culling Settings)]
        // 0.923 is mathematically correct for 8 cards (45 deg separation)
        // Adjust this slightly (0.91 - 0.93) if you see gaps or overlap.
        _AngleThreshold ("View Angle Threshold", Range(0.5, 1.0)) = 0.92
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        LOD 100

        Pass
        {
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 cardForward : TEXCOORD1; 
                float3 toCamera : TEXCOORD2;   
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float _AngleThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // 1. Get World Space Normal
                float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
                
                // 2. FLATTEN Y: Project onto XZ plane and normalize
                // This ensures the angle check ignores vertical tilt
                worldNormal.y = 0;
                o.cardForward = normalize(worldNormal);
                
                // 3. Get Direction to Camera (from object center)
                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz; 
                float3 toCam = _WorldSpaceCameraPos - worldPos;
                
                // 4. FLATTEN Y: Project camera direction onto XZ plane
                toCam.y = 0;
                o.toCamera = normalize(toCam);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate "Horizontal" dot product
                float dotProduct = dot(i.cardForward, i.toCamera);
                
                if (dotProduct < _AngleThreshold)
                {
                    discard;
                }
                
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a < _Cutoff)
                {
                    discard;
                }
                
                return col;
            }
            ENDCG
        }
    }
}