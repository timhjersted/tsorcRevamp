// Gigas Solar Slabs detonation: a multi-layered explosive golden-light burst.
// Keeps Effects/GigasNovaRing.fx untouched for Wrath of Gold's slower molten solar fire.
// Four lightweight ps_2_0 techniques stack in GigasLightHand:
//   1. GigasSolarRupture    (AlphaBlend, premultiplied) — staggered billowing solar plasma clouds
//   2. GigasSolarShockwave  (AlphaBlend, premultiplied) — crisp expanding golden concussion rings
//   3. GigasSolarFlareBurst (Additive, SourceAlpha/One) — layered centered Flare_616 & 666 ray pop
//   4. GigasSolarCoreFlash  (AlphaBlend, premultiplied) — high-contrast incandescent sun heart
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
float4 PixelGrid;

// ---------------------------------------------------------------------------------------------
// 1. GigasSolarRupture — one explosive billowing golden plasma pulse (drawn 3x staggered).
// Progress = caller-eased 0..1 expansion (pop curve computed in C# so ps_2_0 saves uniform slots);
// Active = 1 / scale (>= 0.90); Direction = per-pulse phase seed.
// ---------------------------------------------------------------------------------------------
float4 SolarRupturePixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = (coords - 0.5) * Active;
    float r = length(p) * 2.0;

    // Contracting UV scale makes turbulent cells visibly rush outward from the clap seam.
    float2 flowPoint = p * (1.16 - Progress * 0.42);
    float n1 = tex2D(PrimarySampler, flowPoint * 1.45 + float2(Direction * 0.37 - Time * 0.14, Time * 0.09) + 0.5).r;
    float n2 = tex2D(DetailSampler, flowPoint * 2.25 + float2(Direction * 1.73 + Time * 0.22, -Time * 0.24) + 0.5).r;
    float shape = saturate(n1 * 0.76 + n2 * 0.48 - 0.24);

    // Proof of quad cutoff: blastRadius maxes at 0.10 + 0.66 = 0.76, so raggedRadius maxes at
    // 0.76 * (0.72 + 0.30) = 0.775 in r (= 0.388 in scaled len). Even at Active = 0.90, the outer
    // edge of both body and shell dies before raw len = 0.47 (< 0.50), independent of noise.
    float blastRadius = 0.10 + Progress * 0.66;
    float raggedRadius = blastRadius * (0.72 + shape * 0.30);
    float body = saturate((raggedRadius - r) * 4.8);

    // Organic noise-torn caldera hollowing as the pulse matures: reads as an exploding solar wave
    // tearing apart into bright golden flame clouds rather than a uniform geometric donut.
    float caldera = saturate((r + shape * 0.24) * 3.6 + 1.23 - Progress * 3.10);
    body *= caldera;

    float shell = saturate(1.0 - abs(r - raggedRadius + (n2 - 0.5) * 0.08) * 8.5);
    float veins = saturate(n2 * 1.95 - 0.42) * body;
    float embers = saturate(n2 * 2.55 - 1.55) * shell;

    float3 rgb = DarkColor * body
               + MidColor * (veins + shell * 0.85)
               + CoreColor * (embers + shell * 0.50);
    float alpha = saturate(body * 0.84 + shell * 0.66) * Opacity;
    return float4(rgb * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// 2. GigasSolarShockwave — crisp torn golden concussion ring with trailing solar wake.
// Active = ring radius in half-quad UV units (<= 0.74); Direction = reciprocal UV half-thickness.
// Caller supplies >= 4.5x half-thickness outer padding so body/core provably hit 0 before len = 0.5.
// ---------------------------------------------------------------------------------------------
float4 SolarShockwavePixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = coords - 0.5;
    float len = length(p);

    // Planar UV sampling avoids the center-pinch spoke artifact that ring-space stamps into wakes.
    float n1 = tex2D(PrimarySampler, p * 1.55 + float2(-Time * 0.22, Time * 0.14) + 0.5).r;
    float n2 = tex2D(DetailSampler, p * 2.65 + float2(Time * 0.18, -Time * 0.24) + 0.5).r;
    float grain = saturate(n1 * 0.84 + n2 * 0.66 - 0.30);

    // Proof of quad cutoff: body is 0 for |dist| > 1.05, core for |dist| > 0.32, and wake extends
    // strictly inward (dist < 0). With 4.5x caller padding, max grain displacement (0.375) leaves
    // > 3.0 half-thicknesses of clear margin inside all four quad edges.
    float dist = (len - Active * 0.5) * Direction - (grain - 0.5) * 0.75;
    float core = saturate(1.0 - abs(dist) * 3.1);
    float body = saturate(1.0 - abs(dist) * 0.92);
    float wake = saturate(-dist * 0.38) * saturate(1.0 + dist * 0.24) * saturate(grain * 1.48 - 0.20);
    float spark = saturate(n2 * 2.60 - 1.58) * (body + wake * 0.75);

    float3 rgb = DarkColor * (body * 0.58 + wake * 0.54)
               + MidColor * (body * 0.98 + wake * 0.92)
               + CoreColor * (core * 1.25 + spark * 0.92);
    float alpha = saturate(body * 0.86 + wake * 0.50 + core * 0.68) * Opacity;
    return float4(rgb * Opacity, alpha);
}

