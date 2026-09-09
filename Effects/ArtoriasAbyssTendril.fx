// ArtoriasAbyssTendril.fx
// Dark corpse-smoke sheath, independently moving violet/white filaments, and
// a directional three-pronged grab tip. Designed for Reach / ps_2_0.
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
float Direction;
float2 DrawSize;
float2 PrimaryTextureSize;
// Only consumed by the *Pixelated techniques below (ArtoriasVFX.Draw sets it unconditionally via
// ?.SetValue, so the plain techniques - still used by the tendril-hand grab reach elsewhere -
// simply ignore it and render exactly as before).
float4 PixelGrid;

float2 LocalUV(float2 coords)
{
    return coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0));
}

float4 TendrilShadowPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
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

// Same shadow sheath, quantized into chunky blocks (matches the sweep/thrust's pixel-art look) -
// for Artorias's PierceStabHold impale burst (ArtoriasVFX.DrawImpaleTendrils), which reads too soft
// standing still for seconds next to that family. The tendril-hand grab reach keeps the smooth
// TendrilShadowPixel above; see vfx-shader-tips §35 (give a second consumer its own technique).
// Duplicated rather than shared through a common helper: routing both through one function with
// parameters pushed this shader from ~60 to 69/64 arithmetic slots at ps_2_0 - the compiler does
// not optimize across the call boundary as well as it does one flat entry point. Keep them
// textually identical except for the `uv` line if either is ever tuned.
float4 TendrilShadowPixelPixelated(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
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

float4 TendrilCorePixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 6.0);
    float noise = tex2D(DetailSampler,
        uv * float2(4.1, 5.3) + float2(Time * 0.12, -Time * 0.08)).r;
    float waveA = sin(uv.x * 20.0 + Time * 7.2) * 0.085 * taper;
    float waveB = (-waveA * 0.72 + (noise - 0.5) * 0.055) * taper;
    float strandA = saturate((0.040 - abs(uv.y - 0.5 - waveA)) * 25.0);
    float strandB = saturate((0.031 - abs(uv.y - 0.5 - waveB)) * 32.0);
    float strands = saturate(strandA + strandB * 0.78) * taper;
    float pulsePos = frac(Time * (0.58 + Active * 0.24));
    float pulse = saturate((0.11 - abs(uv.x - pulsePos)) * 9.1) * strands;
    float breakup = strands * saturate(0.48 + noise * 0.72);
    float3 color = lerp(DarkColor, MidColor, breakup);
    color = lerp(color, CoreColor, pulse * 0.88);
    return float4(vertexColor.rgb * color * (breakup * 0.82 + pulse * 1.38),
        vertexColor.a * saturate(breakup + pulse) * Opacity);
}

// See TendrilShadowPixelPixelated above - same deal, for the bright core strands. The second
// offset strand (waveB/strandB) is dropped here rather than shared through a helper (see that
// comment for why): with PixelateShaderUV added, this technique's ps_2_0 arithmetic budget was
// 65-74/64 depending on compiler (both over) - a decorative second strand is the right thing to
// cut, not the shape math, since the pixel quantization already carries a lot of the visual
// interest that strandB was adding.
float4 TendrilCorePixelPixelated(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(LocalUV(coords), PixelGrid);
    float taper = saturate(uv.x * 6.0) * saturate((1.0 - uv.x) * 6.0);
    float noise = tex2D(DetailSampler,
        uv * float2(4.1, 5.3) + float2(Time * 0.12, -Time * 0.08)).r;
    // No sin()-driven wave here (unlike the plain technique above): ps_2_0 expands sin() into a
    // multi-instruction polynomial, and the blocky quantization already breaks up a straight
    // centerline enough that the wobble wasn't buying its cost back.
    float strandA = saturate((0.040 - abs(uv.y - 0.5)) * 25.0);
    float strands = strandA * taper;
    float pulsePos = frac(Time * (0.58 + Active * 0.24));
    float pulse = saturate((0.11 - abs(uv.x - pulsePos)) * 9.1) * strands;
    float breakup = strands * saturate(0.48 + noise * 0.72);
    float3 color = lerp(DarkColor, MidColor, breakup);
    color = lerp(color, CoreColor, pulse * 0.88);
    return float4(vertexColor.rgb * color * (breakup * 0.82 + pulse * 1.38),
        vertexColor.a * saturate(breakup + pulse) * Opacity);
}

float4 TendrilTipPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = LocalUV(coords);
    float2 p = uv - 0.5;
    float noise = tex2D(DetailSampler,
        uv * 4.2 + float2(-Time * 0.10, Time * 0.075)).r;
    float lengthMask = saturate(uv.x * 5.0) * saturate((1.0 - uv.x) * 5.0);
    float center = saturate((0.055 - abs(p.y)) * 18.2);
    float upper = saturate((0.045 - abs(p.y - 0.17)) * 22.2);
    float lower = saturate((0.045 - abs(p.y + 0.17)) * 22.2);
    float prongs = saturate(center + upper + lower) * lengthMask;
    float knot = saturate((0.15 - length(p * float2(1.35, 1.0))) * 6.7);
    float body = saturate(prongs * (0.62 + noise * 0.62) + knot);
    float core = saturate(center * lengthMask + knot * 0.55) * Active;
    float3 stateColor = lerp(MidColor, float3(0.86, 0.18, 0.68), Active * 0.58);
    float3 color = lerp(DarkColor, stateColor, body);
    color = lerp(color, CoreColor, core * 0.72);
    return float4(vertexColor.rgb * color * (body * 0.86 + core * 1.15),
        vertexColor.a * body * Opacity);
}

technique ArtoriasTendrilShadow
{
    pass ShadowPass { PixelShader = compile ps_2_0 TendrilShadowPixel(); }
}

technique ArtoriasTendrilCore
{
    pass CorePass { PixelShader = compile ps_2_0 TendrilCorePixel(); }
}

technique ArtoriasTendrilTip
{
    pass TipPass { PixelShader = compile ps_2_0 TendrilTipPixel(); }
}

technique ArtoriasTendrilShadowPixelated
{
    pass ShadowPixelatedPass { PixelShader = compile ps_2_0 TendrilShadowPixelPixelated(); }
}

technique ArtoriasTendrilCorePixelated
{
    pass CorePixelatedPass { PixelShader = compile ps_2_0 TendrilCorePixelPixelated(); }
}
