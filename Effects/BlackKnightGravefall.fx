sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Tear(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float n = tex2D(DetailSampler, p * float2(3.0, 5.0) + float2(Time * 0.1, 0)).r;
    float slit = saturate((0.055 + Progress * 0.018 - abs(p.y)) * 24.0);
    float taper = saturate((0.48 - abs(p.x)) * 5.0);
    float edge = saturate((0.018 - abs(abs(p.y) - 0.035)) * 55.0) * taper;
    float haze = slit * taper * (0.3 + n * 0.35);
    float core = edge * (0.65 + Progress * 0.35);
    float3 color = lerp(DarkColor, MidColor, haze);
    color = lerp(color, CoreColor, core);
    return float4(color * (haze * 0.6 + core * 1.55), saturate(haze + core) * Opacity) * v;
}

float4 Trail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.3, 4.0) + float2(-Time * 0.2, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.1);
    float body = saturate(streak * 0.72 + n * 0.36 - 0.34) * taper;
    float core = saturate((streak - 0.72) * 3.5) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.62 + core * 1.35), saturate(body + core) * Opacity) * v;
}

technique BlackKnightGraveTear { pass P { PixelShader = compile ps_2_0 Tear(); } }
technique BlackKnightGraveTrail { pass P { PixelShader = compile ps_2_0 Trail(); } }
