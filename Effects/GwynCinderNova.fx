// Gwyn's Cinder Nova — the First Flame thrown outward as a ring, plus the inward gather that
// telegraphs it. Two techniques, one uniform set, so the C# call sites differ by a single string.
//
// DAMAGE CONTRACT (mirrors GwynCinderNova.Colliding): the white-hot outer LIP sits exactly at
// RingRadius + RingHalfThickness and is deliberately the crispest thing in the effect — a ~3px
// feather. Everything ragged, licking or sooty trails INWARD, over ground the ring has already
// swept, so the decoration can never imply damage ahead of the real collision boundary.
//
// Both techniques are drawn under BlendState.AlphaBlend, which XNA treats as PREMULTIPLIED. Colour
// is ACCUMULATED from density-weighted tiers, so the sum is already premultiplied and the return is
// `float4(color * Opacity, alpha)` — a bare `float4(color, alpha)` here would paint a flat tinted
// rectangle the size of the quad. Additive was the old blend and it is why this effect read as a
// pale hoop: over Terraria's daytime sky (~0.6, 0.75, 0.9) nothing saturated survives additively,
// and fire without a dark sooty body is not fire.
#include "PixelShaderCommon.fxh"

sampler ShapeNoise : register(s0);    // macro turbulence — decides the SILHOUETTE only
sampler DetailNoise : register(s1);   // fine turbulence — colour and embers only, never shape

float3 OuterColor;        // sooty trailing smoke. Dark on purpose: it occludes rather than glows.
float3 FlameColor;        // the orange body
float3 CoreColor;         // white-hot lip and embers
float Opacity;
float Time;
float2 DrawSize;          // the quad in world pixels
float2 CoordScale;        // PrimaryTextureSize / DrawSize — recovers 0..1 quad UV from source coords
float4 PixelGrid;         // xy = 2px block count across the quad, zw = its reciprocal
float RingRadius;
float RingHalfThickness;
float TrailLength;

float4 GwynCinderNovaPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords * CoordScale, PixelGrid);
    float2 fromCenter = (uv - 0.5) * DrawSize;
    float radius = length(fromCenter);
    float2 dir = fromCenter / max(radius, 1.0);
    float2 swirl = float2(-dir.y, dir.x);
    float front = radius - RingRadius;               // >0 is ahead of the damage boundary

    // Ring space: sampling at `dir * k` walks a bounded disc of the texture no matter how large the
    // quad grows, so the ring is seamless BY CONSTRUCTION — there is no tiling repeat to hide. The
    // `depth` term walks the sample radially so the band has thickness rather than being one smeared
    // streak, and sliding along the tangent rotates the pattern, rolling the fire around the
    // circumference for a few ops instead of a sin/cos rotation (which alone costs ~30 slots).
    float depth = front * 0.0042;
    float2 shapePoint = (dir + swirl * (Time * 0.085)) * (0.36 + depth) + 0.5;
    float2 detailPoint = (dir - swirl * (Time * 0.150)) * (0.83 + depth * 3.1) + 0.5;
    float shape = tex2D(ShapeNoise, shapePoint).r;
    float detail = tex2D(DetailNoise, detailPoint).r;

    // The lip is a hard edge at the collision radius — 1/0.34 ≈ 3px of feather — and the sheath's
    // trailing edge is the noise-driven one, so the fire licks and breathes while the damage
    // boundary the player reads never moves. Noise is the dominant half of `reach`, which normally
    // breaks a shape into disconnected puffs (§46); it cannot here, because the crisp lip anchors
    // every column of the sheath to the same outer circle.
    //
    // This is ONE mask, not a hot band plus a separate sooty wake. Those were two shapes built from
    // the same `front` and the same `shape` sample, and paying for the maths twice cost 8 slots of a
    // 64-slot budget for a boundary nothing could see: the flame-to-soot transition is carried by
    // `towardLip` in the colour ramp instead, which was already being computed.
    float lip = saturate((RingHalfThickness - front) * 0.34);
    float reach = RingHalfThickness * 0.85 + TrailLength * (0.35 + shape * 1.05);
    float sheath = lip * saturate((front + reach) * 0.028);

    // Heat falls off toward the middle of the ring, so the outer third is flame and the trailing
    // depth goes over to soot. Without this the whole sheath saturates to FlameColor and the effect
    // reads as an orange donut rather than as fire with a burnt tail.
    float towardLip = saturate((front + RingHalfThickness * 1.10) * 0.030);
    float heat = sheath * towardLip * (0.55 + detail * 0.62);
    float core = sheath * saturate((front + RingHalfThickness * 0.35) * 0.045);

    // Embers ride the flame, off the top of the detail field. Reuses the sample already made.
    float ember = saturate(detail * 1.85 - 1.05) * heat;

    // Accumulated tiers rather than a chain of lerps: each hotter layer is confined by its own
    // density term, which reads like real HDR falloff and costs roughly half. Every weight here is
    // already a density, so the sum IS the premultiplied colour and only needs Opacity applied —
    // the deep sheath contributes almost no light at high alpha, which is what makes the trailing
    // soot occlude the sky instead of tinting it (§43).
    float alpha = saturate(sheath * 0.90 + core * 0.30) * Opacity;
    float3 color = OuterColor * (sheath * 0.90)
        + FlameColor * (heat * 0.85)
        + CoreColor * (core * core * 1.10 + ember * 0.90);
    return float4(color * Opacity, alpha);
}

