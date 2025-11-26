Shader "UI/RadialProgress"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ProgressColor ("Progress Color", Color) = (0.816, 0.667, 0.475, 1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        
        // Added Outer Radius Property
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.48
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.3
        
        _StartAngle ("Start Angle", Range(0, 360)) = 0
        
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ProgressColor;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _FillAmount;
            float _InnerRadius;
            float _OuterRadius; // Variable declaration
            float _StartAngle;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centered = IN.texcoord - 0.5;
                float dist = length(centered);
                
                // Clockwise logic (atan2(x,y))
                float angle = atan2(centered.x, centered.y);
                angle = angle / (3.14159265 * 2.0);
                angle = frac(angle - (_StartAngle / 360.0));
                
                // --- OUTER RADIUS LOGIC ---
                // We use _OuterRadius as the hard edge, and subtract 0.02 to create a soft fade inward.
                // This smoothstep returns 0 if dist >= _OuterRadius (transparent)
                // and 1 if dist <= (_OuterRadius - 0.02) (opaque)
                float outerEdge = smoothstep(_OuterRadius, _OuterRadius - 0.02, dist);
                
                float innerEdge = smoothstep(_InnerRadius - 0.02, _InnerRadius, dist);
                float donut = outerEdge * innerEdge;
                
                float fill = step(angle, _FillAmount);
                
                fixed4 color = fixed4(0, 0, 0, 0);
                float progressMask = donut * fill;
                
                color.rgb = _ProgressColor.rgb * progressMask;
                color.a = progressMask * _ProgressColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                
                return color;
            }
            ENDCG
        }
    }
}