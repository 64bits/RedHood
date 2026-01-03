Shader "UI/StitchedRoundedRect_Pro"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Background Color", Color) = (1,1,1,1)
        
        _RectSize ("Rect Size (px)", Vector) = (100, 100, 0, 0)
        _Radius ("Corner Radius (px)", Float) = 15
        
        [Header(Stitches)]
        _StitchColor ("Stitch Color", Color) = (0,0,0,1)
        _StitchInset ("Stitch Inset (px)", Float) = 8
        _StitchThickness ("Stitch Width (px)", Float) = 2
        _StitchLength ("Stitch Length (px)", Float) = 6
        _StitchSpacing ("Stitch Gap (px)", Float) = 4

        // UI Required
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _StitchColor;
            float4 _RectSize;
            float _Radius;
            float _StitchInset;
            float _StitchThickness;
            float _StitchLength;
            float _StitchSpacing;
            float4 _ClipRect;

            v2f vert(appdata_t v) {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float sdRoundedBox(float2 p, float2 b, float r) {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target {
                // Transform UV to pixel coordinates relative to center
                float2 pixelPos = (IN.uv - 0.5) * _RectSize.xy;
                float2 halfSize = _RectSize.xy * 0.5;

                // 1. Background Rounded Rect
                float d = sdRoundedBox(pixelPos, halfSize, _Radius);
                float alpha = smoothstep(1.0, 0.0, d);

                // 2. Stitch Path (The "Thread Line")
                float stitchD = sdRoundedBox(pixelPos, halfSize - _StitchInset, max(0, _Radius - _StitchInset));
                // Thickness of the line itself
                float stitchLine = smoothstep(_StitchThickness, _StitchThickness - 1.0, abs(stitchD));

                // 3. Stitched Perimeter Mapping (The "Dashes")
                // We use the SDF value to create a coordinate that wraps around the edge
                // This prevents the "slanted" look on sides.
                float dashCoord = 0;
                if (abs(pixelPos.x) * _RectSize.y > abs(pixelPos.y) * _RectSize.x)
                    dashCoord = pixelPos.y;
                else
                    dashCoord = pixelPos.x;

                float totalDashUnit = _StitchLength + _StitchSpacing;
                float dash = step(fmod(abs(dashCoord + 1000.0), totalDashUnit), _StitchLength);

                // 4. Composition
                fixed4 col = IN.color;
                float finalStitch = stitchLine * dash;
                col.rgb = lerp(col.rgb, _StitchColor.rgb, finalStitch * _StitchColor.a);
                col.a *= alpha;

                // Standard Unity UI Clipping
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}