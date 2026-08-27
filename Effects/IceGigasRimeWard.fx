// Rime Ward counter-stance: a thick, jagged shell of ice wrapping the giant's whole body.
//
// Kept all-around rather than a frontal half-shield because the ward's detonation launches shards
// in a full 360 burst (IceGigas.RunRimeWard) — a front-only plate would visibly contradict shards
// leaving its back.
//
// VoronoiNoise is used as GEOMETRY, not as a brightness modulator. It is flat-shaded polygons with
// hard cell steps, so sampling it around a small ring circle gives each angular sector its own
// flat radius: straight chords meeting at sharp corners, i.e. broken plate ice. Smoothly blending
// it into a density term (the earlier attempt) destroys exactly that property and reads as noise.
#include "PixelShaderCommon.fxh"
sampler CellNoise : register(s0);
sampler FacetNoise : register(s1);
float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float2 DrawSize;
// Ellipse semi-axes in pixels, matching where RunRimeWard already spawns its perimeter glint dust
// (NPC.Center + dir * (38, 62)), widened slightly so the shell sits just outside them.
float2 ShellAxis;
// Pure-uniform combinations, ALL folded in C#. ps_2_0 has no preshader, so any expression that
// depends only on uniforms is still emitted per pixel — including innocuous-looking scroll offsets
// like (0.5 + Time * 0.03). Folding the three pan offsets and the Rise bias was worth ~6 slots.
//   StoredAlphaGain = 0.78 + Stored * 0.16      (Stored = WardStored / 8)
//   StoredCoreGain  = 0.55 + Stored * 0.45
//   GlintGain       = 0.6  + Stored * 1.4       — a loaded ward glitters harder
//   EdgePan         = 0.5  + Time * 0.008
//   FillPan         = 0.5  + Time * 0.030
//   CrackPan        =        Time * 0.105
//   RiseOffset      = Rise - 1.0                (Rise: 0 -> 1, shell crystallises feet-upward)
float StoredAlphaGain;
float StoredCoreGain;
float GlintGain;
float EdgePan;
float FillPan;
float CrackPan;
float RiseOffset;
float4 PixelGrid;

float4 IceGigasRimeWardPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float2 p = (coords - 0.5) * DrawSize;
    float2 norm = p / ShellAxis;
    float radius = length(norm);

    // Jagged boundary: the cell steps become sharp per-angle radius jumps. Sampled RAW rather than
    // blended with a second field, so the hard steps survive as chipped plate edges.
    //
    // Sampled in plain elliptical space rather than ring space. Ring space would need `dir`, and
    // the normalize plus ring UV measured 10 slots — a third of this shader's budget — purely to
    // make the perturbation perfectly constant along each ray. At 0.45 the sample crosses ~10
    // cells per lap of the unit ellipse, which is the chunky plate count wanted, and because the
    // shell band is thin the radial drift across it is small enough that the chords still read as
    // straight. Cheaper and visually equivalent here.
    float edgeCell = tex2D(CellNoise, norm * 0.45 + EdgePan).r;
    float jagged = edgeCell - 0.5;
    float localOuter = 1.0 + jagged * 0.16;

    // Facet fill in ordinary elliptical space (norm, NOT dir) — this is a filled mass, and
    // ring-space sampling on a body smears every feature radially into a starburst of spokes.
    // The crack layer rides a scaled copy of the fill UV: an independent per-axis expression cost
    // ~7 slots, and at 2x the frequency on a different texture the two still never share a phase.
    float2 fillUV = norm * 1.30 + FillPan;
    float facet = tex2D(FacetNoise, fillUV).r;
    float crack = tex2D(CellNoise, fillUV * 2.0 - CrackPan).r;
    float shimmer = saturate(facet * 0.58 + crack * 0.50 - 0.10);
    // Thin bright fracture lines traced off the crack field's own web, rather than just using it
    // to dim the fill — this is what sells "faceted slab" over "textured grey blob". Single
    // abs-based band-pass rather than two opposing saturates: same peak and width, ~3 slots less.
    float fractureLine = saturate(1.0 - abs(crack - 0.54) * 12.5);

    // Filled band from 58% of the radius out to the jagged boundary: thick armour plating, not a
    // hairline outline. The inner cut jitters with the same noise so it never reads as two
    // concentric perfect circles. `fromEdge` is shared with the rim below — both are the same
    // distance field, and computing it twice is a boundary nobody can see.
    float innerCut = 0.58 + jagged * 0.08;
    float fromEdge = localOuter - radius;
    // `towardEdge` climbs from the inner cut outward. The first draft also carried a separate
    // analytic rimGlow band, but that was a second shape built from the SAME distance field for a
    // boundary nobody can distinguish — 8 slots for nothing. Squaring this term concentrates it at
    // the outer lip and gives the identical bright-edge read for free.
    float towardEdge = saturate((radius - innerCut) * 6.0);
    float shellFill = saturate(fromEdge * 7.0) * towardEdge * (0.55 + shimmer * 0.45);

    // coords.y runs 0 at the top of the ellipse box to 1 at the feet, so this grows the shell
    // upward from the feet as Rise climbs.
    float formed = saturate((RiseOffset + coords.y) * 9.0);
    shellFill *= formed;

    // The fracture web doubles as the ward's charge readout. An earlier draft thresholded `facet`
    // a second time for a separate glint layer; scaling this already-computed term by GlintGain
    // (which carries Stored) gives the same "a loaded ward glitters harder" tell for ~5 slots less.
    float fracture = fractureLine * shellFill;
    float lip = towardEdge * towardEdge * shellFill;

    // Accumulated tiers rather than chained lerps: under premultiplied alpha the sum IS the
    // premultiplied colour, so no separate multiply by alpha is needed on the return.
    float alpha = saturate(shellFill * StoredAlphaGain + fracture * 0.30) * Opacity;
    float3 color = OuterColor * (shellFill * 0.34)
        + MiddleColor * (shellFill * 0.78)
        + CoreColor * ((lip * 0.42 + fracture * GlintGain * 0.30) * StoredCoreGain);
    return float4(color * Opacity, alpha);
}

technique IceGigasRimeWard
{
    pass IceGigasRimeWardPass { PixelShader = compile ps_2_0 IceGigasRimeWardPixel(); }
}
