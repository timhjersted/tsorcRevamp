sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

float2 UV(float2 c)
{
    return c * PrimaryTextureSize / max(DrawSize, float2(1, 1));
}

float4 Streak(float4 vertexColor : COLOR0, float2 coordinate : TEXCOORD0) : COLOR0
{
    float2 uv = UV(coordinate);
    float streak = tex2D(PrimarySampler, uv).r;
    float detail = tex2D(DetailSampler, uv * float2(2.4, 3.2) + float2(-Time * 0.24 * Direction, Time * 0.03)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.4);
    float body = saturate(streak * 0.78 + detail * 0.28 - 0.4) * taper;
    float core = saturate((streak - lerp(0.79, 0.66, Active)) * 4.2) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    float alpha = saturate(body * 0.78 + core * 1.25) * Opacity;
    return float4(color * (body * 0.7 + core * 1.45), alpha) * vertexColor;
}

float4 Ring(float4 vertexColor : COLOR0, float2 coordinate : TEXCOORD0) : COLOR0
{
    float2 p = UV(coordinate) - 0.5;
    float radius = length(p);
    float detail = tex2D(DetailSampler, p * 3.0 + 0.5 + float2(Time * 0.035 * Direction, -Time * 0.05)).r;
    float ringWidth = lerp(0.028, 0.048, Active);
    float ring = saturate((ringWidth - abs(radius - 0.43)) * 42.0);
    float inner = saturate((0.34 - radius) * 5.0) * (1.0 - Active) * (1.0 - Progress) * 0.28;
    float breaks = saturate(detail * 1.45 - lerp(0.7, 0.38, Active));
    float body = ring * lerp(0.55 + breaks * 0.45, 1.0, Active);
    float core = saturate((ring - 0.62) * 2.65) * Active;
    float3 color = lerp(DarkColor, MidColor, body + inner);
    color = lerp(color, CoreColor, core);
    float alpha = saturate(body + core + inner) * Opacity;
    return float4(color * (body * 0.78 + core * 1.5 + inner * 0.25), alpha) * vertexColor;
}

float4 Seal(float4 vertexColor : COLOR0, float2 coordinate : TEXCOORD0) : COLOR0
{
    float2 p = UV(coordinate) - 0.5;
    float radius = length(p);
    float outer = saturate((0.035 - abs(radius - lerp(0.46, 0.34, Progress))) * 34.0);
    float inner = saturate((0.025 - abs(radius - lerp(0.31, 0.12, Progress))) * 42.0);
    float axisRays = max(saturate((0.014 - abs(p.x)) * 72.0), saturate((0.014 - abs(p.y)) * 72.0));
    float diagonalRays = saturate((0.016 - abs(abs(p.x) - abs(p.y))) * 64.0);
    float rays = max(axisRays, diagonalRays) * saturate((0.42 - radius) * 6.0);
    float body = saturate(outer + inner * 0.75 + rays * 0.52);
    float core = saturate((outer + inner - 0.72) * 2.4) * Active;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    float alpha = saturate(body + core) * Opacity;
    return float4(color * (body * 0.72 + core * 1.45), alpha) * vertexColor;
}

technique RedKnightStreak { pass P { PixelShader = compile ps_2_0 Streak(); } }
technique RedKnightRing { pass P { PixelShader = compile ps_2_0 Ring(); } }
technique RedKnightSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
