// Ice Gigas frost breath: the blown-mist jet the GigasFrostBreathPuff projectiles fly along.
// The caller rotates this quad to the CURRENT sweep angle each tick, so uv.x = 0 sits at the
// mouth and uv.y = 0.5 is the jet centreline.
//
// Deliberately turbulence-driven rather than crystalline: this is vapour, which keeps it reading
// as a different material from the faceted ice of the ring / ward / zone shaders.
#include "PixelShaderCommon.fxh"
sampler MistNoise : register(s0);
sampler ChopNoise : register(s1);
float3 OuterColor;
float3 MiddleColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
// Pre-combined on the C# side. `Active` (telegraph vs committed exhale) only ever appeared inside
// pure-uniform expressions, and ps_2_0 has no preshader — it re-evaluates those for EVERY pixel,
// so folding them into finished uniforms is a straight saving with no visual change.
//   CoreMix       = 0.30 + Active * 0.42
//   BodyAlphaGain = 0.82 + Active * 0.18
//   EmissiveGain  = Active * Opacity * 0.09
//   AlphaScale    = Progress * Opacity
float CoreMix;
float BodyAlphaGain;
float EmissiveGain;
float AlphaScale;
float4 PixelGrid;

float4 IceGigasFrostBreathPixel(float2 coords : TEXCOORD0) : COLOR0
{
    coords = PixelateShaderUV(coords, PixelGrid);
    float along = coords.x;

    // Low frequency + fast along-scroll = large billows visibly travelling away from the mouth,
    // instead of fine fuzz sitting still on a static wedge.
    //
    // The chop layer rides a scaled copy of the mist UV plus a scalar offset rather than building
    // its own per-axis expression — a second independent float2 uv cost 9 slots here. It still
    // samples a different texture at 2.4x the frequency and scroll rate, so the two layers never
    // share a phase and no repeating scroll line appears.
    float2 mistUV = float2(along * 1.15 - Time * 1.45, coords.y * 1.70 + Time * 0.18);
    float lobe = tex2D(MistNoise, mistUV).r;
    float chop = tex2D(ChopNoise, mistUV * 2.4 + Time * 0.12).r;
    float billow = saturate(lobe * 0.66 + chop * 0.52 - 0.12);

    // The jet meanders rather than firing perfectly straight. The wander scales with `along`, so
    // it stays anchored at the lips and only gets loose further out.
    float drift = (lobe - 0.5) * 0.15 * along;
    float across = abs(coords.y - 0.5 - drift);

    // Widening wedge whose width the noise owns ~43% of, so the silhouette bulges and pinches
    // into travelling lobes.
    //
    // The bound is deliberate rather than guarded: spread maxes at 0.34, so halfWidth maxes at
    // 0.34 * 1.20 = 0.408, and |drift| at 0.075 — the jet provably dies 0.483 from the centreline,
    // inside the 0.5 quad edge, for ANY noise value. That makes a separate cutoff term
    // unnecessary (worth 4 slots) and is strictly safer than relying on one. The caller draws a
    // correspondingly TALLER quad so the on-screen breadth is unchanged.
    float spread = 0.09 + 0.25 * along;
    float halfWidth = spread * (0.68 + lobe * 0.52);

    // Density patches: the cone thins where a lobe has passed, so it reads as puffed breath
    // rather than a solid painted wedge. The 0.62 baseline keeps it connected instead of speckling.
    // Re-thresholded off `billow` rather than re-combining both noise layers a second time — the
    // two were near-identical mixes of the same pair, so this shares the term for ~4 slots.
    float puff = 0.62 + saturate(billow * 1.55 - 0.42) * 0.38;
    float envelope = saturate(along * 9.0) * saturate((1.0 - along) * 3.2);
    // One shared distance field for both layers (§51d — two shapes off the same field are one
    // shape). A second independent saturate for the core cost ~11 slots on its own.
    float depth = (halfWidth - across) * 9.5;
    float body = saturate(depth) * (0.55 + billow * 0.45) * puff * envelope;
    // A soft uneven mid-band, NOT a sharp thread — a steeper core reads as a laser filament.
    float core = saturate(depth * 0.30) * (0.5 + billow * 0.5) * envelope;

    // Brightness contract: an earlier pass read as a washed-out grey smear because the
    // premultiplied rgb sat just BELOW alpha, and a muted blue at ~0.6 coverage over a
    // (0.6, 0.75, 0.9) sky composites to almost exactly the sky colour. The cure is contrast, not
    // opacity — the BODY (not just the core) lifts toward pale frost so it is lighter than what
    // it covers. Active keeps the inhale subdued and the exhale vivid.
    //
    // Accumulated tiers rather than two chained float3 lerps: ~9 ops instead of ~24, which is
    // most of what brought this from 78 slots to budget. Weights were solved against the lerp
    // chain at typical inputs (body .8 / billow .5 / core .3) so the ramp lands in the same place.
    float frost = saturate(billow * 0.52 + core * 0.30) * CoreMix;
    float heat = body * (0.80 + billow * 0.42) * Progress;
    float3 color = OuterColor * (heat * 0.09)
        + MiddleColor * (heat * 0.56)
        + CoreColor * (heat * frost + core * EmissiveGain);
    float alpha = saturate(body * BodyAlphaGain + core * 0.30) * AlphaScale;
    return float4(color, alpha);
}

technique IceGigasFrostBreath
{
    pass IceGigasFrostBreathPass { PixelShader = compile ps_2_0 IceGigasFrostBreathPixel(); }
}
