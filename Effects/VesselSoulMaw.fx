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

float4 SoulMawPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;
    float spiral = tex2D(PrimarySampler, uv + float2(Time * 0.018, -Time * 0.012)).r;
    float noise = tex2D(DetailSampler, p * 3.7 + float2(-Time * 0.08, Time * 0.055)).r;
    float influence = saturate((1.0 - r) * 9.0);
    float boundary = saturate(1.0 - abs(r - 0.965) * 32.0);
    float calmCore = saturate((Direction - r) * 18.0);
    float flowLines = saturate((spiral - 0.48) * 3.4) * saturate((noise + spiral - 0.72) * 2.2);
    float streams = flowLines * influence * saturate((r - Direction) * 6.0) * (0.62 + Progress * 0.38);
    float aperture = saturate((0.23 - r) * 10.0);
    float hotCommit = Active * saturate((0.48 - r) * 3.0);
    float3 color = lerp(DarkColor, MidColor, streams + spiral * influence * 0.24);
    color = lerp(color, CoreColor, streams * streams * 0.72 + hotCommit * 0.42);
    color = lerp(color, DarkColor, max(calmCore, aperture));
    float intensity = streams * (0.52 + Progress * 0.36) + boundary * 0.32 + hotCommit * 0.5;
    float alpha = saturate(streams * 0.72 + influence * noise * 0.12 + boundary * 0.20 + aperture * 0.72) * Opacity;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

technique VesselSoulMaw
{
    pass SoulMawPass { PixelShader = compile ps_2_0 SoulMawPixel(); }
}
