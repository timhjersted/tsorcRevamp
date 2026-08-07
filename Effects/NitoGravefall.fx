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
    float along = c.x;
    float across = abs(c.y - 0.5) * 2.0;
    float2 phase = float2(Direction * 6.1, Direction * 2.9);

    // Two opposing-drift layers streaming backward off the head.
    // Low frequency along the trail, high across it, so the noise features stretch into streaks
    // rather than tiling as round puffs.
    float n1 = tex2D(PrimarySampler, float2(along * 1.1 - Time * 1.05, c.y * 2.6) + phase).r;
    float n2 = tex2D(DetailSampler, float2(along * 2.2 - Time * 1.60, c.y * 3.4) + phase).r;
    float wisp = saturate(n1 * 0.85 + n2 * 0.70 - 0.32);

    // Comet profile: pinched to nothing at the tail, fattest just behind the head, closed past it.
    // The rise is slow on purpose so there is a long taper — at 1.6 it reached full width by 62% of
    // the quad and the "trail" was a lozenge with no tail.
    float spine = saturate(along * 1.25) * saturate((1.0 - along) * 4.8);
    // reach peaks at 0.30 + 0.28 = 0.58 and is zero at both ends, so the ribbon provably vanishes
    // before all four quad edges whatever the noise does. Kept well under half the quad height: the
    // draw is only 24px tall against 78px long, and a ribbon that fills it is a blob, not a streak.
    // Noise is the smaller half of the width so the ribbon stays a ribbon; when it dominated, the
    // trail broke into disconnected puffs that read as the same white blobs as the old version.
    float reach = spine * (0.30 + wisp * 0.28);
    float ribbon = saturate((reach - across) * 4.5);

    // The body frays as it falls behind: solid at the head, torn to separate wisps down the tail.
    float body = ribbon * ribbon * saturate(wisp + along * 0.70 - 0.12);
    // Compact bloom right at the projectile, and sparse bone glints shedding off the ribbon.
    float head = saturate(1.0 - abs(along - 0.82) * 8.0) * ribbon;
    float glint = saturate(wisp * 2.45 - 1.55) * ribbon;

    float fade = 1.0 - Progress * 0.45;

    // PREMULTIPLIED over AlphaBlend rather than additive — same reason as the reaper wake. A pale
    // soul trail driven additively over a bright sky is white by construction, which is precisely the
    // "solid blocks of white" complaint. Occluding in grave violet and adding only the glints back
    // keeps the wisps readable.
    float3 tint = lerp(DarkColor, MidColor, saturate(body * 1.15 + head * 0.60));
    float alpha = saturate(body * 0.85 + head * 0.60) * Opacity * fade;
    return float4(tint * alpha + CoreColor * ((head * 0.28 + glint * 0.60) * Opacity * fade), alpha);
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
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;                       // 1.0 at the axis edges, 1.41 in the corners
    float2 dir = p / (len + 0.0005);
    float2 perp = float2(-dir.y, dir.x);       // ring tangent

    // Spiral arms: the sample slides along the tangent by an amount that grows toward the centre,
    // which is exactly what winds a straight noise field into arms, and the whole field rotates.
    float twist = 0.62 - r * 0.52 + Time * 0.11;
    float arm = tex2D(PrimarySampler, dir * (0.33 + r * 0.25) + perp * twist + 0.5).r;
    // Nebula underlayer draining INWARD: pushing the sample radius outward over time makes the
    // pattern appear to fall toward the centre.
    // (reuses `perp * twist` verbatim rather than scaling it — a second scale cost two slots for a
    // difference nobody can see once the two layers are combined)
    float neb = tex2D(DetailSampler, dir * (0.20 + Time * 0.15) + perp * twist + 0.5).r;
    // Rim flames, sampled in ring space so they are continuous the whole way round with no seam,
    // scrolling outward so the tongues visibly lick away from the disc.
    float fireN = tex2D(PrimarySampler, dir * (0.44 - Time * 0.26) + perp * 0.31 + 0.5).r;

    // Disc body. Provably zero for r >= 0.86, so the corners of the quad are never touched, and it
    // saturates to a flat 1.0 inside r = 0.475 — which is also the event horizon, so no separate
    // `hole` term is needed (it was redundant with this and cost three slots).
    float disc = saturate((0.86 - r) * 2.6);
    // saturate(r * 3.2) opens a hole at the very centre so the arms wind INTO something rather than
    // crossing straight through and lighting up the middle of the event horizon.
    float arms = saturate(arm * 1.30 + neb * 0.65 - 0.68) * disc * saturate(r * 3.2);

    // Flame crown: a band from inside the disc out to a noise-driven radius that tops out at 0.88,
    // so it too can never reach the quad boundary. Weighted to the top of the oval.
    float crownR = 0.58 + fireN * 0.30;
    float topBias = 0.42 + saturate(0.5 - c.y) * 1.16;
    float flame = saturate((crownR - r) * 5.2) * saturate((r - 0.40) * 3.4) * topBias;
    float ember = saturate(fireN * 2.5 - 1.62) * flame;

    // Charge and opacity folded into one scalar so both the colour and the alpha pay for it once.
    float amp = (0.58 + Progress * 0.42) * Opacity;

    // Premultiplied: DarkColor is (6,3,12)/255, so the disc term occludes the sky to near-black
    // while everything after it adds light back on top of that hole.
    float3 rgb = DarkColor * disc
               + MidColor * arms * 1.25
               + float3(0.62, 0.08, 0.11) * flame * 1.15      // deep pyre red
               + CoreColor * (ember * 1.9);   // flame*flame hot-tip term traded for the centre hole

    float alpha = saturate(disc * 0.92 + flame * 0.55);
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
