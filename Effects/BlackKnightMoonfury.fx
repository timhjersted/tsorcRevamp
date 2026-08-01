sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Coal(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;
    float n = tex2D(DetailSampler, uv * 3.7 + float2(Time * 0.1, -Time * 0.14)).r;
    float orb = saturate((0.92 - r) * 8.0);
    float pulse = 0.68 + 0.18 * sin(Time * 8.0) + Progress * 0.22;
    float fissure = orb * saturate(n * 1.8 - 0.85) * (0.55 + Active * 0.45);
    float core = saturate((0.42 - r) * 8.0) * pulse;
    float3 color = lerp(DarkColor, MidColor, orb + fissure);
    color = lerp(color, CoreColor, core + fissure * 0.5);
    return float4(color * (orb * 0.4 + fissure + core * 1.25), saturate(orb * 0.55 + fissure + core) * Opacity) * v;
}

float4 Smoke(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.0, 3.0) + float2(-Time * 0.13, Time * 0.06)).r;
    float taper = saturate(uv.x * 4.0) * saturate((1.0 - uv.x) * 2.2);
    float body = saturate(streak * 0.55 + n * 0.55 - 0.4) * taper;
    float3 color = lerp(DarkColor, MidColor, n);
    return float4(color * body * 0.55, body * Opacity * 0.7) * v;
}

float4 Blast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.0 + float2(Time * 0.12, -Time * 0.08)).r;
    float square = max(abs(p.x), abs(p.y));
    float boundary = saturate((0.035 - abs(square - 0.46)) * 34.0);
    float ring = saturate((0.07 - abs(r - Progress * 0.52)) * 15.0);
    float smoke = saturate((0.58 - r) * 3.0) * n * (1.0 - Progress);
    float core = boundary + ring * (0.6 + n * 0.4);
    float3 color = lerp(DarkColor, MidColor, smoke + ring);
    color = lerp(color, CoreColor, core);
    return float4(color * (smoke * 0.35 + ring + boundary * 1.45), saturate(smoke + ring + boundary) * Opacity) * v;
}

technique BlackKnightMoonfuryCoal { pass P { PixelShader = compile ps_2_0 Coal(); } }
technique BlackKnightMoonfurySmoke { pass P { PixelShader = compile ps_2_0 Smoke(); } }
technique BlackKnightMoonfuryBlast { pass P { PixelShader = compile ps_2_0 Blast(); } }
