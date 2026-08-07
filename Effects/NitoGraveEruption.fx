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

// All three techniques in this file run the PYRE palette (Dark = near-black, Mid = deep blood red,
// Core = ember orange) and are drawn premultiplied over BlendState.AlphaBlend rather than additively.
// That is the only way to get black fire: additive can add light to the sky but can never take any
// away, which is why the old versions read as a grey grainy sparkle over a bright blue background.
// With premultiplied alpha the sooty body punches a genuinely dark hole and the embers still glow
// on top of it, in one pass.

// ---------------------------------------------------------------------------------------------
// NitoGroundRift — the telegraph under a grave spike / grasping hand.
//
// Rebuilt as a FULL SIDE-VIEW effect. The old one was `length(float2(x, y * 2.3))` banded into a
// squashed disc — a top-down decal in a side-on game, which is why it looked wrong the moment the
// terrain under it was not perfectly flat. This version is a horizontal fissure that straddles the
// quad's midline: flame tongues lick up out of it, charred glow smears down INTO the tiles, and the
// whole thing is meant to clip into the ground rather than sit on top of it.
//
// Progress = the telegraph running; the fissure tears open from the middle of the lane outward.
// ---------------------------------------------------------------------------------------------
float4 GroundRiftPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float x = c.x;
    float lane = saturate((x - 0.02) * 6.0) * saturate((0.98 - x) * 6.0);

    // Ground does not flow, so this layer is static; only the fire scrolls. Sampled at a LOW
    // frequency on purpose — it exists to give the fissure line an organic wobble, and anything
    // detailed enough to read as a pattern reads as a pattern (the first pass used Techno_13 at 1.7x
    // and you could see the cracked-plate texture repeating under the ground line like circuitry).
    float crack = tex2D(PrimarySampler, c * float2(0.55, 0.45)).r;
    // High frequency across the lane, LOW frequency up it, so the flames come out as vertical tongues
    // rather than as a row of round blobs.
    float fire = tex2D(DetailSampler, float2(x * 2.6 - Time * 0.10, c.y * 0.55 - Time * 0.55)).r;

    // h > 0 above the fissure line, h < 0 below it. The line itself wobbles with the rock so it is
    // never a ruler-straight seam.
    float h = 0.5 + (crack - 0.5) * 0.09 - c.y;
    float reveal = saturate((Progress * 1.3 + 0.10 - abs(x - 0.5) * 1.8) * 3.5) * lane;

    // Flame tongues. Height is noise-driven and tops out at 0.39 against a fissure line that never
    // sits above 0.545, so the flames provably die before the top of the quad whatever the noise does.
    float flameH = 0.08 + fire * 0.31;
    float flame = saturate(h * 11.0) * saturate((flameH - h) * 4.5) * reveal;
    // Charred glow bleeding down into the tiles; provably gone by 0.945 of the quad height. Also the
    // dominant term in the alpha, which is what makes this read as BLACK fire rather than a bright
    // orange smear: the sooty body occludes, the flame only adds light on top of it.
    float soot = saturate(-h * 3.2) * saturate((0.40 + h) * 3.6) * reveal;
    // The hot seam sitting right on the line.
    float seam = saturate(1.0 - abs(h) * 11.0) * reveal * (0.55 + fire * 0.60);
    float ember = saturate(fire * 2.4 - 1.55) * saturate(flame + seam);

    float3 rgb = DarkColor * saturate(soot * 1.5 + flame * 0.9)
               + MidColor * (flame * 1.45 + seam * 1.15)
               + CoreColor * (ember * 1.15 + seam * seam * 0.45);
    // Flames occlude as well as glow, or the sky mixes through and the deep red turns salmon.
    float alpha = saturate(soot * 1.05 + flame * 1.20 + seam * 0.85);
    return float4(rgb * Opacity, alpha * Opacity);
}

