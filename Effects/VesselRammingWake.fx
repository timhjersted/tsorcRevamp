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

float4 RamCorePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = abs(uv - 0.5) * 2.0;
    float noise = tex2D(DetailSampler, uv * 4.0 + float2(-Time * 0.13, Time * 0.07)).r;
    float box = saturate((1.0 - max(p.x, p.y)) * 10.0);
    float edge = saturate(1.0 - abs(max(p.x, p.y) - 0.94) * 22.0);
    float pressure = box * saturate(noise * 1.4 - 0.25);
    float3 color = lerp(DarkColor, MidColor, pressure);
    color = lerp(color, CoreColor, edge);
    return float4(sampleColor.rgb * color * (pressure * 0.42 + edge * 1.25),
        sampleColor.a * saturate(pressure * 0.45 + edge) * Opacity);
}

float4 RamWakePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float streak = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.2, 3.6) + float2(-Time * 0.22, Time * 0.03)).r;
    float taper = saturate(uv.x * 3.0) * saturate((1.0 - uv.x) * 1.7);
    float wake = saturate(streak * 0.7 + noise * 0.44 - 0.32) * taper;
    float soul = saturate((noise - 0.7) * 3.3) * wake;
    float3 color = lerp(DarkColor, MidColor, wake);
    color = lerp(color, CoreColor, soul * 0.72 + Active * wake * 0.18);
    return float4(sampleColor.rgb * color * (wake * 0.58 + soul * 0.8), sampleColor.a * wake * Opacity);
}

technique VesselRammingCore { pass CorePass { PixelShader = compile ps_2_0 RamCorePixel(); } }
technique VesselRammingWake { pass WakePass { PixelShader = compile ps_2_0 RamWakePixel(); } }
