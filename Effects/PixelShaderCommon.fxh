// Opt-in local-quad pixelation for procedural VFX. DrawSize is the final world-space quad size,
// so a PixelBlockSize of 2 creates deliberate 2x2 blocks at normal 1x gameplay zoom.
float2 PixelateShaderUV(float2 uv, float2 drawSize, float pixelBlockSize)
{
    float2 blockUV = pixelBlockSize / max(drawSize, float2(1.0, 1.0));
    return (floor(uv / blockUV) + 0.5) * blockUV;
}
