// Winter's Grasp: a disc of floor that floods with mist and then CRYSTALLISES into a sheet of
// broken plate ice. Fixed radius (GigasFrostZone.ZoneRadius) — it does not expand; `Active` flips
// the read from telegraph to crystallised on the same tick the real hitbox turns on.
//
// VoronoiNoise is used as GEOMETRY, not as a brightness modulator. It is flat-shaded polygons with
// hard cell steps, so:
//   * sampled around a small ring circle, the radius is piecewise CONSTANT per cell — straight
//     chords meeting at sharp corners, i.e. a broken plate rim rather than a wobbly noise curve;
//   * sampled in Cartesian across the disc, each cell is a flat brightness PLATEAU (one ice slab),
//     and a second tap a fraction of a cell away detects the cell WALLS, which become the bright
//     fracture web between slabs.
// Smoothly blending the cells into a density term destroys exactly the hard steps that make ice
// read as ice — that draft rendered as "noise, not ice chunks".
// T_Noise_Wo14 (genuinely faceted crystal) then textures the interior of each slab.
#include "PixelShaderCommon.fxh"
sampler CellNoise : register(s0);
sampler FacetNoise : register(s1);
float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float2 DrawSize;
float Radius;
// 0 -> 1 across the telegraph, then Active flips 0 -> 1 at the crystallise tick.
float Progress;
float Active;
// Pure-uniform combinations folded in C# — ps_2_0 has no preshader, so even a scroll offset like
// (0.5 + Time * 0.02) is re-evaluated per pixel.
//   EdgePan     = (0.5 + Time * 0.004, 0.5 - Time * 0.003)
//   ShapePan    = (0.5 + Time * 0.010, 0.5 - Time * 0.008)
//   FacetPan    = (0.5 - Time * 0.014, 0.5 + Time * 0.011)
//   RimGain     = 0.50 + Active * 0.50   (telegraph rim is half strength)
//   RimCoreGain = 0.40 + Active * 0.40
//   CellScale   = 1.2 / quadSize          FacetScale = 3.4 / quadSize
// (dividing p by DrawSize.x per pixel is a divide by a pure uniform — pre-divide it in C#)
float2 EdgePan;
float2 ShapePan;
float2 FacetPan;
float RimGain;
float RimCoreGain;
float CellScale;
float FacetScale;
float4 PixelGrid;

float4 IceGigasWintersGraspPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = (coords - 0.5) * DrawSize;
    float radius = length(p);
    float2 dir = p / max(radius, 0.0005);

    // CHUNKED BOUNDARY. dir * 0.16 walks a circle of circumference ~1.0 texture tile, so a full
    // lap crosses ~11 Voronoi cells => ~11 plate-sized chunks around the rim. Used RAW so the cell
    // steps survive as sharp radius jumps. Swing 0.86..1.14 keeps the mean radius honest; the peak
    // reach is 1.14 * Radius and the rim adds at most 16 more, which the caller's quad padding
    // covers with room to feather.
    float edgeCell = tex2D(CellNoise, dir * 0.16 + EdgePan).r;
    float localRadius = Radius * (0.86 + edgeCell * 0.28);

    // FLAT SLAB INTERIOR. Cartesian, never ring space — this is a filled body, and ring-space
    // sampling on a body smears every feature radially into a starburst of spokes. Scale 1.2
    // across the quad puts ~11 cells across the disc, i.e. ~29px slabs at gameplay scale.
    float2 cellUV = p * CellScale + ShapePan;
    float cell = tex2D(CellNoise, cellUV).r;
    // Second tap ~1/8 of a cell away: equal inside a slab, different across a boundary, so the
    // difference isolates the cell walls. This is the fracture web between chunks. The offset is a
    // fixed, slightly anisotropic literal (not a time pan) so the two taps stay a constant
    // distance apart — panning it would make the web thickness pulse.
    float fracture = saturate(abs(cell - tex2D(CellNoise, cellUV + float2(0.011, 0.009)).r) * 9.0);
    // Angular crystal detail inside each slab, so a chunk is not dead flat colour.
    float facet = tex2D(FacetNoise, p * FacetScale + FacetPan).r;
    float slabTone = 0.30 + cell * 0.70;

    // The filled sheet: near-uniform coverage inside, with the slab variation carried by colour
    // rather than by punching holes in it. Provably zero for radius >= localRadius.
    float sheet = saturate((localRadius - radius) * 0.42);
    // Rim width varies with the cell so the lip reads as chipped facets, not a painted band.
    // Feathered by MULTIPLY, not by dividing through max(rimWidth * 0.6, 1.0) — that divide plus
    // its max cost 16 arithmetic slots, a quarter of the entire Reach budget, for a feather the
    // eye cannot distinguish from this fixed 0.15 scale. It also degrades gracefully to zero
    // instead of leaving a hairline where the denominator bottoms out.
    float rimWidth = 7.0 + edgeCell * 9.0;
    float rim = saturate((rimWidth - abs(radius - localRadius)) * 0.15);

    // Telegraph: mist swirling inward with the slab structure only hinted; crystallised: the full
    // sheet plus a bright rim. Active is the same tick GigasFrostZone.Crystallizing turns on, so
    // the visual promise and the hitbox agree exactly.
    // Telegraph mist off the crystal-detail layer alone. Mixing the slab cells in as well cost 2
    // slots and was arguably wrong anyway: the plate structure should not be legible before the
    // sheet has actually formed.
    float mistSwirl = saturate(facet - 0.30) * Progress;
    float telegraphBody = sheet * 0.26 * mistSwirl;
    float crystalBody = sheet * (0.52 + slabTone * 0.34 + facet * 0.16);
    float finalBody = lerp(telegraphBody, crystalBody, Active);
    float finalRim = rim * RimGain;
    float finalFracture = fracture * sheet * Active;

    // Accumulated tiers rather than chained lerps: under premultiplied alpha the sum IS the
    // premultiplied colour, so the return needs no separate multiply by alpha.
    float alpha = saturate(finalBody * 0.85 + finalRim * 0.55 + finalFracture * 0.45) * Opacity;
    float3 color = OuterColor * (finalBody * 0.34)
        + MiddleColor * (finalBody * (0.46 + slabTone * 0.38))
        + CoreColor * (finalRim * RimCoreGain + finalFracture * 0.85);
    return float4(color * Opacity, alpha);
}

technique IceGigasWintersGrasp
{
    pass IceGigasWintersGraspPass { PixelShader = compile ps_2_0 IceGigasWintersGraspPixel(); }
}