// ---------------------------------------------------------------------------------------------
// NitoGravePlume — the pyre the crimson sword-dance blade climbs out of.
//
// Replaces the "tiny white grainy dust" in the screenshot: a column of red-and-black grave-fire, fat
// at the base and licking into separated tongues at the top. Sooty body and burning body are two
// different reads off the SAME noise (low noise = smoke, high noise = flame), which is what makes it
// look like one substance instead of two stacked layers.
//
// Progress = the spike's rise, and it runs backwards while the spike sinks, so the fire retreats
// into the ground with it.
// ---------------------------------------------------------------------------------------------
float4 GravePlumePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float x = c.x - 0.5;
    float y = c.y;                       // 0 at the top, 1 at the buried base

    // HIGH frequency across the column, LOW frequency up it. That ratio is the whole trick for fire:
    // it stretches the noise features into vertical tongues. The first pass sampled y at 1.3x/2.4x
    // against x at 1.5x/2.9x — features wider than they were tall — and the column came out looking
    // like popcorn.
    float n1 = tex2D(PrimarySampler, float2(c.x * 1.9 + Time * 0.07, y * 0.85 - Time * 0.42)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 3.4 - Time * 0.11, y * 1.20 - Time * 0.78)).r;
    float fire = saturate(n1 * 0.80 + n2 * 0.75 - 0.28);
    // The SILHOUETTE comes off the smooth macro layer alone. Driving it from the combined field let
    // the fine layer chew the outline into confetti.
    float shape = saturate(n1 * 1.35 - 0.22);

    // Noise-modulated falloff distance rather than an eroded shape. reach maxes at
    // (0.14 + 0.30) * (0.35 + 0.62) = 0.427, so the column provably dies before |x| = 0.5.
    float width = 0.14 + y * 0.30;
    float reach = width * (0.35 + shape * 0.62);
    float col = saturate((reach - abs(x)) * 7.0);

    // Climbs as Progress runs, feathered at the top and bottom edges (the bottom band is under the
    // floor line, so nothing readable is lost to that fade).
    float rise = saturate((Progress * 1.2 - (1.0 - y)) * 3.0);
    col *= rise * saturate(y * 6.0) * saturate((1.0 - y) * 5.0);

    // A wider, fainter shroud of smoke so the column does not stop dead at its own silhouette.
    // width * 1.10 maxes at 0.484, still inside |x| = 0.5.
    float haze = saturate((width * 1.10 - abs(x)) * 4.0)
               * rise * saturate(y * 6.0) * saturate((1.0 - y) * 5.0) * (0.12 + shape * 0.30);

    // The burning fraction is deliberately the MINORITY of the column. Most of it is soot: that is
    // the difference between "black fire with red in it" and the orange blob the first pass produced.
    float hot = col * saturate(fire * 1.90 - 0.85);
    float soot = col * saturate(1.15 - fire * 1.05) + haze;
    float ember = saturate(fire * 2.50 - 1.68) * col;

    float3 rgb = DarkColor * (soot + hot * 0.35)
               + MidColor * hot * 1.55
               + CoreColor * (ember * 1.15 + hot * hot * 0.30);
    // The burning parts must occlude too, or the sky mixes through them and deep red comes out salmon.
    float alpha = saturate(soot * 1.05 + hot * 1.15);
    return float4(rgb * Opacity, alpha * Opacity);
}

// ---------------------------------------------------------------------------------------------
// NitoGraveHand — the grasping hand. T_NitoGraveHand is a bespoke silhouette and stays as the shape
// source (that part was never the problem); what was missing is anything around it.
//
// Now: a bone hand clawing out of a pillar of grave-fire. The fire is a procedural column keyed to
// the hand's footprint rather than a dilated copy of the silhouette, because the source art runs
// edge to edge vertically and a dilate would have clipped flat against the top of the quad.
//
// Progress = 0..0.3 during the telegraph, then 0..1 over the ten ticks of the grab.
// ---------------------------------------------------------------------------------------------
float4 GraveHandPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // The silhouette only occupies x 0.32..0.67 of its 400px source, so a straight sample leaves a
    // thin sliver in the middle of a wide quad. Squeezing the sampled x range to the middle 55%
    // fills the draw and still leaves margin either side (0.225..0.775 — no wrap).
    float4 hand = tex2D(PrimarySampler, float2((c.x - 0.5) * 0.55 + 0.5, c.y));
    float fire = tex2D(DetailSampler, float2(c.x * 2.2 - Time * 0.09, c.y * 1.9 - Time * 0.60)).r;

    float y = c.y;
    float rise = saturate((Progress * 1.35 - (1.0 - y) * 0.85) * 4.0);
    float silhouette = hand.a * rise;
    // Cubed, not squared. The source art's red channel is high across almost the whole hand, so a
    // gentler curve paints the entire silhouette pale bone-grey and it reads as a flat cut-out; the
    // cube keeps the highlight on the raised edges of the knuckles and leaves the rest charred.
    float bone = hand.r * hand.r * hand.r * silhouette;

    // reach maxes at (0.10 + 0.26) * (0.38 + 0.60) = 0.353, so the pyre provably clears |x| = 0.5.
    float width = 0.10 + y * 0.26;
    float reach = width * (0.38 + fire * 0.60);
    float blaze = saturate((reach - abs(c.x - 0.5)) * 7.0)
                * saturate(y * 5.0) * saturate((1.0 - y) * 4.0) * rise;
    // A horizontally-widened copy of the silhouette (narrower sampled x window = fatter hand). Only
    // the x axis is stretched, so this cannot introduce the top/bottom clipping a full dilate would.
    // The difference against the real silhouette is the band either side of the hand, and the flames
    // are concentrated into it so the fire visibly wreathes the fingers instead of ignoring them.
    float halo = tex2D(PrimarySampler, float2((c.x - 0.5) * 0.40 + 0.5, c.y)).a;
    float wreath = saturate(halo - hand.a);
    float lick = blaze * saturate(fire * 1.70 - 0.42) * (1.0 + wreath * 1.6);
    float ember = saturate(fire * 2.50 - 1.62) * blaze * (1.0 + wreath);

    float3 rgb = DarkColor * saturate(silhouette * 1.05 + blaze)
               + MidColor * (lick * 1.45 + silhouette * 0.34)
               + CoreColor * (bone * 0.70 + ember * 1.15);
    float alpha = saturate(silhouette * 0.95 + blaze * 0.70);
    return float4(rgb * Opacity, alpha * Opacity);
}

technique NitoGroundRift
{
    pass Pass1 { PixelShader = compile ps_2_0 GroundRiftPixel(); }
}

technique NitoGravePlume
{
    pass Pass1 { PixelShader = compile ps_2_0 GravePlumePixel(); }
}

technique NitoGraveHand
{
    pass Pass1 { PixelShader = compile ps_2_0 GraveHandPixel(); }
}
