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
float4 PixelGrid;

float2 PixelateNitoUV(float2 uv)
{
    return (floor(uv * PixelGrid.xy) + 0.5) * PixelGrid.zw;
}

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
    c = PixelateNitoUV(c);
    float x = c.x;
    float lane = saturate(1.0 - abs(x - 0.5) * 2.08);

    // Ground does not flow, so this layer is static; only the fire scrolls. Sampled at a LOW
    // frequency on purpose — it exists to give the fissure line an organic wobble, and anything
    // detailed enough to read as a pattern reads as a pattern (the first pass used Techno_13 at 1.7x
    // and you could see the cracked-plate texture repeating under the ground line like circuitry).
    float crack = tex2D(PrimarySampler, c * float2(0.48, 0.34) + float2(0.13, 0.07)).r;
    // High frequency across the lane, LOW frequency up it, so the flames come out as vertical tongues
    // rather than as a row of round blobs.
    float fire = tex2D(DetailSampler, float2(x * 2.15 - Time * 0.08, c.y * 0.40 - Time * 0.48)).r;

    // h > 0 above the fissure line, h < 0 below it. The line itself wobbles with the rock so it is
    // never a ruler-straight seam.
    float h = 0.58 + (crack - 0.5) * 0.075 - c.y;
    float reveal = saturate((Progress * 1.4 + 0.12 - abs(x - 0.5) * 1.75) * 3.8) * lane;

    // Flame tongues. Height is noise-driven and tops out at 0.39 against a fissure line that never
    // sits above 0.545, so the flames provably die before the top of the quad whatever the noise does.
    float flameH = 0.14 + crack * 0.30 + fire * 0.25;
    float flame = saturate(h * 8.8) * saturate((flameH - h) * 4.0) * reveal;
    // Charred glow bleeding down into the tiles; provably gone by 0.945 of the quad height. Also the
    // dominant term in the alpha, which is what makes this read as BLACK fire rather than a bright
    // orange smear: the sooty body occludes, the flame only adds light on top of it.
    float soot = saturate(-h * 2.8) * saturate((0.38 + h) * 3.2) * reveal;
    // The hot seam sitting right on the line.
    float seam = saturate(1.0 - abs(h) * 13.0) * reveal * (0.50 + fire * 0.42);
    float ember = saturate(fire * 2.65 - 1.72) * saturate(flame + seam);

    float3 rgb = DarkColor * (soot * 1.25 + flame * 0.76)
               + MidColor * (flame * 1.22 + seam * 0.86)
               + CoreColor * ember * 0.62;
    // Flames occlude as well as glow, or the sky mixes through and the deep red turns salmon.
    float alpha = saturate(soot * 1.08 + flame * 1.02 + seam * 0.64);
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
    c = PixelateNitoUV(c);
    float x = c.x - 0.5;
    float y = c.y;                       // 0 at the top, 1 at the buried base

    // HIGH frequency across the column, LOW frequency up it. That ratio is the whole trick for fire:
    // it stretches the noise features into vertical tongues. The first pass sampled y at 1.3x/2.4x
    // against x at 1.5x/2.9x — features wider than they were tall — and the column came out looking
    // like popcorn.
    float n1 = tex2D(PrimarySampler, float2(c.x * 1.48 + Time * 0.055, y * 0.62 - Time * 0.34)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 2.75 - Time * 0.08, y * 0.78 - Time * 0.67)).r;
    float fire = saturate(n1 * 0.68 + n2 * 0.78 - 0.25);
    // The SILHOUETTE comes off the smooth macro layer alone. Driving it from the combined field let
    // the fine layer chew the outline into confetti.
    float shape = saturate(n1 * 1.34 - 0.21);

    // Noise-modulated falloff distance rather than an eroded shape. reach maxes at
    // (0.14 + 0.30) * (0.35 + 0.62) = 0.427, so the column provably dies before |x| = 0.5.
    float width = 0.12 + y * 0.34;
    float reach = width * (0.34 + shape * 0.64);
    float col = saturate((reach - abs(x)) * 6.2);

    // Climbs as Progress runs, feathered at the top and bottom edges (the bottom band is under the
    // floor line, so nothing readable is lost to that fade).
    float rise = saturate((Progress * 1.23 - (1.0 - y)) * 2.85);
    col *= rise * saturate(y * 6.0) * saturate((1.0 - y) * 4.4);

    // A wider, fainter shroud of smoke so the column does not stop dead at its own silhouette.
    // width * 1.10 maxes at 0.484, still inside |x| = 0.5.
    float haze = col * (0.18 + shape * 0.36);

    // The burning fraction is deliberately the MINORITY of the column. Most of it is soot: that is
    // the difference between "black fire with red in it" and the orange blob the first pass produced.
    float hot = col * saturate(fire * 1.72 - 0.56);
    float soot = col * saturate(1.24 - fire * 0.92) + haze;
    float ember = saturate(n2 * 2.75 - 1.82) * hot;

    float3 rgb = DarkColor * (soot * 1.10 + hot * 0.50)
               + MidColor * hot * 1.30
               + CoreColor * ember * 0.70;
    // The burning parts must occlude too, or the sky mixes through them and deep red comes out salmon.
    float alpha = saturate(soot * 1.06 + hot * 1.06);
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
    c = PixelateNitoUV(c);
    // The silhouette only occupies x 0.32..0.67 of its 400px source, so a straight sample leaves a
    // thin sliver in the middle of a wide quad. Squeezing the sampled x range to the middle 55%
    // fills the draw and still leaves margin either side (0.225..0.775 — no wrap).
    float4 hand = tex2D(PrimarySampler, float2((c.x - 0.5) * 0.52 + 0.5, c.y));
    float fire = tex2D(DetailSampler, float2(c.x * 1.85 - Time * 0.075, c.y * 1.30 - Time * 0.52)).r;

    float y = c.y;
    float rise = saturate((Progress * 1.34 - (1.0 - y) * 0.84) * 3.7);
    float silhouette = hand.a * rise;
    // Cubed, not squared. The source art's red channel is high across almost the whole hand, so a
    // gentler curve paints the entire silhouette pale bone-grey and it reads as a flat cut-out; the
    // cube keeps the highlight on the raised edges of the knuckles and leaves the rest charred.
    float bone = hand.r * hand.r * hand.r * silhouette;

    // reach maxes at (0.10 + 0.26) * (0.38 + 0.60) = 0.353, so the pyre provably clears |x| = 0.5.
    float width = 0.12 + y * 0.30;
    float reach = width * (0.40 + fire * 0.58);
    float blaze = saturate((reach - abs(c.x - 0.5)) * 6.2)
                * saturate(y * 4.5) * saturate((1.0 - y) * 3.5) * rise;
    // A horizontally-widened copy of the silhouette (narrower sampled x window = fatter hand). Only
    // the x axis is stretched, so this cannot introduce the top/bottom clipping a full dilate would.
    // The difference against the real silhouette is the band either side of the hand, and the flames
    // are concentrated into it so the fire visibly wreathes the fingers instead of ignoring them.
    float halo = tex2D(PrimarySampler, float2((c.x - 0.5) * 0.34 + 0.5, c.y)).a;
    float wreath = saturate(halo - hand.a * 0.76);
    float lick = blaze * saturate(fire * 1.56 - 0.35) * (0.72 + wreath * 1.65);
    float smoke = blaze * saturate(1.16 - fire) * 0.50;
    float ember = saturate(fire * 2.75 - 1.83) * blaze * (0.65 + wreath);

    float3 rgb = DarkColor * (silhouette * 0.88 + blaze * 0.68 + smoke * 0.78)
               + MidColor * (lick * 1.18 + silhouette * 0.23)
               + CoreColor * bone * 0.62
               + float3(0.86, 0.24, 0.20) * ember * 0.52;
    float alpha = saturate(silhouette * 0.98 + blaze * 0.73);
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
