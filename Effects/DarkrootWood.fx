// Darkroot Sentry living wood: thorns, roots, vines and ground/head buds.
// Geometry comes from the caller's cached gameplay path; these techniques only paint bark, moss and sap
// INSIDE that silhouette. Premultiplied AlphaBlend: every return is float4(rgb * coverage, coverage).
// s0 = T_PerlinNoise_Tiled (longitudinal grain), s1 = Voronoi_10 (bark plates / cracks). Both seamless.
#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float Time;
float Progress;      // growth / reveal 0..1 (Thorn: unused; Root: rise; Vine: tip arc fraction; Bud: growth)
float Active;        // 1 while lethal (brighter sap). Vine: repurposed as the tip's arc fraction (DarkrootVine.PreDraw).
float Opacity;       // fade, 0..1
float4 PixelGrid;    // xy = 2px block count across the quad, zw = reciprocal (computed in C#)
float2 SegmentRange; // Vine: arc fraction at this segment quad's start / end. Root: x = per-root seed 0..1
float ArcPixels;     // Vine: the whole vine's arc length in px (SegmentRange * ArcPixels = px along the vine)

static const float3 BarkColor = float3(0.29, 0.19, 0.12);
static const float3 DeepBark = float3(0.12, 0.075, 0.045);
static const float3 PaleBark = float3(0.52, 0.38, 0.24);
static const float3 MossColor = float3(0.40, 0.62, 0.25);
static const float3 SapColor = float3(0.86, 0.88, 0.72);

// Thorn: uv.x = tail (0) -> point (1), uv.y across. 32x12 quad.
// Core half-width w peaks at .5 (3px of the 6px half-height) and is 0 at both ends, so the lethal-looking body
// stays inside the 24x6 damage capsule; fray is faint and only behind the middle.
float4 Thorn(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float across = abs(uv.y - 0.5) * 2.0;
    float width = (1.0 - uv.x) * saturate(uv.x * 5.0) * 0.62;
    float grain = tex2D(PrimarySampler, float2(uv.x * 1.2 - Time * 0.6, uv.y * 3.0)).r;

    float body = saturate((width - across) * 8.0);
    float sapLine = saturate(1.0 - abs(across - width * 0.45) * 9.0) * body * grain;
    float fray = saturate((width + grain * 0.25 - across) * 4.0) * (1.0 - body) * saturate(1.0 - uv.x * 1.6) * 0.3;
    float pointGlint = saturate((uv.x - 0.78) * 5.0) * body;

    float alpha = (body + fray) * Opacity;
    float3 color = BarkColor * (0.55 + grain * 0.6) * (body + fray)
        + MossColor * sapLine * (0.35 + Active * 0.45)
        + SapColor * pointGlint * 0.35;
    return float4(color * Opacity, alpha);
}

// Root: 48x136 quad anchored at its bottom; uv.y = 1 at the floor, h = height fraction above it (1 = 136px).
// Trunk half-width tapers 12px at the floor to 0 at h = .94 (128px), matching the 24x128 tapered collider. A Perlin
// column picked by SegmentRange.x (per-root seed) both bends the spine (typically +-5px, more near the top) and swells
// or pinches the width +-1.5px into knots, so no two roots match.
// Thorns: every 27px band (h * 5) grows one barb on alternating sides, ~6px thick at the trunk, ~12px long, rising 8px
// as it goes and shrinking toward the top; decorative, outside the collider. Progress reveals bottom-to-top.
float4 Root(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float h = 1.0 - uv.y;
    float seed = SegmentRange.x;

    float knot = tex2D(PrimarySampler, float2(seed, h * 0.9)).r - 0.5;
    float x = uv.x - 0.5 - knot * 0.45 * h;
    float halfWidth = 0.25 - h * 0.266 + knot * 0.06;
    float trunk = saturate((halfWidth - abs(x)) * 60.0);

    // Barb geometry in band units (1 = 27px; 1 uv.x = 48px = 1.78 bands): out = distance past the trunk edge on this
    // band's side, rise = height into the band. Half-thickness 3px (.111) narrowing 1px per 4px out and toward the top.
    float band = h * 5.0 + seed;
    float side = step(0.5, frac(band * 0.5)) * 2.0 - 1.0;
    float outward = x * side - halfWidth;
    float rise = frac(band);
    float barbHalf = 0.111 - outward * 0.444 - h * 0.074;
    float barb = saturate((barbHalf - abs(rise - 0.185 - outward * 1.19)) * 40.0) * step(0.0, outward);

    float grain = tex2D(PrimarySampler, float2(x * 3.0 + h * 2.2, h * 0.9 - Time * 0.25)).r;
    float plates = tex2D(DetailSampler, float2(x * 1.6 + seed, h * 3.4)).r;
    float crack = step(plates, 0.22);

    float reveal = saturate((Progress - h) * 14.0);
    float body = max(trunk, barb) * reveal;
    float channel = saturate(1.0 - abs(x) * 30.0) * grain;

    float alpha = body * Opacity;
    float3 color = BarkColor * (0.45 + grain * 0.7) * (1.0 - crack * 0.6) * body
        + MossColor * channel * body * (0.35 + Active * 0.5);
    return float4(color * Opacity, alpha);
}

