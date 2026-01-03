Shader "UI/BavarianBorder"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BorderThickness ("Stripe Thickness (px)", Float) = 20
        _OuterInset ("Outer Inset (px)", Float) = 4
        _StripeWidth ("Stripe Width (px)", Float) = 15
        _ColorA ("Stripe Color A", Color) = (0.15, 0.45, 0.8, 1)
        _ColorB ("Stripe Color B", Color) = (1, 1, 1, 1)
        _BackgroundColor ("Center/Outer Color", Color) = (1, 1, 1, 1)
        
        _RectSize ("Rect Size (px)", Vector) = (100, 100, 0, 0)

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float2 pixelPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _BorderThickness;
            float _OuterInset;
            float _StripeWidth;
            float4 _ColorA;
            float4 _ColorB;
            float4 _BackgroundColor;
            float4 _RectSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                // Center the pixel coordinates (-halfSize to +halfSize)
                o.pixelPos = (v.texcoord - 0.5) * _RectSize.xy;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 halfSize = _RectSize.xy * 0.5;
                
                // 1. Calculate Distance to Edge in Pixels
                // Positive inside, negative outside
                float2 edgeDist2d = halfSize - abs(i.pixelPos);
                float d = min(edgeDist2d.x, edgeDist2d.y);

                // 2. Define Zones (in pixels)
                float isStripe = step(_OuterInset, d) * step(d, _OuterInset + _BorderThickness);
                float isInside = step(_OuterInset + _BorderThickness, d);
                
                // 3. Mitered Diagonal Stripes
                // By using (pixelPos.x + pixelPos.y), we get a perfect 45-degree angle
                // regardless of the UI element's aspect ratio.
                float stripeCoord = i.pixelPos.x + i.pixelPos.y;
                
                // Calculate stripe pattern based on pixel width
                // We multiply by 0.5 because the x+y gradient moves faster than x or y alone
                float pattern = frac(stripeCoord / (_StripeWidth * 2.0));
                float patternMask = step(0.5, pattern);
                float4 stripeCol = lerp(_ColorA, _ColorB, patternMask);
                
                // 4. Final Composition
                float4 finalColor = _BackgroundColor;
                
                // Layer the stripes over the background
                finalColor = lerp(finalColor, stripeCol, isStripe);
                
                // Apply the inner center color (if you want the middle to be solid)
                // If you want the center to be transparent/different, adjust here.
                // finalColor = lerp(finalColor, _BackgroundColor, isInside);

                // Standard UI Alpha/Texture handling
                fixed4 tex = tex2D(_MainTex, i.uv);
                finalColor.a *= i.color.a * tex.a;
                
                // Smooth edges (AA)
                float aa = smoothstep(0, 1, d); // Optional: clips the outer edges of the rect
                finalColor.a *= aa;

                return finalColor;
            }
            ENDCG
        }
    }
}