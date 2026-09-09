// ArtoriasBoomerangOrbit.fx
// Counter-rotating abyss wisps around the hostile crescent's crisp core.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;
// Solid technique only (see below) - ?.SetValue no-ops for the original technique, which doesn't
// declare it.
float4 PixelGrid;

float2 RotateUV(float2 p, float angle)
{
    float s = sin(angle);
    float c = cos(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

float4 BoomerangOrbitPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 p = coords - 0.5;
    float radius = length(p);
    float spin = Time * (0.82 + Active * 0.24) * Direction;
    float2 uvA = RotateUV(p, spin) + 0.5;
    float2 uvB = RotateUV(p, -spin * 0.63) + 0.5;

    float spiralA = tex2D(PrimarySampler, uvA).r;
    float spiralB = tex2D(PrimarySampler, uvB * 1.23 - 0.115).r;
    float noise = tex2D(DetailSampler,
        coords * 3.2 + float2(Time * 0.09 * Direction, -Time * 0.12)).r;

    float outer = saturate((0.51 - radius) * 13.0);
    float hollow = saturate((radius - 0.10) * 13.0);
    float brokenSpiral = saturate(spiralA * 0.70 + spiralB * 0.45 + noise * 0.35 - 0.43);
    float wisps = brokenSpiral * outer * hollow;
    float hot = saturate((wisps - 0.42) * 2.25);

    float3 returnColor = lerp(MidColor, float3(0.92, 0.18, 0.72), Active * 0.72);
    float3 color = lerp(DarkColor, returnColor, wisps);
    color = lerp(color, CoreColor, hot * 0.58);
    return float4(vertexColor.rgb * color * (wisps * 0.90 + hot * 0.70),
        vertexColor.a * wisps * Opacity);
}

technique ArtoriasBoomerangOrbit
{
    pass OrbitPass { PixelShader = compile ps_2_0 BoomerangOrbitPixel(); }
}

// Solid underlay: drawn FIRST with premultiplied alpha, with the crisp additive original above
// layered on top of it (ArtoriasVFX.DrawBoomerangOrbit does both passes) - "too translucent" fixed
// by giving the swirl real occlusion, then the original crisp spiral keeps the recognizable arm
// shape on top. Uses a tangent-slide swirl (vfx-shader-tips §50) instead of RotateUV's two full
// sin/cos rotations: the original technique above is ALREADY 75/64 arithmetic slots on its own
// (pre-existing, unrelated to this change), so there was never room to add PixelateShaderUV to a
// second copy of that same math.
float4 BoomerangOrbitSolidPixel(float4 vertexColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 pixelCoords = PixelateShaderUV(coords, PixelGrid);
    float2 p = pixelCoords - 0.5;
    float radius = length(p);
    float2 dir = p / max(radius, 0.0005);
    float2 perp = float2(-dir.y, dir.x);
    float spin = Time * (0.82 + Active * 0.24) * Direction;
    float2 uvA = pixelCoords + perp * (spin * 0.16);
    float2 uvB = pixelCoords * 1.23 - 0.115 + perp * (-spin * 0.63 * 0.16);

    float spiralA = tex2D(PrimarySampler, uvA).r;
    float spiralB = tex2D(PrimarySampler, uvB).r;
    float noise = tex2D(DetailSampler,
        pixelCoords * 3.2 + float2(Time * 0.09 * Direction, -Time * 0.12)).r;

    float outer = saturate((0.51 - radius) * 13.0);
    float hollow = saturate((radius - 0.10) * 13.0);
    // Original -0.43 threshold read as thin/pale (Option A). -0.28 was tried and let the two
    // counter-spiraling arms merge into one blob. -0.36 keeps them distinguishable while still
    // being far more solid than the original.
    float brokenSpiral = saturate(spiralA * 0.70 + spiralB * 0.45 + noise * 0.35 - 0.36);
    float wisps = brokenSpiral * outer * hollow;
    float hot = saturate((wisps - 0.34) * 2.25);

    float3 returnColor = lerp(MidColor, float3(0.92, 0.18, 0.72), Active * 0.72);
    float3 color = lerp(DarkColor, returnColor, wisps);
    color = lerp(color, CoreColor, hot * 0.58);
    float alpha = saturate(wisps * 1.3) * Opacity;
    return float4(vertexColor.rgb * color * (0.85 + hot * 0.55) * alpha, vertexColor.a * alpha);
}

technique ArtoriasBoomerangOrbitSolid
{
    pass OrbitSolidPass { PixelShader = compile ps_2_0 BoomerangOrbitSolidPixel(); }
}
