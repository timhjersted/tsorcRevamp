// ArtoriasPurpleFire.fx
// Layered mask-driven Abyss flame for traveling floor fire and homing wisps.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;
float2 WorldDrawSize;
// Set by ArtoriasVFX.DrawHomingFlameWisp, the sole consumer of this effect.
float4 PixelGrid;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float MaskValue(float4 sampleValue)
{
    return max(sampleValue.a, max(sampleValue.r, max(sampleValue.g, sampleValue.b)));
}

// Pixelated (PixelGrid, 6px blocks - see ArtoriasVFX.DrawHomingFlameWisp) and less translucent
// than before: the old -0.38/-0.54 thresholds discarded most of the sprite mask before any color
// could show through, which is what read as "too translucent" - lowered here, plus a flat coverage
// boost on the final alpha, so the shape itself is unchanged but far more of it is visible.
float4 PurpleFireBodyPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    // The sin()-driven UV wobble this used to have (a subtle sub-pixel shimmer on the sampled mask)
    // cost ~20 arithmetic slots on its own at ps_2_0 - sin() expands into a multi-instruction
    // polynomial here, same finding as the tendril pixelation pass. Adding PixelGrid pushed this
    // shader (already at the ceiling before either change) well over 64 with it; the chunky pixel
    // quantization now supplies its own per-block variation, so the wobble wasn't buying much once
    // it was competing with a blockier, more solid-reading shape anyway.
    float mask = MaskValue(tex2D(PrimarySampler, uv));
    float noise = tex2D(DetailSampler,
        uv * float2(3.1, 4.7) + float2(Time * 0.08 * Direction, -Time * 0.18)).r;
    float sideFade = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
    float endFade = saturate(uv.y * 5.0) * saturate((1.0 - uv.y) * 5.0);
    float edgeFade = sideFade * endFade;
    float torn = saturate(mask * 1.12 + noise * 0.46 - 0.20) * edgeFade;
    float ember = torn * saturate((noise - 0.62) * 2.7);
    float3 color = lerp(DarkColor, MidColor, torn * (0.62 + Progress * 0.20));
    color = lerp(color, CoreColor, ember * 0.40);
    float alpha = vertexColor.a * saturate(torn * 1.35) * Opacity;
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 PurpleFireCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    // See PurpleFireBodyPixel above - same wobble, same reason it's gone.
    float mask = MaskValue(tex2D(PrimarySampler, uv));
    float noise = tex2D(DetailSampler,
        uv * float2(4.3, 5.6) + float2(-Time * 0.13, Time * 0.06 * Direction)).r;
    float sideFade = saturate(1.0 - abs(uv.x - 0.5) * 2.0);
    sideFade *= sideFade;
    float endFade = saturate(uv.y * 5.0) * saturate((1.0 - uv.y) * 5.0);
    float edgeFade = sideFade * endFade;
    float core = saturate(mask * 1.24 + noise * 0.30 - 0.36) * edgeFade;
    float hot = saturate((core - 0.40) * 1.92) * (0.72 + Active * 0.28);
    float3 color = lerp(MidColor, CoreColor, hot);
    float intensity = saturate(core * 0.95 + hot * 1.25);
    return float4(vertexColor.rgb * color * intensity,
        vertexColor.a * core * Opacity);
}

technique ArtoriasPurpleFireBody
{
    pass BodyPass { PixelShader = compile ps_2_0 PurpleFireBodyPixel(); }
}

technique ArtoriasPurpleFireCore
{
    pass CorePass { PixelShader = compile ps_2_0 PurpleFireCorePixel(); }
}