// ---------------------------------------------------------------------------------------------
// 3. GigasSolarFlareBurst — heavenly light starburst pop (T_VFX_Flare_616 / 666 _Centered).
// Drawn with BlendState.Additive (SourceAlpha / One -> dst + rgb * a), so rgb is NOT premultiplied.
// Active = UV scale (> 1 zooms in tighter, 1 = full flare); Direction = shimmer phase seed.
// ---------------------------------------------------------------------------------------------
float4 SolarFlareBurstPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = coords - 0.5;
    float len = length(p);
    float2 dir = p / max(len, 0.0005);

    float2 flareUV = p * Active + 0.5;
    float flare = tex2D(PrimarySampler, flareUV).r;
    float shimmer = tex2D(DetailSampler, dir * (0.40 + len * 0.32) + float2(Direction * 0.31 - Time * 0.45, Time * 0.32)).r;

    // Proof of quad cutoff: edgeMask hits 0 at len = 0.48 (< 0.50), squared for soft approach.
    float edgeMask = saturate((0.48 - len) * 10.0);
    edgeMask = edgeMask * edgeMask;

    float rays = flare * (0.74 + shimmer * 0.66) * edgeMask;
    float hotCore = flare * flare * edgeMask;

    float3 rgb = lerp(DarkColor, MidColor, saturate(rays * 1.35));
    rgb = lerp(rgb, CoreColor, saturate(hotCore * 1.40));
    float alpha = saturate(rays * 0.95 + hotCore * 0.52) * Opacity;
    return float4(rgb, alpha);
}

// ---------------------------------------------------------------------------------------------
// 4. GigasSolarCoreFlash — high-contrast incandescent sun heart framed by a golden occlusion halo.
// Drawn premultiplied over AlphaBlend so the core pops sharply even against bright daytime sky.
// ---------------------------------------------------------------------------------------------
float4 SolarCoreFlashPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = coords - 0.5;
    float len = length(p);
    float r = len * 2.0;

    float star = tex2D(PrimarySampler, coords).r;
    float churn = tex2D(DetailSampler, p * 2.2 + float2(-Time * 0.25, Time * 0.19) + 0.5).r;

    // Proof of quad cutoff: quadFade hits 0 at len = 0.48 (< 0.50), squared for smooth falloff.
    float quadFade = saturate((0.48 - len) * 12.0);
    quadFade = quadFade * quadFade;

    float coreRadius = 0.34 - Progress * 0.16;
    float orb = saturate((coreRadius - r + (churn - 0.5) * 0.08) * 6.5) * quadFade;
    float halo = saturate((0.76 - r + (churn - 0.5) * 0.14) * 2.6) * quadFade;
    float crown = saturate(star * 1.35) * quadFade;

    float3 rgb = DarkColor * (halo * 0.58)
               + MidColor * (halo * 0.78 + crown * 0.90)
               + CoreColor * (orb * 1.38 + crown * crown * 0.80);
    float alpha = saturate(halo * 0.76 + orb * 0.96 + crown * 0.58) * Opacity;
    return float4(rgb * Opacity, alpha);
}

technique GigasSolarRupture
{
    pass GigasSolarRupturePass { PixelShader = compile ps_2_0 SolarRupturePixel(); }
}

technique GigasSolarShockwave
{
    pass GigasSolarShockwavePass { PixelShader = compile ps_2_0 SolarShockwavePixel(); }
}

technique GigasSolarFlareBurst
{
    pass GigasSolarFlareBurstPass { PixelShader = compile ps_2_0 SolarFlareBurstPixel(); }
}

technique GigasSolarCoreFlash
{
    pass GigasSolarCoreFlashPass { PixelShader = compile ps_2_0 SolarCoreFlashPixel(); }
}