// Vine segment: uv.x along this segment, uv.y across a 20px quad (1 across unit = 10px). along = arc fraction of the
// whole vine; px = the same distance in pixels (ArcPixels), so bark detail keeps its size on a short or long vine.
// Cable half-width .42-.58 (4.2-5.8px, knotted by Perlin). Within the last Active (= 16px as an arc fraction) before
// the tip it swells by .3 (to ~8px, the damage radius) and closes to a point exactly at Progress.
// Surface: two strands twisting every 12px (dark grooves to pale crowns), mottled by grain and Voronoi plates, moss
// where the grain runs high, round-shaded across the width. Thorns are a separate technique (VineThorns, below).
float4 Vine(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float along = lerp(SegmentRange.x, SegmentRange.y, uv.x);
    float side = uv.y * 2.0 - 1.0;
    float across = abs(side);
    float px = along * ArcPixels;
    float toTip = (Progress - along) / max(Active, 0.001);

    float swell = saturate(1.0 - abs(toTip - 0.55) * 1.6);
    float tipClose = saturate(toTip * 3.0);
    float knot = tex2D(PrimarySampler, float2(px * 0.012, 0.37)).r;
    float halfWidth = (0.42 + knot * 0.16 + swell * 0.3) * tipClose;
    float cable = saturate((halfWidth - across) * 9.0);

    // strand = distance from the strand crown (0) to the groove (.5): diagonal bands 12px apart leaning across.
    float strand = abs(frac(px * 0.083 + side * 0.7) - 0.5);
    float plates = tex2D(DetailSampler, float2(px * 0.03, uv.y * 0.6)).r;
    float grain = tex2D(PrimarySampler, float2(px * 0.05 - Time * 0.3, uv.y * 1.5)).r;

    float tone = saturate(0.95 - strand * 1.5 + grain * 0.35) * (0.45 + plates * 0.75) * (1.0 - across * 0.45);
    float3 bark = lerp(DeepBark, PaleBark, tone) + MossColor * saturate(grain * 2.5 - 1.6);

    float3 color = bark * cable + SapColor * swell * cable * 0.35;
    return float4(color * Opacity, cable * Opacity);
}

// Vine thorns: drawn on the same quads just BEFORE Vine, so the cable covers each thorn's root (split from Vine to
// fit ps_2_0). Same width maths as Vine. Band units (1 band = 18px): one thorn per band on alternating sides, base
// half-thickness .14 band (2.5px) narrowing to a point 4px past the cable edge, leaning toward the tip. Every third
// band is a moss-green leaf instead. Decorative only (outside the tip's damage radius); none on the swollen tip.
float4 VineThorns(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float along = lerp(SegmentRange.x, SegmentRange.y, uv.x);
    float side = uv.y * 2.0 - 1.0;
    float px = along * ArcPixels;
    float toTip = (Progress - along) / max(Active, 0.001);

    float swell = saturate(1.0 - abs(toTip - 0.55) * 1.6);
    float tipClose = saturate(toTip * 3.0);
    float knot = tex2D(PrimarySampler, float2(px * 0.012, 0.37)).r;
    float halfWidth = (0.42 + knot * 0.16 + swell * 0.3) * tipClose;

    float band = px * 0.0556;
    float barbSide = frac(band * 0.5) > 0.5 ? side : -side;
    float outward = barbSide - halfWidth;
    float barb = saturate((0.14 - outward * 0.35 - abs(frac(band) - 0.3 - outward * 0.6)) * 30.0);
    barb *= saturate((outward > -0.2 ? tipClose : 0.0) - swell * 2.0);
    float leaf = frac(band * 0.334) > 0.667 ? 1.0 : 0.0;

    float3 thorn = lerp(DeepBark * 1.8, MossColor * 0.9, leaf);
    return float4(thorn * barb * Opacity, barb * Opacity);
}

// Bud: telegraph. Low-opacity knot that condenses as Progress grows; Active brightens a sap core as release nears.
// Body radius max .55 * 1.1 = .6 of the half-quad and quadFade forces 0 before the edge in every direction.
float4 Bud(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;
    float r = length(p);
    float n = tex2D(PrimarySampler, p * 0.6 + (0.5 + Time * 0.05)).r;

    float radius = 0.55 * Progress * (0.8 + n * 0.3);
    float body = saturate((radius - r) * 10.0);
    float core = saturate((0.22 * Progress - r) * 18.0) * (0.3 + Active * 0.7);
    float quadFade = saturate((1.0 - r) * 4.0);

    float alpha = saturate(body * 0.75 + core) * quadFade * Opacity;
    float3 color = (BarkColor * (1.0 - n * 0.4) + MossColor * n * 0.6) * body * 0.75 + SapColor * core;
    return float4(color * quadFade * Opacity, alpha);
}

technique DarkrootThorn
{
    pass P0 { PixelShader = compile ps_2_0 Thorn(); }
}

technique DarkrootRoot
{
    pass P0 { PixelShader = compile ps_2_0 Root(); }
}

technique DarkrootVine
{
    pass P0 { PixelShader = compile ps_2_0 Vine(); }
}

technique DarkrootVineThorns
{
    pass P0 { PixelShader = compile ps_2_0 VineThorns(); }
}

technique DarkrootBud
{
    pass P0 { PixelShader = compile ps_2_0 Bud(); }
}
