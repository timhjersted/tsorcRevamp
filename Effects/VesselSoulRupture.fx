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

float4 SoulRupturePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 centered = uv - 0.5;
    float radius = length(centered) * 2.0;
    float noise = tex2D(DetailSampler, centered * 4.4 + float2(-Time * 0.1, Time * 0.075)).r;
    float inside = saturate((1.0 - radius) * 18.0);
    float boundary = saturate(1.0 - abs(radius - 0.965) * 34.0) * inside;
    float corruptionFront = Progress * 1.16;
    float corrupt = saturate((corruptionFront - radius + (noise - 0.5) * 0.13) * 7.0) * inside;
    float activeFill = inside * Direction;
    float body = max(corrupt * (0.44 + noise * 0.42), activeFill * (0.7 + noise * 0.3));
    float veins = saturate((noise - 0.73) * 3.7) * body;
    float centerCore = saturate((0.24 - radius) * 4.2) * (0.35 + Progress * 0.65);
    float3 color = lerp(DarkColor, MidColor, body + veins);
    color = lerp(color, CoreColor, boundary + centerCore * 0.35);
    float intensity = body * 0.64 + veins * 0.45 + boundary * 1.25 + centerCore * 0.42;
    float alpha = saturate(body * 0.9 + boundary + centerCore * 0.35) * Opacity;
    return float4(sampleColor.rgb * color * intensity, sampleColor.a * alpha);
}

technique VesselSoulRupture
{
    pass SoulRupturePass { PixelShader = compile ps_2_0 SoulRupturePixel(); }
}
