Shader "UI/BavarianBorder_Fixed"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BorderThickness ("Stripe Thickness", Range(0.0, 0.5)) = 0.08
        _OuterInset ("Outer Inset", Range(0.0, 0.2)) = 0.02
        _StripeScale ("Stripe Density", Float) = 10
        _ColorA ("Stripe Color A (Corner)", Color) = (0.15, 0.45, 0.8, 1)
        _ColorB ("Stripe Color B", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Center/Outer Color", Color) = (1, 1, 1, 1)
        
        // Required for UI.Mask
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float _BorderThickness;
            float _OuterInset;
            float _StripeScale;
            float4 _ColorA;
            float4 _ColorB;
            float4 _BackgroundColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // 1. Calculate Signed Distance Field for the rectangle edges
                // d is the distance from the nearest edge (0 at edge, 0.5 at center)
                float2 dists = min(uv, 1.0 - uv);
                float d = min(dists.x, dists.y);

                // 2. Define our zones
                float stripeStart = _OuterInset;
                float stripeEnd = _OuterInset + _BorderThickness;
                
                // Mask for the stripe area
                float isStripe = step(stripeStart, d) * step(d, stripeEnd);
                
                // 3. Mitered Coordinate System for Stripes
                // This ensures stripes "turn the corner" correctly.
                // We use (x+y) but flip logic based on which edge we are closer to.
                float stripeCoord = (uv.x + uv.y);
                
                // To force the "Elbow" at corners to be Color A:
                // We use the frac of the coordinate. 
                // Using (uv.x + uv.y) ensures the 0,0 and 1,1 corners start at the same phase.
                float pattern = frac(stripeCoord * _StripeScale);
                float patternMask = step(0.5, pattern);
                
                // Smooth out the pattern mask slightly if desired, or keep it sharp
                float4 stripeCol = lerp(_ColorA, _ColorB, patternMask);
                
                // 4. Final Color Composition
                // If in stripe zone, use stripeCol. Otherwise, use _BackgroundColor.
                float4 finalColor = lerp(_BackgroundColor, stripeCol, isStripe);
                
                // Standard UI Alpha handling
                fixed4 tex = tex2D(_MainTex, uv);
                finalColor.a *= i.color.a * tex.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}