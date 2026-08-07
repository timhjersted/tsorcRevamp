sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 WaterCast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.0 + float2(Time * 0.08, -Time * 0.09)).r;
    float radius = lerp(0.46, 0.29, Progress);
    float ring = saturate((0.038 - abs(r - radius)) * 30.0);
    float spokes = saturate((0.018 - min(abs(p.x), abs(p.y))) * 55.0) * saturate((radius - r) * 6.0);
    float gather = saturate((radius - r) * 4.0) * saturate(n * 1.5 - 0.55);
    float body = saturate(ring + spokes * (0.35 + Active * 0.15) + gather * 0.28);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, ring);
    return float4(color * (body * 0.75 + ring), body * Opacity) * v;
}

float4 Bubble(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 2.5 + float2(Time * 0.08, -Time * 0.06)).r;
    float shell = saturate((0.055 - abs(r - 0.43)) * 18.0);
    float inner = saturate((0.4 - r) * 3.0) * (0.1 + n * 0.18);
    float gleam = saturate((0.12 - length(p - float2(-0.15, -0.16))) * 8.0);
    float pressure = Active * saturate((0.035 - abs(r - lerp(0.18, 0.48, Progress))) * 28.0);
    float body = saturate(shell + inner + gleam + pressure);
    float3 color = lerp(DarkColor, MidColor, inner + shell);
    color = lerp(color, CoreColor, gleam + pressure + shell * 0.45);
    return float4(color * (inner * 0.45 + shell + gleam * 1.4 + pressure), body * Opacity) * v;
}

float4 TidalCrest(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float n = tex2D(DetailSampler, uv * float2(3.2, 2.2) + float2(-Time * 0.18 * Direction, Time * 0.03)).r;
    float edgeX = saturate((0.055 - min(uv.x, 1.0 - uv.x)) * 18.0);
    float edgeY = saturate((0.055 - min(uv.y, 1.0 - uv.y)) * 18.0);
    float boundary = saturate(edgeX + edgeY);
    float body = saturate(n * 1.15 - 0.28) * saturate(uv.y * 3.2);
    float foam = saturate((0.23 - uv.y) * 4.4) * saturate(n * 1.8 - 0.72);
    float3 color = lerp(DarkColor, MidColor, body + boundary);
    color = lerp(color, CoreColor, boundary + foam);
    return float4(color * (body * 0.46 + boundary * 1.2 + foam), saturate(body * 0.55 + boundary + foam) * Opacity) * v;
}

float4 RushPS(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float mask = tex2D(PrimarySampler, UV(c)).r;
    return float4(MidColor * mask, mask * Opacity) * v;
}

float4 WaterBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.2 + float2(Time * 0.08, -Time * 0.06)).r;
    float ring = saturate((0.06 - abs(r - Progress * 0.5)) * 16.0);
    float spray = saturate((0.54 - r) * 2.8) * saturate(n * 1.75 - 0.67) * (1.0 - Progress);
    float3 color = lerp(MidColor, CoreColor, ring);
    return float4(color * (ring * 1.35 + spray * 0.65), saturate(ring + spray) * Opacity) * v;
}

technique QuaraWaterCast { pass P { PixelShader = compile ps_2_0 WaterCast(); } }
technique QuaraBubble { pass P { PixelShader = compile ps_2_0 Bubble(); } }
technique QuaraTidalCrest { pass P { PixelShader = compile ps_2_0 TidalCrest(); } }
technique QuaraTideRush { pass P { PixelShader = compile ps_2_0 RushPS(); } }
technique QuaraWaterBurst { pass P { PixelShader = compile ps_2_0 WaterBurst(); } }
