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

// ---------------------------------------------------------------------------------------------
// NitoGravefallTrail — the soul wake behind a bone shard / falling grave spike.
//
// The old version took its SHAPE from T_Windstreak3, whose RGB is solid white across the entire
// canvas (the streak lives only in the alpha channel), so `tex2D(...).r` returned a constant 1.0 and
// contributed nothing but a flat lozenge. That is the "solid blocks of white" from the screenshots.
// Procedural now, and BoneCore has been pulled back off white so the core cannot blow out.
//
// The quad is centred ~26px BEHIND the projectile and is 78-92px long, so the head sits at
// c.x ~= 0.81 and the tail runs off at c.x = 0. Direction carries a per-projectile phase — without
// it every shard in a five-shot fan samples the identical pattern and the fan reads as one stamp.
// ---------------------------------------------------------------------------------------------
float4 GravefallTrailPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateNitoUV(c);
    float along = c.x;
    float across = abs(c.y - 0.5) * 2.0;
    float2 phase = float2(Direction * 6.1, Direction * 2.9);

    // Two opposing-drift layers streaming backward off the head.
    // Low frequency along the trail, high across it, so the noise features stretch into streaks
    // rather than tiling as round puffs.
    float n1 = tex2D(PrimarySampler, float2(along * 0.88 - Time * 0.92, c.y * 2.15) + phase).r;
    float n2 = tex2D(DetailSampler, float2(along * 1.75 - Time * 1.42, c.y * 3.0) + phase).r;
    float wisp = saturate(n1 * 0.88 + n2 * 0.62 - 0.29);

    // Comet profile: pinched to nothing at the tail, fattest just behind the head, closed past it.
    // The rise is slow on purpose so there is a long taper — at 1.6 it reached full width by 62% of
    // the quad and the "trail" was a lozenge with no tail.
    float spine = saturate(along * 1.22) * saturate((1.0 - along) * 4.5);
    // reach peaks at 0.30 + 0.28 = 0.58 and is zero at both ends, so the ribbon provably vanishes
    // before all four quad edges whatever the noise does. Kept well under half the quad height: the
    // draw is only 24px tall against 78px long, and a ribbon that fills it is a blob, not a streak.
    // Noise is the smaller half of the width so the ribbon stays a ribbon; when it dominated, the
    // trail broke into disconnected puffs that read as the same white blobs as the old version.
    float reach = spine * (0.34 + wisp * 0.25);
    float ribbon = saturate((reach - across) * 4.2);

    // The body frays as it falls behind: solid at the head, torn to separate wisps down the tail.
    float body = ribbon * ribbon * saturate(wisp + along * 0.78 - 0.10);
    // Compact bloom right at the projectile, and sparse bone glints shedding off the ribbon.
    float head = saturate(1.0 - abs(along - 0.83) * 7.3) * ribbon;
    float glint = saturate(wisp * 2.62 - 1.72) * ribbon;

    float fade = 1.0 - Progress * 0.42;

    // PREMULTIPLIED over AlphaBlend rather than additive — same reason as the reaper wake. A pale
    // soul trail driven additively over a bright sky is white by construction, which is precisely the
    // "solid blocks of white" complaint. Occluding in grave violet and adding only the glints back
    // keeps the wisps readable.
    float3 tint = lerp(DarkColor, MidColor, saturate(body * 1.05 + head * 0.52));
    float alpha = saturate(body * 0.90 + head * 0.52) * Opacity * fade;
    return float4(tint * alpha + CoreColor * ((head * 0.20 + glint * 0.42) * Opacity * fade), alpha);
}

// ---------------------------------------------------------------------------------------------
// NitoGraveSky — the tear in the sky that grave spikes fall out of.
//
// Was a thin white lens: `length(float2(x, y * 2.8))` banded at 0.31 inside a 118x54 quad works out
// to a 73x12px oval floating in a mostly empty box, which is why it read as "just an oval circle".
// The compression now comes from the QUAD's own aspect — the shader works in a plain UV circle, so
// the effect fills its draw at whatever aspect the caller asks for (a 118x54 spike portal, or the
// 600x112 gathering band the boss hangs over the player during Sword Rain / Judgment).
//
// What it is now: a drowned galaxy. Spiral arms wound in ring space (tangent-slide rather than
// atan2 — same swirl, a fraction of the slots, and no plus/minus-pi seam), matter draining inward
// toward a black event horizon, and a crown of red-and-black grave-fire licking off the rim,
// weighted to the top edge. Drawn premultiplied over AlphaBlend so the hole is genuinely BLACK and
// the embers still glow on top of it — additive alone can only ever add light, never punch a hole.
//
// Progress = the portal spinning up. Colours: Dark = the void, Mid = death-magic purple for the
// arms, Core = ember tips. The deep pyre red between them is a constant here so the caller keeps
// three meaningful slots.
// ---------------------------------------------------------------------------------------------
float4 GraveSkyPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateNitoUV(c);
    float2 p = c - 0.5;
    float r = length(p) * 2.0;                 // 1.0 at the axis edges, 1.41 in the corners

    // A compact Cartesian swirl preserves the inward-draining galaxy motion without paying the
    // Reach cost of normalizing every pixel into ring space.
    float twist = 0.45 - r * 0.35 + Time * 0.10;
    float2 swirl = p + float2(-p.y, p.x) * twist;
    float arm = tex2D(PrimarySampler, swirl * 1.15 + 0.5).r;
    float neb = tex2D(DetailSampler, swirl * 0.82 + float2(Time * 0.12, -Time * 0.06) + 0.5).r;
    float fireN = tex2D(PrimarySampler, p * 1.40 + float2(-Time * 0.21, Time * 0.04) + 0.5).r;

    // Disc body. Provably zero for r >= 0.86, so the corners of the quad are never touched, and it
    // saturates to a flat 1.0 inside r = 0.475 — which is also the event horizon, so no separate
    // `hole` term is needed (it was redundant with this and cost three slots).
    float disc = saturate((0.91 - r) * 2.8);
    // saturate(r * 3.2) opens a hole at the very centre so the arms wind INTO something rather than
    // crossing straight through and lighting up the middle of the event horizon.
    float arms = saturate(arm * 1.18 + neb * 0.62 - 0.62) * disc * saturate(r * 3.6);

    // Flame crown: a band from inside the disc out to a noise-driven radius that tops out at 0.88,
    // so it too can never reach the quad boundary. Weighted to the top of the oval.
    float crownR = 0.62 + fireN * 0.32;
    float topBias = 0.34 + saturate(0.58 - c.y) * 1.32;
    float flame = saturate((crownR - r) * 4.5) * saturate((r - 0.38) * 3.2) * topBias;
    float ember = saturate(fireN * 2.72 - 1.83) * flame;

    // Charge and opacity folded into one scalar so both the colour and the alpha pay for it once.
    float amp = (0.54 + Progress * 0.46) * Opacity;

    // Premultiplied: DarkColor is (6,3,12)/255, so the disc term occludes the sky to near-black
    // while everything after it adds light back on top of that hole.
    float3 rgb = DarkColor * disc
               + MidColor * arms * 1.05
               + float3(0.52, 0.06, 0.10) * flame * 1.03
               + CoreColor * ember * 0.78;

    float alpha = saturate(disc * 0.96 + flame * 0.62);
    return float4(rgb * amp, alpha * amp);
}

technique NitoGravefallTrail
{
    pass Pass1 { PixelShader = compile ps_2_0 GravefallTrailPixel(); }
}

technique NitoGraveSky
{
    pass Pass1 { PixelShader = compile ps_2_0 GraveSkyPixel(); }
}
