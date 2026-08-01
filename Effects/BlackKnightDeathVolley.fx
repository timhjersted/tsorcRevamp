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
    float n = tex2D(DetailSampler, p * 3.0 + float2(-Time * 0.1, Time * 0.06)).r;
    float radius = lerp(0.45, 0.13, Progress);
    float outer = saturate((0.045 - abs(r - radius)) * 28.0);
    float rays = saturate((0.02 - min(abs(p.x), abs(p.y))) * 50.0);
    float diagonals = saturate((0.018 - min(abs(p.x - p.y), abs(p.x + p.y))) * 55.0);
    float teeth = saturate(rays + diagonals) * saturate((radius - r) * 6.0);
    float collapse = saturate((radius - r) * 5.0) * (0.18 + n * 0.28);
    float body = saturate(outer + teeth + collapse);
    float core = saturate((outer - 0.7) * 3.3);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.75 + core * 1.5), saturate(body + core) * Opacity) * v;
}

float4 Thread(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.2, 3.0) + float2(-Time * 0.16, 0)).r;
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 3.0);
    float threadValue = saturate(streak * 0.8 + n * 0.24 - 0.48) * taper;
    float3 color = lerp(MidColor, CoreColor, Progress);
    return float4(color * threadValue, threadValue * Opacity) * v;
}

float4 SpectralTrail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.6, 3.5) + float2(-Time * 0.22, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.2);
    float ragged = saturate(streak * 0.7 + n * 0.4 - 0.33) * taper;
    float core = saturate((streak - 0.7) * 3.3) * taper;
    float3 color = lerp(DarkColor, MidColor, ragged);
    color = lerp(color, CoreColor, core);
    return float4(color * (ragged * 0.7 + core * 1.25), saturate(ragged + core) * Opacity) * v;
}

technique BlackKnightDeathSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
technique BlackKnightAimThread { pass P { PixelShader = compile ps_2_0 Thread(); } }
technique BlackKnightDeathTrail { pass P { PixelShader = compile ps_2_0 SpectralTrail(); } }
