// Opt-in local-quad pixelation for procedural VFX. DrawSize is the final world-space quad size,
// so a PixelBlockSize of 2 creates deliberate 2x2 blocks at normal 1x gameplay zoom.
float2 PixelateShaderUV(float2 uv, float2 drawSize, float pixelBlockSize)
{
    float2 blockUV = pixelBlockSize / max(drawSize, float2(1.0, 1.0));
    return (floor(uv / blockUV) + 0.5) * blockUV;
}

// Pre-divided variant. `pixelGrid.xy` is the block count across the quad and `.zw` its reciprocal,
// both computed in C#. The max() and divide above are pure-uniform maths, and a raw ps_2_0 entry
// point has no preshader — it re-evaluates them for EVERY pixel, ~12 slots. This form costs ~2.
float2 PixelateShaderUV(float2 uv, float4 pixelGrid)
{
    return (floor(uv * pixelGrid.xy) + 0.5) * pixelGrid.zw;
}
