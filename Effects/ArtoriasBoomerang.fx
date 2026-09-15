// ArtoriasBoomerang.fx
// Dedicated Reach-safe magic treatment for the hostile crescent boomerang.
// The recognizable sprite remains the crisp core; the wider moving layers live
// in separate effects so each legacy Reach program stays comfortably small.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;
float Layer;
float2 FrameUVOrigin;
float2 FrameUVScale;
// ArtoriasFanCrescentPixelated only. PixelGrid is blocks across the (padded) frame and their
// reciprocal; FrameBlockStep is one block in ATLAS uv (FrameUVScale / blocks). Both divided in C#.
float4 PixelGrid;
float2 FrameBlockStep;

float4 BoomerangCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = (coords - FrameUVOrigin) / FrameUVScale;
    float4 shape = tex2D(PrimarySampler, coords);
    float noise = tex2D(DetailSampler,
        uv * float2(2.8, 3.7) + float2(Time * 0.23 * Direction, -Time * 0.17)).r;
    float energy = saturate(noise * 0.82 + Active * 0.18);
    float3 color = lerp(DarkColor, MidColor, 0.38 + energy * 0.62);
    color = lerp(color, CoreColor, energy * Layer * 0.72);
    float brightness = 0.70 + energy * 0.72 + Layer * 0.22;
    float alpha = shape.a * lerp(0.38, 0.96, Layer) * Opacity;
    return float4(vertexColor.rgb * color * brightness, vertexColor.a * alpha);
}

technique ArtoriasBoomerangCore
{
    pass CorePass { PixelShader = compile ps_2_0 BoomerangCorePixel(); }
}

// Spiral Fan's spinning crescent, pixelated to match the blocky orbit/ribbon underlays it rides on.
// The sprite's alpha is binary (0 or 255), so all shading comes from eroding/dilating that mask one
// pixel block at a time: violet glow block outside, dark outline block inside, purple fill, lavender
// glints two blocks deep. Premultiplied for AlphaBlend (vfx-shader-tips §43) so it stays purple over sky.
float4 FanCrescentPixelatedPixel(float2 coords : TEXCOORD0) : COLOR0
{
    // Snap in FRAME space and map back to the atlas, so blocks line up with this frame, not the sheet.
    float2 frameUV = PixelateShaderUV((coords - FrameUVOrigin) / FrameUVScale, PixelGrid);
    float2 uv = FrameUVOrigin + frameUV * FrameUVScale;
    float2 stepX = float2(FrameBlockStep.x, 0.0);
    float2 stepY = float2(0.0, FrameBlockStep.y);

    float centre = tex2D(PrimarySampler, uv).a;
    float left = tex2D(PrimarySampler, uv - stepX).a;
    float right = tex2D(PrimarySampler, uv + stepX).a;
    float up = tex2D(PrimarySampler, uv - stepY).a;
    float down = tex2D(PrimarySampler, uv + stepY).a;
    float nearMin = min(min(left, right), min(up, down));
    float nearMax = max(max(left, right), max(up, down));
    float farMin = min(min(tex2D(PrimarySampler, uv - stepX * 2.0).a, tex2D(PrimarySampler, uv + stepX * 2.0).a),
        min(tex2D(PrimarySampler, uv - stepY * 2.0).a, tex2D(PrimarySampler, uv + stepY * 2.0).a));

    // fill = interior blocks, outline = the silhouette's own edge blocks, glow = one block past it.
    // glow is nonzero only within one ~5.5-texel block of the sprite, so it never reaches the quad
    // edge: the sprite has 8-18 empty rows above/below, and the caller pads 8 texels left/right.
    float fill = centre * nearMin;
    float outline = centre - fill;
    float glow = saturate(nearMax - centre);
    float deep = fill * farMin;

    // Noise in frame space rotates with the spinning quad, so the glints churn with the blade.
    // High frequency + high threshold keeps them to a few sparks: a big lavender patch washed out the purple.
    float shimmer = tex2D(DetailSampler, frameUV * 3.1 + float2(Time * 0.21, -Time * 0.34)).r;
    float glint = deep * saturate(shimmer * 3.0 - 1.5);

    float3 material = DarkColor * outline
        + MidColor * (fill * (0.70 + shimmer * 0.38) + glow * 0.62)
        + CoreColor * (glint * 0.65);
    float alpha = saturate(centre + glow * 0.50) * Opacity;
    return float4(material * Opacity, alpha);
}

technique ArtoriasFanCrescentPixelated
{
    pass FanCrescentPass { PixelShader = compile ps_2_0 FanCrescentPixelatedPixel(); }
}
