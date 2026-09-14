// ArtoriasAbyssTendrilReach.fx
// 2x2-pixel version of Artorias's Tendril Reach shader family. The original
// ArtoriasAbyssTendril effect remains untouched as the smooth backup and for its
// existing impale consumers. Designed for Reach / ps_2_0.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float Active;
float2 DrawSize;
float2 PrimaryTextureSize;
float4 PixelGrid;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 TendrilReachShadowPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    float taper = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float noise = tex2D(DetailSampler,
        uv * float2(3.2, 4.8) + float2(-Time * 0.09, Time * 0.045)).r;
    float fog = tex2D(PrimarySampler,
        uv * float2(1.7, 2.3) + float2(Time * 0.025, -Time * 0.038)).r;
    float wave = sin(uv.x * 15.0 + Time * 4.8) * 0.095 * taper;
    float distanceFromBody = abs(uv.y - 0.5 - wave);
    float width = 0.105 + noise * 0.075 + Active * 0.025;
    float body = saturate((width - distanceFromBody) * 9.5) * taper;
    float torn = body * saturate(fog * 0.72 + noise * 0.48 - 0.22);
    float3 color = lerp(DarkColor, MidColor, torn * 0.38);
    float alpha = vertexColor.a * saturate(body * 0.62 + torn * 0.36) * Opacity;
    return float4(vertexColor.rgb * color * alpha, alpha);
}

float4 TendrilReachCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 6.0);
    float noise = tex2D(DetailSampler,
        uv * float2(4.1, 5.3) + float2(Time * 0.12, -Time * 0.08)).r;
    // The smooth shader's second strand and sin wobble are deliberately omitted. Pixel
    // quantization supplies that breakup and keeps this pass below the ps_2_0 slot ceiling.
    float strand = saturate((0.040 - abs(uv.y - 0.5)) * 25.0) * taper;
    float pulsePos = frac(Time * (0.58 + Active * 0.24));
    float pulse = saturate((0.11 - abs(uv.x - pulsePos)) * 9.1) * strand;
    float breakup = strand * saturate(0.48 + noise * 0.72);
    float3 color = lerp(DarkColor, MidColor, breakup);
    color = lerp(color, CoreColor, pulse * 0.88);
    return float4(vertexColor.rgb * color * (breakup * 0.82 + pulse * 1.38),
        vertexColor.a * saturate(breakup + pulse) * Opacity);
}

float4 TendrilReachTipPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    float2 p = uv - 0.5;
    float noise = tex2D(DetailSampler,
        uv * 4.2 + float2(-Time * 0.10, Time * 0.075)).r;
    float lengthMask = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float center = saturate((0.055 - abs(p.y)) * 18.2);
    float upper = saturate((0.045 - abs(p.y - 0.17)) * 22.2);
    float lower = saturate((0.045 - abs(p.y + 0.17)) * 22.2);
    float prongs = saturate(center + upper + lower) * lengthMask;
    float2 knotPoint = p * float2(1.35, 1.0);
    float knot = saturate((0.0225 - dot(knotPoint, knotPoint)) * 44.4);
    float body = saturate(prongs * (0.62 + noise * 0.62) + knot);
    float core = saturate(center * lengthMask + knot * 0.55) * Active;
    // The caller already selects DangerMagenta for a hostile tip and AbyssViolet otherwise,
    // so repeating that state lerp here only spends scarce ps_2_0 arithmetic slots.
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, core * 0.72);
    return float4(vertexColor.rgb * color * (body * 0.86 + core * 1.15),
        vertexColor.a * body * Opacity);
}

technique ArtoriasTendrilReachShadow
{
    pass ShadowPass { PixelShader = compile ps_2_0 TendrilReachShadowPixel(); }
}

technique ArtoriasTendrilReachCore
{
    pass CorePass { PixelShader = compile ps_2_0 TendrilReachCorePixel(); }
}

technique ArtoriasTendrilReachTip
{
    pass TipPass { PixelShader = compile ps_2_0 TendrilReachTipPixel(); }
}
