// QuaraWaterProjectile.fx
// Water & Blue Flame projectile shaders for QuaraHydromancer in Reach ps_2_0
// All techniques clip to the circle primary texture's alpha to avoid square boundary

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
float4 uSourceRect;

float4 Bubble(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Sample circle texture alpha to clip to circular shape
    float circleMask = tex2D(PrimarySampler, c).a;
    if (circleMask < 0.01) return float4(0, 0, 0, 0);

    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 2.5));

    // Dual moving noise offsets for dynamic blue flames
    float2 flameUV1 = c * 2.2 + float2(Time * 0.18, -Time * 0.32);
    float2 flameUV2 = c * 3.5 + float2(-Time * 0.28, Time * 0.22);
    float n1 = tex2D(DetailSampler, flameUV1).r;
    float n2 = tex2D(DetailSampler, flameUV2).r;

    float intensity = saturate(n1 * 1.3 + n2 * 0.8 - 0.4);
    float flameBody = pow(intensity, 1.3) * saturate(1.0 - r * 0.6);

    // Specular rim highlight & glowing core
    float rim = saturate(1.0 - abs(r - 0.72) * 5.0);
    float coreGlow = saturate(1.0 - r * 1.8);

    float3 color = lerp(DarkColor, MidColor, flameBody);
    color = lerp(color, CoreColor, coreGlow + rim * 0.6);

    float alpha = saturate(flameBody * 1.3 + rim * 0.8 + coreGlow) * edgeMask * circleMask * Opacity;
    return float4(v.rgb * color * (1.2 + coreGlow * 0.8), alpha) * v.a;
}

float4 Droplet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float circleMask = tex2D(PrimarySampler, c).a;
    if (circleMask < 0.01) return float4(0, 0, 0, 0);

    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 2.2));

    float n = tex2D(DetailSampler, c * 3.0 + float2(Time * 0.12, 0.0)).r;
    float core = saturate(1.0 - r * 1.4);
    float body = core * (0.7 + n * 0.5);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate(1.0 - r * 2.2));

    float alpha = saturate(body * 1.3) * edgeMask * circleMask * Opacity;
    return float4(v.rgb * color * 1.3, alpha) * v.a;
}

float4 WaterBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float circleMask = tex2D(PrimarySampler, c).a;
    if (circleMask < 0.01) return float4(0, 0, 0, 0);

    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edgeMask = saturate(pow(1.0 - saturate(r), 2.0));

    // Dynamic blue flame explosion
    float2 noiseUV = c * 2.8 + float2(-Time * 0.3, Time * 0.25);
    float n = tex2D(DetailSampler, noiseUV).r;

    float waveRadius = lerp(0.1, 0.85, Progress);
    float shockwave = saturate(1.0 - abs(r - waveRadius) * 4.5);
    float flameSpray = saturate(1.0 - r * 1.1) * saturate(n * 1.8 - 0.4) * (1.0 - Progress);

    float body = shockwave * 1.2 + flameSpray * 1.5;
    float3 color = lerp(DarkColor, MidColor, flameSpray);
    color = lerp(color, CoreColor, shockwave);

    float alpha = saturate(body) * edgeMask * circleMask * Opacity * (1.0 - Progress * 0.5);
    return float4(v.rgb * color * (1.3 + shockwave * 0.7), alpha) * v.a;
}

technique QuaraBubble { pass P { PixelShader = compile ps_2_0 Bubble(); } }
technique QuaraDroplet { pass P { PixelShader = compile ps_2_0 Droplet(); } }
technique QuaraWaterBurst { pass P { PixelShader = compile ps_2_0 WaterBurst(); } }
