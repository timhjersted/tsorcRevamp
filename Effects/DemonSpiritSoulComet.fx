sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Comet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.4, 3.6) + float2(-Time * 0.18, Time * 0.06)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.0);
    float shroud = saturate(streak * 0.68 + n * 0.42 - 0.34) * taper;
    float core = saturate((streak - 0.72) * 3.6) * taper;
    float3 color = lerp(DarkColor, MidColor, shroud);
    color = lerp(color, CoreColor, core);
    return float4(color * (shroud * 0.65 + core * 1.4), saturate(shroud + core) * Opacity) * v;
}

float4 Brackets(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = abs(UV(c) - 0.5);
    float corner = saturate((0.045 - min(abs(p.x - 0.43), abs(p.y - 0.43))) * 28.0);
    float arms = saturate((0.17 - min(p.x, p.y)) * 10.0);
    float marker = corner * arms;
    float3 color = lerp(MidColor, CoreColor, Progress);
    return float4(color * marker * (0.65 + Progress * 0.75), marker * Opacity) * v;
}

float4 Burst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float box = max(abs(p.x), abs(p.y));
    float n = tex2D(DetailSampler, uv * 3.0 + float2(Time * 0.1, -Time * 0.08)).r;
    float blast = saturate((0.08 - abs(r - Progress * 0.55)) * 14.0);
    float square = saturate((0.035 - abs(box - 0.45)) * 34.0);
    float smoke = saturate((0.6 - r) * 2.6) * n * (1.0 - Progress);
    float flash = saturate((0.22 - r) * 5.0) * (1.0 - Progress);
    float3 color = lerp(DarkColor, MidColor, blast + smoke);
    color = lerp(color, CoreColor, square + flash);
    return float4(color * (smoke * 0.32 + blast + square * 1.2 + flash * 1.5), saturate(smoke + blast + square + flash) * Opacity) * v;
}

technique DemonSpiritSoulComet { pass P { PixelShader = compile ps_2_0 Comet(); } }
technique DemonSpiritExpiryBrackets { pass P { PixelShader = compile ps_2_0 Brackets(); } }
technique DemonSpiritSoulBurst { pass P { PixelShader = compile ps_2_0 Burst(); } }
