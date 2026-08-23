// Gravity of the Sun — Gwyn's 760px pull field. It deals no damage; the greatsword waiting at the
// centre is the payload. So the effect has exactly two jobs: sell that everything inside it is being
// dragged inward, and suggest roughly how far the field reaches.
//
// THE REPEAT, AND WHAT WAS WORTH KEEPING. The original's pattern was good — an `abs(a - b)`
// interference between two noise fields, which lights up where the two cross and produces filament
// walls present in neither layer. Its defect was purely that it sampled PLANAR at
// `fieldPoint * (3.4 + radial01 * 1.8)`, about five texture units from centre to rim, so a 512px
// noise repeated ~10 times across the 1520px quad and rendered as an obvious lattice of cells.
// Enlarging the scale only enlarges the lattice, and no "more seamless" texture helps: the tile
// boundary is invisible, the REPETITION is what reads.
//
// Ring space (`dir * k + 0.5`) never leaves a disc of radius k around the texture centre, whatever
// the quad's size, so there is no second tile to reach and the repeat cannot exist. The interference
// technique is kept intact on top of it. Ring space also smears its features radially, which §49
// calls a defect — here it is the subject, because the features ARE the inflow streams.
//
// An intermediate rewrite threw out the interference along with the repeat and left a single smooth
// field plus a hard analytic rim. That read as basic, and the rim read as a circle drawn on top of
// the effect rather than as part of it. Both are deliberately reversed here.
#include "PixelShaderCommon.fxh"

sampler WebNoise : register(s0);      // cellular web — the filament structure
sampler ChurnNoise : register(s1);    // fine turbulence it interferes against

float3 BoundaryColor;     // the faint wash filling the field
float3 StreamColor;       // the golden inflow filaments
float3 CoreColor;         // where filaments cross and go hot
float Opacity;
float Time;
float2 DrawSize;          // the quad in world pixels
float2 CoordScale;        // PrimaryTextureSize / DrawSize — recovers 0..1 quad UV from source coords
float4 PixelGrid;         // xy = block count across the quad, zw = its reciprocal
float PullRadius;
float InnerRadius;

float4 GwynSolarVortexPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords * CoordScale, PixelGrid);
    float2 fromCenter = (uv - 0.5) * DrawSize;
    float radius = length(fromCenter);
    float2 dir = fromCenter / max(radius, 1.0);
    float2 swirl = float2(-dir.y, dir.x);
    float radial01 = saturate(radius / PullRadius);

    // Inward motion for about two slots and no dedicated term. Adding an offset to the ring-space
    // SCALE walks the sampled radius outward, so features appear to travel inward; frac() makes that
    // loop seamlessly, because the sampler wraps and a whole texture unit of shift is a no-op.
    // Multiplying Time by a varying speed instead jumps the phase by hours of scroll — see §51a.
    float inflow = frac(Time * 0.30);

    // Counter-twisting the two layers keeps the interference alive instead of letting it sit as one
    // stamped pattern. Twist grows toward the centre, winding the filaments into a vortex for a few
    // ops rather than the ~30 slots a real rotation matrix costs.
    float twistA = (1.0 - radial01) * 0.42 + Time * 0.13;
    float twistB = (1.0 - radial01) * 0.75 - Time * 0.09;
    float2 pointA = dir * (0.70 + radial01 * 0.80 + inflow) + swirl * twistA + 0.5;
    float2 pointB = dir * (1.05 + radial01 * 1.25 + inflow) + swirl * twistB + 0.5;
    // The filaments are the CONTOUR where the two fields are equal, not where they differ. That
    // distinction is the whole effect: `abs(a - b)` is large across most of the disc, so thresholding
    // it upward fills the field with a solid glowing blob, which is what the first attempt at this
    // rewrite produced. Inverting it lights up only the thin iso-lines where the two layers cross —
    // walls that exist in neither texture, drifting as the layers counter-twist past each other.
    float web = abs(tex2D(WebNoise, pointA).r - tex2D(ChurnNoise, pointB).r);
    float filaments = saturate(1.0 - web * 9.0);

    // Soft boundary, fading over ~120px. The rim this replaces was a 20px analytic band pinned to
    // PullRadius. It marked the pull radius exactly — but the field does no damage, so there was
    // never a boundary worth drawing that hard, and it read as a circle sitting on top of the effect
    // rather than as part of it. `insideField` still provably reaches zero AT PullRadius, which is
    // inside the padded quad, so nothing can clip against an edge.
    float insideField = saturate((PullRadius - radius) * 0.0085);
    float outsideCore = saturate((radius - InnerRadius) * 0.016);
    float field = insideField * outsideCore;

    // Density rises toward the centre, so the filaments visibly converge rather than filling the
    // disc evenly. That gradient is what makes a radial pattern read as inflow, not as a starburst.
    float flow = filaments * field * (0.26 + (1.0 - radial01) * 1.05);

    // Premultiplied alpha, deliberately at LOW alpha with rgb above it: this is honestly light and
    // it covers most of the screen, so it has to tint the fight rather than hide it. The same blend
    // contract that lets the nova's soot occlude lets this one glow.
    float alpha = saturate(flow * 0.85 + field * 0.05) * Opacity;
    float3 color = BoundaryColor * (field * 0.055)
        + StreamColor * (flow * 0.80)
        + CoreColor * (flow * flow * 0.60);
    return float4(color * Opacity, alpha);
}

technique GwynSolarVortex
{
    pass GwynSolarVortexPass
    {
        PixelShader = compile ps_2_0 GwynSolarVortexPixel();
    }
}
