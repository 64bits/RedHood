void StitchMask_float(float2 UV, float Radius, float StitchCount, float StitchLength, float EdgeWidth, out float Mask)
{
    // Center UVs
    float2 centered = UV - 0.5;
    
    // Calculate distance to rounded square edge
    float2 d = abs(centered) - (0.5 - Radius);
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - Radius;
    
    // Create perimeter parameter (0-1 around the shape)
    float angle = atan2(centered.y, centered.x);
    float perimeter = (angle / 6.28318530718) + 0.5; // Normalize to 0-1
    
    // Create dash pattern
    float pattern = frac(perimeter * StitchCount);
    float stitch = step(pattern, StitchLength) * step(StitchLength, 1.0 - pattern + StitchLength);
    
    // Mask to edge region only
    float edgeMask = smoothstep(EdgeWidth, 0.0, abs(dist)) * smoothstep(0.0, -0.02, dist);
    
    Mask = stitch * edgeMask;
}