// QuaraWaterProjectile.fx
// Bubble Burst's pressurised shell, launched-bubble overlay, and detonation splash.
// These passes draw through AlphaBlend, so each output is explicitly premultiplied.
// PixelGrid is filled by EnemyVFX.Draw: a 2x2 gameplay-pixel grid over the final draw quad.

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize, PixelDrawSize;
float4 PixelGrid, uSourceRect;

float2 PixelateWaterUV(float2 uv)
{
    return (floor(uv * PixelGrid.xy) + 0.5) * PixelGrid.zw;
}

// The casting bubble and the watery material immediately behind every launched Bubble projectile.
// Its noisy rim stays inside the supplied circle alpha, which removes the old square halo while
// retaining a bright enough silhouette at the launched sprite's small 24px size.
float4 Bubble(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateWaterUV(c);
    float circleMask = tex2D(PrimarySampler, c).a;
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edge = saturate((1.0 - r) * 3.4) * circleMask;

    // A single panning field keeps this underneath ps_2_0's instruction limit after pixelisation.
    float n = tex2D(DetailSampler, c * 3.25 + float2(Time * 0.18, -Time * 0.24)).r;
    float body = saturate(1.0 - r * 0.94) * saturate(n * 1.06 - 0.13);
    float rim = saturate(1.0 - abs(r - (0.67 + (n - 0.5) * 0.10)) * 7.8);
    float core = saturate(1.0 - r * 2.12) * (0.46 + n * 0.40);

    float3 color = lerp(DarkColor, MidColor, body * 0.88 + rim * 0.18);
    color = lerp(color, CoreColor, saturate(rim * (0.48 + Active * 0.18) + core * 0.42));
    float alpha = saturate(body * 0.80 + rim * (0.64 + Active * 0.10) + core * 0.20) * edge * Opacity;
    return float4(color * alpha, alpha) * v;
}

// A compact falling water bead. It shares the same pixel grid and premultiplied edge behaviour
// so callers can use it later without bringing the old rectangular fringe back.
float4 Droplet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateWaterUV(c);
    float circleMask = tex2D(PrimarySampler, c).a;
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edge = saturate((1.0 - r) * 3.6) * circleMask;
    float n = tex2D(DetailSampler, c * 3.15 + float2(Time * 0.12, -Time * 0.19)).r;
    float body = saturate(1.0 - r * 1.05) * saturate(n * 0.82 + 0.24);
    float glint = saturate(1.0 - length(p + float2(0.22, 0.30)) * 3.1) * saturate(n * 1.25);

    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, glint * 0.82);
    float alpha = saturate(body * 0.82 + glint * 0.34) * edge * Opacity;
    return float4(color * alpha, alpha) * v;
}

// The 18-tick impact burst. Its silhouette is procedural instead of trusting the primary texture's
// alpha, which can be opaque across its whole quad; this is what guarantees no square splash field.
float4 WaterBurst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateWaterUV(c);
    float2 p = (c - 0.5) * 2.0;
    float r = length(p);
    float edge = saturate((1.0 - r) * 3.1);
    float2 foamUV = c * 2.65 + float2(-Time * 0.26, Time * 0.18);
    float foam = saturate(tex2D(DetailSampler, foamUV).r * 1.48 - 0.26);
    float churn = saturate(foam * 1.55 - 0.18);
    float core = saturate(1.0 - r * 1.36) * (0.40 + foam * 0.60);
    float alpha = edge * saturate(churn * 0.86 + foam * 0.52 + core * 0.24) * Opacity;
    float3 color = lerp(MidColor, CoreColor, foam);
    color = lerp(DarkColor, color, churn);
    return float4(color * alpha, alpha) * v;
}

technique QuaraBubble { pass P { PixelShader = compile ps_2_0 Bubble(); } }
technique QuaraDroplet { pass P { PixelShader = compile ps_2_0 Droplet(); } }
technique QuaraWaterBurst { pass P { PixelShader = compile ps_2_0 WaterBurst(); } }
