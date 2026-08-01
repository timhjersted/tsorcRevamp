sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Tail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.2, 3.5) + float2(-Time * 0.2, Time * 0.05)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.2);
    float body = saturate(streak * 0.72 + n * 0.32 - 0.36) * taper;
    float vein = Active * saturate((abs(sin((uv.x + n * 0.15) * 20.0)) - 0.82) * 6.0) * body;
    float core = saturate((streak - 0.72) * 3.5) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core + vein);
    return float4(color * (body * 0.62 + core * 1.35 + vein), saturate(body + core + vein) * Opacity) * v;
}

float4 Core(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p) * 2.0;
    float orb = saturate((0.92 - r) * 9.0);
    float core = saturate((0.46 - r) * 10.0);
    float3 color = lerp(MidColor, CoreColor, core);
    return float4(color * (orb * 0.65 + core * 1.5), saturate(orb + core) * Opacity) * v;
}

float4 Impact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(float2(p.x, p.y * 1.7));
    float ring = saturate((0.055 - abs(r - lerp(0.12, 0.43, Progress))) * 22.0);
    float pupil = saturate((0.17 - length(p)) * 6.0) * (1.0 - Progress);
    float3 color = lerp(MidColor, CoreColor, pupil + ring * 0.5);
    return float4(color * (ring + pupil * 1.5), saturate(ring + pupil) * Opacity) * v;
}

technique EvilEyeFlameTail { pass P { PixelShader = compile ps_2_0 Tail(); } }
technique EvilEyeFlameCore { pass P { PixelShader = compile ps_2_0 Core(); } }
technique EvilEyeFlameImpact { pass P { PixelShader = compile ps_2_0 Impact(); } }
