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

float2 LocalUV(float2 coords) { return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0)); }

float4 SoulNovaPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float r = length(p);
    float noise = tex2D(DetailSampler, p * 5.2 + float2(-Time * 0.11, Time * 0.07)).r;
    float distanceToRing = abs(r - Active * 0.5);
    float band = saturate(1.0 - distanceToRing / max(Direction, 0.006));
    float torn = band * saturate(noise * 1.45 - 0.22);
    float leading = saturate(1.0 - abs(r - (Active * 0.5 + Direction * 0.72)) / max(Direction * 0.18, 0.002));
    float seam = saturate(1.0 - abs(r - (Active * 0.5 - Direction * 0.55)) / max(Direction * 0.25, 0.003));
    float wisps = saturate(1.0 - distanceToRing / (Direction * 2.1)) * saturate(noise - 0.5) * 0.45;
    float3 color = lerp(DarkColor, MidColor, torn + wisps);
    color = lerp(color, CoreColor, leading);
    color = lerp(color, DarkColor, seam * 0.82);
    float alpha = saturate(band * 0.90 + leading + wisps) * Opacity;
    return float4(sampleColor.rgb * color * (torn * 0.68 + leading * 1.45 + wisps * 0.35), sampleColor.a * alpha);
}

technique VesselSoulNova
{
    pass SoulNovaPass { PixelShader = compile ps_2_0 SoulNovaPixel(); }
}
