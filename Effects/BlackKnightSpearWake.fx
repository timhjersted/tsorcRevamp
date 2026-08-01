sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Wake(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.2, 3.4) + float2(-Time * 0.2, 0)).r;
    float taper = saturate(uv.x * 5.5) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.75 + n * 0.3 - 0.38) * taper;
    float steel = saturate((streak - 0.72) * 3.6) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, steel);
    return float4(color * (body * 0.62 + steel * 1.3), saturate(body + steel) * Opacity) * v;
}

float4 Impact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float ring = saturate((0.045 - abs(r - lerp(0.16, 0.42, Progress))) * 28.0);
    float crossRay = saturate((0.022 - min(abs(p.x), abs(p.y))) * 45.0);
    float diagonalRay = saturate((0.02 - min(abs(p.x - p.y), abs(p.x + p.y))) * 50.0);
    float rune = saturate(crossRay + diagonalRay) * saturate((0.4 - r) * 6.0);
    float flash = saturate((0.18 - r) * 5.5) * (1.0 - Progress);
    float body = saturate(ring + rune);
    float3 color = lerp(MidColor, CoreColor, flash + ring * 0.5);
    return float4(color * (body + flash * 1.5), saturate(body + flash) * Opacity) * v;
}

technique BlackKnightSpearWake { pass P { PixelShader = compile ps_2_0 Wake(); } }
technique BlackKnightSpearImpact { pass P { PixelShader = compile ps_2_0 Impact(); } }
