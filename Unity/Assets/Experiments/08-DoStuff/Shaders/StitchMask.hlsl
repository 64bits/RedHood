/**
 * Generates a mask for angled, leather-style stitches inset from the
 * edge of an aspect-corrected rounded rectangle.
 *
 * @param UV              The [0, 1] texture coordinates.
 * @param AspectRatio     The aspect ratio of the rectangle (width / height).
 * @param Radius          The corner radius, normalized to the height.
 * @param StitchInset     How far from the edge to center the stitch line (e.g., 0.05).
 * @param StitchWidth     How "thick" the stitch line is (e.g., 0.02).
 * @param StitchTiling    The frequency/count of the stitches (e.g., 50.0).
 * @param StitchFillRatio The ratio of stitch-to-gap (e.g., 0.7 is 70% stitch, 30% gap).
 * @param StitchAngle     The angle of the stitches in radians (e.g., 0.785 for 45°).
 * @param Smoothness      A small value for anti-aliasing (e.g., 0.005).
 * @param Mask            The output mask (1.0 for stitches, 0.0 elsewhere).
 */
void StitchMask_float(
    float2 UV, 
    float AspectRatio, 
    float Radius, 
    float StitchInset, 
    float StitchWidth, 
    float StitchTiling, 
    float StitchFillRatio, 
    float StitchAngle, 
    float Smoothness,
    out float Mask
)
{
    // 1. Correct for aspect ratio (Fixes stretched corners)
    float2 centered = (UV - 0.5) * float2(AspectRatio, 1.0);
    float2 rectHalfSize = float2(0.5 * AspectRatio, 0.5);

    // 2. Calculate non-stretched Signed Distance (SDF) to the rounded rect
    // 'dist' is 0.0 on the edge, negative inside, positive outside.
    float2 d = abs(centered) - (rectHalfSize - Radius);
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - Radius;

    // 3. Create the stitch pattern from rotated coordinates (Fixes perimeter bug)
    float c = cos(StitchAngle);
    float s = sin(StitchAngle);
    
    // We rotate the (aspect-corrected) 'centered' coords.
    // This makes the pattern geometrically correct and stationary.
    float2 rotatedCentered = float2(centered.x * c - centered.y * s, 
                                  centered.x * s + centered.y * c);
                             
    // Create a repeating line pattern from the rotated Y coordinate
    float pattern = frac(rotatedCentered.y * StitchTiling);
    float stitch = step(pattern, StitchFillRatio); // 1.0 for stitch, 0.0 for gap

    // 4. Create the mask for the band *inside* the edge
    // We want a band centered at 'dist = -StitchInset'.
    float bandCenter = -StitchInset;
    float bandDist = abs(dist - bandCenter); // 0 at band center, grows outwards
    
    // Use smoothstep to create a soft band of 'StitchWidth'
    float halfWidth = StitchWidth * 0.5;
    float edgeMask = smoothstep(halfWidth + Smoothness, halfWidth - Smoothness, bandDist);

    // 5. Combine the stitch pattern and the edge band
    Mask = stitch * edgeMask;
}