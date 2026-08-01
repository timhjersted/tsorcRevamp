sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 FlailHead(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float2 p = uv - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, uv * 3.8 + float2(Time * 0.09, -Time * 0.13)).r;
    float aura = saturate((0.5 - r) * 3.0) * saturate(n * 1.7 - 0.58);
    float shell = saturate((0.05 - abs(r - 0.38)) * 20.0);
    float core = Active * saturate((0.22 - r) * 5.0) * (0.55 + 0.2 * sin(Time * 9.0));
    float3 color = lerp(DarkColor, MidColor, aura + shell);
    color = lerp(color, CoreColor, shell + core);
    return float4(color * (aura * 0.5 + shell + core * 1.35), saturate(aura + shell + core) * Opacity) * v;
}

float4 FlailTrail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.8, 3.7) + float2(-Time * 0.24, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.68 + n * 0.42 - 0.36) * taper;
    float core = saturate((streak - 0.72) * 3.6) * taper * Active;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.58 + core * 1.35), saturate(body + core) * Opacity) * v;
}

float4 FlailPulse(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(p);
    float n = tex2D(DetailSampler, UV(c) * 3.0 + float2(Time * 0.08, -Time * 0.06)).r;
    float ring = saturate((0.03 - abs(r - 0.46)) * 34.0);
    float inward = saturate((0.05 - abs(r - lerp(0.46, 0.18, Progress))) * 20.0);
    float haze = saturate((0.46 - r) * 4.0) * saturate(n * 1.6 - 0.68);
    float3 color = lerp(DarkColor, MidColor, haze + inward);
    color = lerp(color, CoreColor, ring + inward);
    return float4(color * (haze * 0.4 + inward + ring * 1.4), saturate(haze + inward + ring) * Opacity) * v;
}

float4 FlailEmber(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.5, 3.4) + float2(-Time * 0.2, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.2);
    float body = saturate(streak * 0.7 + n * 0.42 - 0.35) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate((streak - 0.76) * 4.0));
    return float4(color * body, body * Opacity) * v;
}

technique GreatBlackKnightFlailHead { pass P { PixelShader = compile ps_2_0 FlailHead(); } }
technique GreatBlackKnightFlailTrail { pass P { PixelShader = compile ps_2_0 FlailTrail(); } }
technique GreatBlackKnightFlailPulse { pass P { PixelShader = compile ps_2_0 FlailPulse(); } }
technique GreatBlackKnightFlailEmber { pass P { PixelShader = compile ps_2_0 FlailEmber(); } }
