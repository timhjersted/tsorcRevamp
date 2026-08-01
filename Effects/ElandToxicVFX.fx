sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 ToxicField(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float circleDistance = length(p);
    float boxDistance = max(abs(p.x), abs(p.y));
    float shapeDistance = lerp(boxDistance, circleDistance, Direction);
    float boundary = saturate((0.032 - abs(shapeDistance - 0.46)) * 32.0);
    float inside = saturate((0.46 - shapeDistance) * 5.0);
    float n1 = tex2D(DetailSampler, uv * 2.7 + float2(Time * 0.055, -Time * 0.04)).r;
    float n2 = tex2D(DetailSampler, uv * 4.3 + float2(-Time * 0.035, Time * 0.06)).g;
    float fumes = inside * saturate(n1 * 0.72 + n2 * 0.55 - 0.47);
    float telegraphPulse = 0.72 + 0.18 * sin(Time * 9.0);
    float activeFill = lerp(0.22, 0.58, Active);
    float edgeCore = boundary * lerp(telegraphPulse, 1.0, Active);
    float fade = saturate(1.0 - Progress * Progress);
    float3 color = lerp(DarkColor, MidColor, fumes + boundary);
    color = lerp(color, CoreColor, edgeCore);
    float alpha = saturate(edgeCore + fumes * activeFill) * Opacity * fade;
    return float4(color * (fumes * activeFill + edgeCore * 1.45), alpha) * v;
}

float4 VenomGlob(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.6, 3.8) + float2(-Time * 0.22, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.1);
    float body = saturate(streak * 0.72 + n * 0.38 - 0.31) * taper;
    float core = saturate((streak - 0.68) * 3.1) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.72 + core * 1.35), saturate(body + core) * Opacity) * v;
}

float4 VenomImpact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.4 + float2(Time * 0.08, -Time * 0.05)).r;
    float ring = saturate((0.065 - abs(r - Progress * 0.5)) * 15.0);
    float splash = saturate((0.52 - r) * 3.0) * saturate(n * 1.7 - 0.63) * (1.0 - Progress);
    float core = ring + splash * 0.45;
    float3 color = lerp(DarkColor, MidColor, splash + ring);
    color = lerp(color, CoreColor, core);
    return float4(color * (splash * 0.6 + ring * 1.4), saturate(splash + ring) * Opacity) * v;
}

technique ElandToxicField { pass P { PixelShader = compile ps_2_0 ToxicField(); } }
technique ElandVenomGlob { pass P { PixelShader = compile ps_2_0 VenomGlob(); } }
technique ElandVenomImpact { pass P { PixelShader = compile ps_2_0 VenomImpact(); } }
