sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Orb(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p) * 2.0;
    float spiral = tex2D(PrimarySampler, uv + float2(Time * 0.035, -Time * 0.025)).r;
    float n = tex2D(DetailSampler, uv * 3.6 + float2(-Time * 0.12, Time * 0.08)).r;
    float shell = saturate((0.92 - r) * 8.0) * saturate(spiral * 0.65 + n * 0.48 - 0.3);
    float facet = saturate((0.55 - max(abs(p.x), abs(p.y))) * 8.0);
    float core = saturate((0.38 - r) * 9.0);
    float3 color = lerp(DarkColor, MidColor, shell + facet * 0.35);
    color = lerp(color, CoreColor, core);
    return float4(color * (shell * 0.72 + facet * 0.35 + core * 1.4), saturate(shell + facet * 0.4 + core) * Opacity) * v;
}

float4 Ribbon(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.0, 3.5) + float2(-Time * 0.18, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.4);
    float body = saturate(streak * 0.72 + n * 0.32 - 0.37) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    return float4(color * body * 0.72, body * Opacity) * v;
}

technique DemonSpiritCrushOrb { pass P { PixelShader = compile ps_2_0 Orb(); } }
technique DemonSpiritCrushRibbon { pass P { PixelShader = compile ps_2_0 Ribbon(); } }