// The windup: an IRIS of fire squeezing shut on him. It must not look like a small copy of the
// blast — the player has to read "he is gathering it in, get clear" rather than "a ring is already
// out here" — so it deliberately mirrors the blast's construction. There the crisp edge is the
// OUTER one, because that is the damage boundary. Here it is the INNER one, because the shrinking
// hole is the thing to read. Same fire, opposite anchor, unmistakably the opposite motion.
float4 GwynCinderNovaChargePixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords * CoordScale, PixelGrid);
    float2 fromCenter = (uv - 0.5) * DrawSize;
    float radius = length(fromCenter);
    float2 dir = fromCenter / max(radius, 1.0);
    float2 swirl = float2(-dir.y, dir.x);
    float front = radius - RingRadius;                // >0 = out in the burning annulus

    float2 shapePoint = (dir + swirl * (Time * 0.110)) * (0.40 + front * 0.0035) + 0.5;
    float2 detailPoint = (dir - swirl * (Time * 0.190)) * (0.90 + front * 0.0090) + 0.5;
    float shape = tex2D(ShapeNoise, shapePoint).r;
    float detail = tex2D(DetailNoise, detailPoint).r;

    // `wall` maxes at (34 + RingRadius * 0.22) * 1.60, which at the caller's 155px start radius is
    // 109px — so the iris provably ends by radius 264 and the caller's fixed 320px draw radius
    // leaves 56px of clear quad at every stage of the collapse. No separate cutoff term needed.
    float mouth = saturate(front * 0.30);             // ~3px feather on the hole's rim
    float wall = (34.0 + RingRadius * 0.22) * (0.55 + shape * 1.05);
    float iris = mouth * saturate((wall - front) * 0.030);

    // Brightness bands marching inward through the flame — the motion cue, four ops and no fetch.
    // A phase rising with both `front` and time holds constant at a front that SHRINKS. The band
    // never goes to zero (0.55 floor): it modulates the fire, it does not slice it into rings.
    //
    // The speed is a constant, deliberately. Scaling `Time` by anything that changes during the
    // effect is a trap: it is a free-running hourly clock, so nudging the multiplier by 0.01 jumps
    // the phase by tens of scroll periods in a single frame. Progress-driven acceleration has to be
    // an accumulator on the caller, not a multiply here — and it was not worth an accumulator.
    float shellPhase = frac(front * 0.030 + Time * 0.85);
    float shells = 0.55 + saturate((0.42 - shellPhase) * 2.60) * 0.75;

    // The hot rim of the hole, as a plain falloff in `front` rather than a second
    // saturate()-of-a-difference: 4 slots cheaper and the same shape.
    float rim = iris * saturate(1.0 - front * 0.055);
    float body = iris * shells * (0.45 + detail * 0.80);

    // Alpha carries the colour here. A flame reading pastel over the daytime sky is under-occluding,
    // not under-lit (§48) — brightening it instead only walks it further toward white. Tiers are
    // accumulated rather than lerped, as in the blast, and for the same slot reasons.
    float alpha = saturate(iris * 0.55 + body * 0.75 + rim * 0.65) * Opacity;
    float3 color = OuterColor * (iris * 0.85)
        + FlameColor * (body * 1.15)
        + CoreColor * (rim * rim * 0.55);
    return float4(color * Opacity, alpha);
}

technique GwynCinderNova
{
    pass GwynCinderNovaPass
    {
        PixelShader = compile ps_2_0 GwynCinderNovaPixel();
    }
}

technique GwynCinderNovaCharge
{
    pass GwynCinderNovaChargePass
    {
        PixelShader = compile ps_2_0 GwynCinderNovaChargePixel();
    }
}
