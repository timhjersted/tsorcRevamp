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

// Nito's Death Nova — the expanding annulus, and the same technique run backwards as the contracting
// charge ring during the 120-tick channel.
//
// Active = the ring radius in half-quad units, Direction = its half thickness in UV units. Both come
// straight from the caller's real collision numbers, so the bright band is genuinely where the hitbox
// is. The caller's padding was widened from 2.4x to 4.5x the half thickness specifically to give this
// shader somewhere to put a soft outer halo: at 2.4x the ring's own outer lip sat within ~3px of the
// quad boundary and there was physically no room to feather it.
//
// Premultiplied over AlphaBlend (see the note at the bottom): the wall of the shockwave darkens the
// world it passes through and the bone seam glows on top of it.
float4 DeathRingPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float2 dir = p / (len + 0.0005);
    float2 perp = float2(-dir.y, dir.x);       // ring tangent

    // Ring-space sampling: walking around the shockfront traces a circle through texture space, so
    // the pattern is continuous by construction — no tiling grid, and none of the +/-pi seam an
    // atan2 polar mapping would reintroduce (and about a quarter of the slots).
    // The first layer's radius scrolls outward, so the material races ahead of the front; the second
    // slides along the tangent, so the front also churns sideways as it travels.
    // Low ring-space radii on purpose: walking the ring traces a SMALL circle through the noise, so
    // one revolution covers barely a tile of it. Sampling further out (the first attempt used 0.30
    // and 0.47) crossed many tiles per revolution and shredded the front into a white sawtooth.
    float n1 = tex2D(PrimarySampler, dir * (0.18 - Time * 0.30) + perp * 0.14 + 0.5).r;
    // The radius term matters: a ring-space sample with no `len` dependence is constant along every
    // ray and paints perfectly straight radial spokes across the wake. Feeding `len` in makes the
    // material churn across the front instead.
    float n2 = tex2D(DetailSampler, dir * (0.26 + len * 0.55) + perp * (Time * 0.07) + 0.5).r;
    float grain = saturate(n1 * 0.95 + n2 * 0.65 - 0.34);

    // Distance from the front, measured in half-thicknesses, with the front's own radius perturbed
    // by the noise so the shockwave is TORN rather than a compass-drawn circle.
    float inv = 1.0 / max(Direction, 0.004);
    float d = (len - Active * 0.5) * inv - (grain - 0.5) * 0.9;

    float core = saturate(1.0 - abs(d) * 2.60);            // a THIN hot seam, not the whole band
    float body = saturate(1.0 - abs(d) * 0.80);            // the band proper
    float halo = saturate(1.0 - abs(d) * 0.38) * 0.35;     // soft bloom spilling either side
    // Wake dragged INWARD behind the front, frayed by the same grain, because an expanding shell
    // leaves its material behind it rather than in front.
    float wake = saturate(-d * 0.26) * saturate(1.0 + d * 0.13) * grain;
    // Bone shards struck off the leading edge.
    float spark = saturate(grain * 2.45 - 1.55) * body;

    // No separate quad cutoff is needed here, and the algebra is worth writing down because the old
    // version got it wrong. The widest term, `halo`, is zero for |d| > 2.63, and d is displaced by at
    // most 0.45 by the grain, so the outermost lit texel sits at len = ringR + 3.08 * half. With the
    // caller's 4.5x padding that is (0.5R + 1.54h) / (R + 4.5h) against a boundary of
    // (0.5R + 2.25h) / (R + 4.5h) — a margin of 0.71h / (R + 4.5h), positive for every radius and
    // thickness the caller can pass, and independent of the noise. `wake` only ever extends inward.
    //
    // PREMULTIPLIED over AlphaBlend, not additive. Additive cannot draw a dark ring: any colour
    // bright enough to be visible over a daytime sky clips straight to white, and that — not the
    // shape maths — is what made both the old version and the first attempt at this one a jagged
    // white hoop. Here the band OCCLUDES in deep grave violet and only the bone seam and the shard
    // glints add light back on top of it.
    float3 tint = lerp(DarkColor, MidColor, saturate(body * 0.85 + wake * 0.55 + halo * 0.50));
    float alpha = saturate(body * 0.80 + wake * 0.34 + halo * 0.55) * Opacity;
    return float4(tint * alpha + CoreColor * ((core * 0.55 + spark * 0.95) * Opacity), alpha);
}

technique NitoDeathRing
{
    pass Pass1 { PixelShader = compile ps_2_0 DeathRingPixel(); }
}
