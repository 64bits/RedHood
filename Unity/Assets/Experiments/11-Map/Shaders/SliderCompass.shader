Shader "UI/SliderCompass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _IconAtlas ("Icon Atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Heading ("Heading", Range(0, 360)) = 0
        _BGColor ("Background Color", Color) = (0.1, 0.1, 0.1, 0.8)
        _TickColor ("Tick Color", Color) = (1, 1, 1, 1)
        _CardinalColor ("Cardinal Color", Color) = (1, 0.3, 0.2, 1)
        _CenterMarkerColor ("Center Marker", Color) = (1, 0.8, 0, 1)
        
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
                
                // Icon data arrays (max 32 icons)
                // x = angle, y = distance, z = iconIndex, w = enabled
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
                half4 col = _BGColor;
                
                // Visible range: 180 degrees total (90 each side)
                float visibleRange = 180.0;
                float degreesPerUnit = visibleRange;
                
                // Calculate world heading at this UV position
                float localDeg = (uv.x - 0.5) * degreesPerUnit;
                float worldDeg = _Heading + localDeg;
                worldDeg = fmod(worldDeg + 360.0, 360.0);
                
                // Draw ticks every 15 degrees
                float tickSpacing = 15.0;
                float nearestTick = round(worldDeg / tickSpacing) * tickSpacing;
                float distToTick = abs(worldDeg - nearestTick);
                
                // Tick width in degrees
                float tickWidth = 1.5;
                
                if (distToTick < tickWidth)
                {
                    // Determine tick height based on type
                    float tickHeight = 0.15;
                    half4 tickCol = _TickColor;
                    
                    int tickDeg = (int)round(nearestTick) % 360;
                    if (tickDeg < 0) tickDeg += 360;
                    
                    // Cardinal directions (N=0, E=90, S=180, W=270)
                    bool isCardinal = (tickDeg == 0 || tickDeg == 90 || tickDeg == 180 || tickDeg == 270);
                    // Intercardinal (NE, SE, SW, NW)
                    bool isIntercardinal = (tickDeg == 45 || tickDeg == 135 || tickDeg == 225 || tickDeg == 315);
                    
                    if (isCardinal)
                    {
                        tickHeight = 0.4;
                        tickCol = _CardinalColor;
                    }
                    else if (isIntercardinal)
                    {
                        tickHeight = 0.25;
                    }
                    
                    // Draw tick from bottom
                    if (uv.y < tickHeight)
                    {
                        float edge = smoothstep(tickWidth, tickWidth * 0.5, distToTick);
                        col = lerp(col, tickCol, edge);
                    }
                }
                
                // Draw letters for cardinals
                float letterY = 0.55;
                float letterHeight = 0.35;
                
                if (uv.y > letterY && uv.y < letterY + letterHeight)
                {
                    // Check each cardinal
                    float cardinals[4] = {0, 90, 180, 270};
                    
                    for (int i = 0; i < 4; i++)
                    {
                        float cardDeg = cardinals[i];
                        float cardX = 0.5 + (cardDeg - _Heading) / degreesPerUnit;
                        
                        // Handle wrap-around
                        if (cardX < 0) cardX += 360.0 / degreesPerUnit;
                        if (cardX > 1) cardX -= 360.0 / degreesPerUnit;
                        
                        float distX = abs(uv.x - cardX);
                        
                        if (distX < 0.04)
                        {
                            // Simple block letter representation
                            float2 letterUV = float2((uv.x - cardX + 0.04) / 0.08, (uv.y - letterY) / letterHeight);
                            float letter = 0;
                            
                            // N
                            if (i == 0)
                            {
                                if (letterUV.x < 0.3 || letterUV.x > 0.7 || 
                                    (letterUV.x > 0.35 && letterUV.x < 0.65 && abs(letterUV.y - (1.0 - letterUV.x)) < 0.25))
                                    letter = 1;
                            }
                            // E
                            else if (i == 1)
                            {
                                if (letterUV.x < 0.35 || letterUV.y > 0.8 || letterUV.y < 0.2 || 
                                    (abs(letterUV.y - 0.5) < 0.1 && letterUV.x < 0.6))
                                    letter = 1;
                            }
                            // S
                            else if (i == 2)
                            {
                                if ((letterUV.y > 0.8 && letterUV.x > 0.2) || 
                                    (letterUV.y < 0.2 && letterUV.x < 0.8) ||
                                    (abs(letterUV.y - 0.5) < 0.1) ||
                                    (letterUV.x > 0.7 && letterUV.y < 0.5) ||
                                    (letterUV.x < 0.3 && letterUV.y > 0.5))
                                    letter = 1;
                            }
                            // W
                            else if (i == 3)
                            {
                                if (letterUV.x < 0.25 || letterUV.x > 0.75 ||
                                    (abs(letterUV.x - 0.5) < 0.15 && letterUV.y < 0.4) ||
                                    (letterUV.y < 0.3 && (abs(letterUV.x - 0.35) < 0.1 || abs(letterUV.x - 0.65) < 0.1)))
                                    letter = 1;
                            }
                            
                            if (letter > 0.5)
                            {
                                col = lerp(col, i == 0 ? _CardinalColor : _TickColor, 0.9);
                            }
                        }
                    }
                }
                
                // Draw icons
                float iconY = 0.25; // Vertical position of icons
                float iconHalfSize = _IconSize * 0.5;
                
                for (int j = 0; j < _IconCount; j++)
                {
                    float4 iconData = _IconData[j];
                    float iconAngle = iconData.x;
                    float iconDist = iconData.y;
                    int iconIndex = (int)iconData.z;
                    float enabled = iconData.w;
                    
                    if (enabled < 0.5) continue;
                    
                    // Calculate alpha based on distance
                    float alpha = 1.0;
                    if (iconDist > 50.0)
                        alpha = 0.0;
                    else if (iconDist > 25.0)
                        alpha = smoothstep(50.0, 25.0, iconDist) * 0.4;
                    else if (iconDist > 10.0)
                        alpha = 0.4 + smoothstep(25.0, 10.0, iconDist) * 0.6;
                    
                    if (alpha < 0.01) continue;
                    
                    // Calculate icon position on compass
                    float angleDiff = iconAngle - _Heading;
                    
                    // Normalize to -180 to 180
                    if (angleDiff > 180.0) angleDiff -= 360.0;
                    if (angleDiff < -180.0) angleDiff += 360.0;
                    
                    // Only draw if within visible range
                    if (abs(angleDiff) > 90.0) continue;
                    
                    float iconX = 0.5 + angleDiff / degreesPerUnit;
                    
                    // Check if current UV is within icon bounds
                    float2 iconUV = float2(
                        (uv.x - iconX) / _IconSize + 0.5,
                        (uv.y - iconY) / _IconSize + 0.5
                    );
                    
                    if (iconUV.x >= 0.0 && iconUV.x <= 1.0 && 
                        iconUV.y >= 0.0 && iconUV.y <= 1.0)
                    {
                        // Calculate atlas UV
                        int col_idx = iconIndex % _AtlasColumns;
                        int row_idx = iconIndex / _AtlasColumns;
                        
                        float2 atlasUV = float2(
                            (col_idx + iconUV.x) / (float)_AtlasColumns,
                            1.0 - (row_idx + iconUV.y) / (float)_AtlasRows
                        );
                        
                        half4 iconColor = SAMPLE_TEXTURE2D(_IconAtlas, sampler_IconAtlas, atlasUV);
                        iconColor.a *= alpha;
                        
                        // Blend icon over compass
                        col.rgb = lerp(col.rgb, iconColor.rgb, iconColor.a);
                        col.a = max(col.a, iconColor.a);
                    }
                }
                
                // Center marker (triangle at top)
                float centerDist = abs(uv.x - 0.5);
                float markerWidth = 0.02;
                float markerHeight = 0.15;
                
                if (uv.y > (1.0 - markerHeight) && centerDist < markerWidth * (1.0 - (uv.y - (1.0 - markerHeight)) / markerHeight))
                {
                    col = _CenterMarkerColor;
                }
                
                col *= input.color;
                return col;
            }
            ENDHLSL
        }
    }
}