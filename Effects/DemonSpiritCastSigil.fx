sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 CastSigilPixel(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
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

float4 CastRayPixel(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 2.5);
    float ray = saturate((streak - 0.42) * 2.2) * taper;
    float3 color = lerp(MidColor, CoreColor, Progress);
    return float4(color * ray, ray * Opacity) * v;
}

technique DemonSpiritCastSigil { pass P { PixelShader = compile ps_2_0 CastSigilPixel(); } }
technique DemonSpiritCastRay { pass P { PixelShader = compile ps_2_0 CastRayPixel(); } }
