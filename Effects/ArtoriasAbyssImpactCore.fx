// ArtoriasAbyssImpactCore.fx
// Hot fractured center for Artorias's Flip Slash landing blast.

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

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 ImpactCorePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float flare = tex2D(PrimarySampler, uv).r;
    return float4(sampleColor.rgb * CoreColor * flare,
        sampleColor.a * flare * Opacity);
}

technique ArtoriasAbyssImpactCore
{
    pass ImpactCorePass { PixelShader = compile ps_3_0 ImpactCorePixel(); }
}
