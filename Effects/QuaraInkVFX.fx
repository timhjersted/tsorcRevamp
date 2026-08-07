sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 InkGeyser(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.2 + float2(Time * 0.06, -Time * 0.08)).r;
    float boundary = saturate((0.035 - abs(r - 0.46)) * 32.0);
    float inside = saturate((0.46 - r) * 5.0);
    float cloud = inside * saturate(n * 1.35 - 0.42);
    float inward = saturate((0.04 - abs(r - lerp(0.46, 0.12, Progress))) * 25.0) * (1.0 - Active);
    float activeCore = Active * saturate((0.22 - r) * 5.0) * (0.55 + n * 0.45);
    float body = saturate(boundary + cloud * lerp(0.22, 0.72, Active) + inward + activeCore);
    float3 color = lerp(DarkColor, MidColor, cloud + boundary);
    color = lerp(color, CoreColor, boundary + inward + activeCore);
    return float4(color * (cloud * 0.45 + boundary * 1.25 + inward + activeCore), body * Opacity) * v;
}

float4 InkJet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.5, 3.6) + float2(-Time * 0.24, Time * 0.05)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.65 + n * 0.45 - 0.38) * taper;
    float core = saturate((streak - 0.74) * 3.8) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.58 + core * 1.35), saturate(body + core) * Opacity) * v;
}

float4 InkBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.5 + float2(Time * 0.07, -Time * 0.06)).r;
    float ring = saturate((0.07 - abs(r - Progress * 0.5)) * 14.0);
    float blot = saturate((0.52 - r) * 3.0) * saturate(n * 1.8 - 0.7) * (1.0 - Progress);
    float3 color = lerp(DarkColor, MidColor, blot + ring);
    color = lerp(color, CoreColor, ring);
    return float4(color * (blot * 0.62 + ring * 1.3), saturate(blot + ring) * Opacity) * v;
}

technique QuaraInkGeyser { pass P { PixelShader = compile ps_2_0 InkGeyser(); } }
technique QuaraInkJet { pass P { PixelShader = compile ps_2_0 InkJet(); } }
technique QuaraInkBurst { pass P { PixelShader = compile ps_2_0 InkBurst(); } }
