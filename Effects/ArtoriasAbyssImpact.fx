// ArtoriasAbyssImpact.fx
// Ragged, volumetric impact cloud for the damaging Flip Slash landing blast.

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

float4 ImpactBodyPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float smoke = tex2D(PrimarySampler, uv).r;
    float alpha = sampleColor.a * smoke * Opacity;
    return float4(sampleColor.rgb * MidColor * alpha, alpha);
}

technique ArtoriasAbyssImpactBody
{
    pass ImpactBodyPass { PixelShader = compile ps_3_0 ImpactBodyPixel(); }
}
