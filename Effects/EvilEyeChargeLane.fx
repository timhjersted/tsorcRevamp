sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float2 UV(float2 c) { return c * PrimaryTextureSize / max(DrawSize, float2(1, 1)); }

float4 Aperture(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = UV(c) - 0.5;
    float r = length(float2(p.x, p.y * 0.7));
    float radius = lerp(0.44, 0.23, Progress);
    float ring = saturate((0.04 - abs(r - radius)) * 28.0);
    float lids = saturate((0.055 - abs(abs(p.y) - (radius - abs(p.x) * 0.45))) * 22.0);
    float core = Active * saturate((0.2 - r) * 6.0);
    float box = max(abs(p.x), abs(p.y));
    float boundary = Active * saturate((0.04 - abs(box - 0.46)) * 28.0);
    float3 color = lerp(MidColor, CoreColor, ring * Progress + core + boundary);
    return float4(color * (ring + lids * 0.7 + core * 1.4 + boundary * 1.5), saturate(ring + lids + core + boundary) * Opacity) * v;
}

float4 Lane(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.3, 3.2) + float2(-Time * 0.16, 0)).r;
    float taper = saturate(uv.x * 7.0) * saturate((1.0 - uv.x) * 2.0);
    float laneValue = saturate(streak * 0.78 + n * 0.22 - 0.5) * taper;
    float core = saturate((streak - 0.78) * 4.0) * taper * Active;
    float3 color = lerp(MidColor, CoreColor, Progress + core);
    return float4(color * (laneValue * 0.62 + core * 1.4), saturate(laneValue + core) * Opacity) * v;
}

float4 Wake(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 uv = UV(c);
    float streak = tex2D(PrimarySampler, uv).r;
    float n = tex2D(DetailSampler, uv * float2(2.2, 3.8) + float2(-Time * 0.24, Time * 0.04)).r;
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 2.0);
    float body = saturate(streak * 0.7 + n * 0.36 - 0.34) * taper;
    float core = saturate((streak - 0.72) * 3.6) * taper;
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core);
    return float4(color * (body * 0.68 + core * 1.35), saturate(body + core) * Opacity) * v;
}

technique EvilEyeChargeAperture { pass P { PixelShader = compile ps_2_0 Aperture(); } }
technique EvilEyeChargeLane { pass P { PixelShader = compile ps_2_0 Lane(); } }
technique EvilEyeChargeWake { pass P { PixelShader = compile ps_2_0 Wake(); } }
