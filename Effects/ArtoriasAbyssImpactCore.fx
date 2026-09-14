// ArtoriasAbyssImpactCore.fx
// Hot fractured center for Artorias's Flip Slash landing blast.

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
float4 PixelGrid;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 ImpactCorePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 0..1 across the quad, snapped to ArtoriasVFX.DrawImpactBlast's 4px gameplay-pixel blocks.
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    // quadFade reaches 0 at radius 0.5 (before every quad edge), so the flare's long rays fade out instead of
    // being cut off where they run past the quad.
    float quadFade = saturate((0.5 - length(uv - 0.5)) * 5.0);
    float flare = tex2D(PrimarySampler, uv).r * quadFade * quadFade;
    return float4(sampleColor.rgb * CoreColor * flare,
        sampleColor.a * flare * Opacity);
}

technique ArtoriasAbyssImpactCore
{
    pass ImpactCorePass { PixelShader = compile ps_2_0 ImpactCorePixel(); }
}
