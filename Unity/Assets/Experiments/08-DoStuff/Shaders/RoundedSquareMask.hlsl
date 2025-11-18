/**
 * Generates a HARD-EDGED (aliased) mask for a rounded rectangle
 * with circular corners.
 *
 * @param UV          The [0, 1] texture coordinates.
 * @param AspectRatio The aspect ratio of the rectangle (width / height).
 * @param Radius      The corner radius, normalized to the height.
 * @param Mask        The output mask (1.0 inside, 0.0 outside).
 */
void RoundedSquareMask_float(float2 UV, float AspectRatio, float Radius, out float Mask)
{
    // 1. Center UVs and correct for aspect ratio
    float2 centered = (UV - 0.5) * float2(AspectRatio, 1.0);
    float2 rectHalfSize = float2(0.5 * AspectRatio, 0.5);

    // 2. Calculate signed distance (SDF)
    float2 d = abs(centered) - (rectHalfSize - Radius);
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - Radius;
    
    // 3. Create the hard mask using 'step'
    // 'dist' is a signed distance: < 0 is inside, > 0 is outside.
    // step(dist, 0.0) returns 1.0 if 0.0 >= dist (inside)
    // and 0.0 if 0.0 < dist (outside).
    Mask = step(dist, 0.0);
}