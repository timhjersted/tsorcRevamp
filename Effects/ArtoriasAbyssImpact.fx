// ArtoriasAbyssImpact.fx
// Ragged, volumetric impact cloud for the damaging Flip Slash landing blast.

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

float4 ImpactBodyPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 0..1 across the quad, snapped to ArtoriasVFX.DrawImpactBlast's 4px gameplay-pixel blocks.
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    float smoke = tex2D(PrimarySampler, uv).r;
    // The smoke textures are not centred and reach their edges, so the quad used to slice them flat.
    // quadFade is 0 at radius 0.5 from the centre (before every quad edge), independent of the texture.
    float quadFade = saturate((0.5 - length(uv - 0.5)) * 5.0);
    float alpha = sampleColor.a * smoke * quadFade * quadFade * Opacity;
    return float4(sampleColor.rgb * MidColor * alpha, alpha);
}

technique ArtoriasAbyssImpactBody
{
    pass ImpactBodyPass { PixelShader = compile ps_2_0 ImpactBodyPixel(); }
}
