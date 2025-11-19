Shader "Custom/ImpostorNormalCulling"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
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
                float3 cardForward : TEXCOORD1; // Direction this card is facing (World Space Normal)
                float3 toCamera : TEXCOORD2;   // Direction from card center to camera (World Space)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Get the card's forward direction (normal) in world space
                o.cardForward = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                
                // Calculate direction from object center to camera. 
                // Using the vertex position is fine, but for impostors, using the center 
                // of the object (v.vertex == 0,0,0) is often better if available.
                // Assuming v.vertex is close to the center of the card for simplification:
                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz; // Use object origin for direction
                o.toCamera = normalize(_WorldSpaceCameraPos - worldPos);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate dot product between card's forward direction and camera direction
                float dotProduct = dot(i.cardForward, i.toCamera);
                
                // We want cards that are facing TOWARD the camera.
                // For a 4-card impostor (90 degrees per card), 
                // each card should be visible for 45 degrees on either side of its normal.
                // cos(45 degrees) ≈ 0.7071
                float cos45deg = 0.7071;
                
                // Only show card if its forward direction is within 45 degrees of the camera direction.
                // The dot product must be POSITIVE (i.e., angle < 90 deg) AND 
                // greater than cos(45 deg) to restrict it to the 45 degree cone.
                if (dotProduct < cos45deg)
                {
                    discard;
                }
                
                // Sample texture and apply alpha cutoff
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