Shader "UI/SliderCompass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _IconAtlas ("Icon Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Heading ("Heading", Range(0, 360)) = 0
        
        // Colors
        _BGColor ("Background Color", Color) = (0.1, 0.1, 0.1, 0.8)
        _TickColor ("Tick Color", Color) = (1, 1, 1, 1)
        _CardinalColor ("Cardinal Color", Color) = (1, 0.3, 0.2, 1)
        _CenterMarkerColor ("Center Marker", Color) = (1, 0.8, 0, 1)
        
        // Fade settings passed from script (defaults here for testing)
        _MaxDistance ("Max Distance", Float) = 80.0
        _FadeStartDistance ("Fade Start Distance", Float) = 50.0
        
        // UI Masking
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
            "RenderPipeline"="UniversalPipeline"
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
            Name "Default"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_IconAtlas);
            SAMPLER(sampler_IconAtlas);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _Heading;
                half4 _BGColor;
                half4 _TickColor;
                half4 _CardinalColor;
                half4 _CenterMarkerColor;
                float _AspectRatio;
                
                float _MaxDistance;
                float _FadeStartDistance;
                
                // Icon data arrays (max 32 icons)
                float4 _IconData[32];
                int _IconCount;
                float _IconSize;
                int _AtlasColumns;
                int _AtlasRows;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // --- SECTION 1: BACKGROUND ---
                // We mainly color the bottom half (the tape). 
                // The top half (icons) is usually transparent, but let's give the whole thing a faint tint if desired.
                half4 col = (uv.y < 0.5) ? _BGColor : half4(0,0,0,0);
                
                float visibleRange = 180.0;
                float degreesPerUnit = visibleRange;
                
                // Calculate world heading at this UV position
                float localDeg = (uv.x - 0.5) * degreesPerUnit;
                float worldDeg = _Heading + localDeg;
                worldDeg = fmod(worldDeg + 360.0, 360.0);
                
                // --- SECTION 2: COMPASS TAPE (BOTTOM HALF 0.0 - 0.5) ---
                if (uv.y <= 0.5)
                {
                    // Draw ticks every 15 degrees
                    float tickSpacing = 15.0;
                    float nearestTick = round(worldDeg / tickSpacing) * tickSpacing;
                    float distToTick = abs(worldDeg - nearestTick);
                    float tickWidth = 1.5;
                    
                    if (distToTick < tickWidth)
                    {
                        // Heights are now relative to the 0.5 boundary
                        float tickHeight = 0.1; // Short tick
                        half4 tickCol = _TickColor;
                        
                        int tickDeg = (int)round(nearestTick) % 360;
                        if (tickDeg < 0) tickDeg += 360;
                        
                        bool isCardinal = (tickDeg == 0 || tickDeg == 90 || tickDeg == 180 || tickDeg == 270);
                        bool isIntercardinal = (tickDeg == 45 || tickDeg == 135 || tickDeg == 225 || tickDeg == 315);
                        
                        if (isCardinal)
                        {
                            tickHeight = 0.25; // Tallest (goes up to 0.25 UV)
                            tickCol = _CardinalColor;
                        }
                        else if (isIntercardinal)
                        {
                            tickHeight = 0.15;
                        }
                        
                        // Draw tick from bottom (y=0) up to height
                        if (uv.y < tickHeight)
                        {
                            float edge = smoothstep(tickWidth, tickWidth * 0.5, distToTick);
                            col = lerp(col, tickCol, edge);
                        }
                    }
                    
                    // Draw Letters (N, E, S, W)
                    // Centered vertically in the space between ticks and the divider (approx y=0.35)
                    float letterY = 0.35; 
                    float letterHeight = 0.15;
                    
                    if (uv.y > letterY - (letterHeight/2) && uv.y < letterY + (letterHeight/2))
                    {
                        float cardinals[4] = {0, 90, 180, 270};
                        
                        for (int i = 0; i < 4; i++)
                        {
                            float cardDeg = cardinals[i];
                            float cardX = 0.5 + (cardDeg - _Heading) / degreesPerUnit;
                            
                            // Handle wrap-around
                            if (cardX < 0) cardX += 360.0 / degreesPerUnit;
                            if (cardX > 1) cardX -= 360.0 / degreesPerUnit;
                            
                            float distX = abs(uv.x - cardX);
                            
                            // Width of the letter box
                            if (distX < 0.04)
                            {
                                // Adjust UVs to render letter
                                float2 letterUV = float2(
                                    (uv.x - cardX + 0.04) / 0.08, 
                                    (uv.y - (letterY - letterHeight/2)) / letterHeight
                                );
                                
                                float letter = 0;
                                
                                // (Logic for block letters N, E, S, W same as before)
                                // N
                                if (i == 0) {
                                    if (letterUV.x < 0.3 || letterUV.x > 0.7 || (letterUV.x > 0.35 && letterUV.x < 0.65 && abs(letterUV.y - (1.0 - letterUV.x)) < 0.25)) letter = 1;
                                }
                                // E
                                else if (i == 1) {
                                    if (letterUV.x < 0.35 || letterUV.y > 0.8 || letterUV.y < 0.2 || (abs(letterUV.y - 0.5) < 0.1 && letterUV.x < 0.6)) letter = 1;
                                }
                                // S
                                else if (i == 2) {
                                    if ((letterUV.y > 0.8 && letterUV.x > 0.2) || (letterUV.y < 0.2 && letterUV.x < 0.8) || (abs(letterUV.y - 0.5) < 0.1) || (letterUV.x > 0.7 && letterUV.y < 0.5) || (letterUV.x < 0.3 && letterUV.y > 0.5)) letter = 1;
                                }
                                // W
                                else if (i == 3) {
                                    if (letterUV.x < 0.25 || letterUV.x > 0.75 || (abs(letterUV.x - 0.5) < 0.15 && letterUV.y < 0.4) || (letterUV.y < 0.3 && (abs(letterUV.x - 0.35) < 0.1 || abs(letterUV.x - 0.65) < 0.1))) letter = 1;
                                }
                                
                                if (letter > 0.5) col = lerp(col, _CardinalColor, 0.9);
                            }
                        }
                    }
                } // End Bottom Half

                // --- SECTION 3: ICONS (TOP HALF 0.5 - 1.0) ---
                // We center icons at y = 0.75
                
                float iconYCenter = 0.75;

                for (int j = 0; j < _IconCount; j++)
                {
                    float4 iconData = _IconData[j];
                    float iconAngle = iconData.x;
                    float iconDist = iconData.y;
                    int iconIndex = (int)iconData.z;
                    float enabled = iconData.w;
                    
                    if (enabled < 0.5) continue;
                    
                    // Fade Logic
                    float alpha = 1.0 - smoothstep(_FadeStartDistance, _MaxDistance, iconDist);
                    if (alpha < 0.01) continue;
                    
                    // Positioning
                    float angleDiff = iconAngle - _Heading;
                    if (angleDiff > 180.0) angleDiff -= 360.0;
                    if (angleDiff < -180.0) angleDiff += 360.0;
                    if (abs(angleDiff) > 90.0) continue;
                    
                    float iconX = 0.5 + angleDiff / degreesPerUnit;
                    
                    // --- ASPECT RATIO CORRECTION HERE ---
                    // We multiply the X difference by _AspectRatio to counter the stretching.
                    // If aspect is 5 (wide bar), we need to traverse UV.x 5x faster to make a square.
                    
                    float2 iconUV = float2(
                        (uv.x - iconX) * (_AspectRatio / _IconSize) + 0.5, // Corrected X
                        (uv.y - iconYCenter) / _IconSize + 0.5           // Standard Y
                    );
                    
                    if (iconUV.x >= 0.0 && iconUV.x <= 1.0 && 
                        iconUV.y >= 0.0 && iconUV.y <= 1.0)
                    {
                        // Atlas sampling
                        int col_idx = iconIndex % _AtlasColumns;
                        int row_idx = iconIndex / _AtlasColumns;
                        
                        float2 atlasUV = float2(
                            (col_idx + iconUV.x) / (float)_AtlasColumns,
                            1.0 - (row_idx + iconUV.y) / (float)_AtlasRows
                        );
                        
                        half4 iconColor = SAMPLE_TEXTURE2D(_IconAtlas, sampler_IconAtlas, atlasUV);
                        iconColor.a *= alpha;
                        
                        col.rgb = lerp(col.rgb, iconColor.rgb, iconColor.a);
                        col.a = max(col.a, iconColor.a);
                    }
                }
                
                // --- SECTION 4: CENTER MARKER (AT Y=0.5) ---
                // Draws a small triangle pointing down at the division line
                float centerDist = abs(uv.x - 0.5);
                float markerW = 0.02;
                float markerH = 0.1;
                float markerTop = 0.5 + markerH;
                
                // Triangle math: Pointing down at 0.5
                if (uv.y > 0.5 && uv.y < markerTop)
                {
                    // Linear slope for triangle
                    float limit = markerW * ((uv.y - 0.5) / markerH);
                    if (centerDist < limit)
                    {
                        col = _CenterMarkerColor;
                    }
                }
                
                col *= input.color;
                return col;
            }
            ENDHLSL
        }
    }
}