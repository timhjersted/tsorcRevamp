sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Seal(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, p * 3.2 + float2(Time * 0.08, -Time * 0.06)).r;
    float radius = lerp(0.43, 0.25, Progress);
    float ring = saturate((0.035 - abs(r - radius)) * 34.0);
    float diagonal = min(abs(p.x - p.y), abs(p.x + p.y));
    float spokes = saturate((0.025 - diagonal) * 40.0) * saturate((radius - r) * 7.0) * saturate((r - 0.08) * 9.0);
    float shard = saturate((0.12 - abs(p.x) - abs(p.y) * 0.34) * 10.0) * Active;
    float body = saturate(ring + spokes * (0.55 + n * 0.45));
    float core = saturate(ring * ring + shard);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.8 + core * 1.4), saturate(body + core) * Opacity) * v;
}

float4 Comet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.4, 3.8) + float2(-Time * 0.18, Time * 0.05)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.72 + n * 0.36 - 0.3) * taper;
    float core = saturate((streak - 0.66) * 3.0) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.75 + core * 1.45), saturate(body + core) * Opacity) * v;
}

float4 Shatter(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float crossRay = saturate((0.025 - min(abs(p.x), abs(p.y))) * 40.0);
    float diagonalRay = saturate((0.022 - min(abs(p.x - p.y), abs(p.x + p.y))) * 45.0);
    float shards = saturate(crossRay + diagonalRay);
    float shell = saturate((0.09 - abs(r - Progress * 0.46)) * 13.0) * shards;
    float flash = saturate((0.22 - r) * 5.0) * (1.0 - Progress);
    float3 color = lerp(MidColor, CoreColor, flash + shell * 0.5);
    return float4(color * (shell + flash * 1.5), saturate(shell + flash) * Opacity) * v;
}

technique BlackKnightHexSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
technique BlackKnightHexComet { pass P { PixelShader = compile ps_2_0 Comet(); } }
technique BlackKnightHexShatter { pass P { PixelShader = compile ps_2_0 Shatter(); } }
