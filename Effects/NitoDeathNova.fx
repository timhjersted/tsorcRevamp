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
float4 PixelGrid; // xy = 2px block count across the final quad, zw = reciprocal

float2 PixelateNitoUV(float2 uv)
{
    return (floor(uv * PixelGrid.xy) + 0.5) * PixelGrid.zw;
}

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
    c = PixelateNitoUV(c);
    float2 p = c - 0.5;
    float len = length(p);
    // Ordinary UV-space material is intentionally used here: it is seamless, cheaper than polar
    // normalization under Reach, and prevents the radial spoke pattern that a ring-space sample can
    // stamp into a large shock front.
    float n1 = tex2D(PrimarySampler, p * 1.35 + float2(-Time * 0.25, Time * 0.08) + 0.5).r;
    float n2 = tex2D(DetailSampler, p * 2.05 + float2(Time * 0.06, -Time * 0.11) + 0.5).r;
    float grain = saturate(n1 * 0.88 + n2 * 0.72 - 0.37);

    // Distance from the front, measured in half-thicknesses, with the front's own radius perturbed
    // by the noise so the shockwave is TORN rather than a compass-drawn circle.
    // Direction is the precomputed reciprocal UV half-thickness; doing the reciprocal in C# saves
    // a per-pixel Reach instruction and keeps this mature pass below the ps_2_0 ceiling.
    float d = (len - Active * 0.5) * Direction - (grain - 0.5) * 0.70;

    float core = saturate(1.0 - abs(d) * 3.35);            // precise collision seam
    float body = saturate(1.0 - abs(d) * 0.92);            // dark material wall
    float halo = saturate(1.0 - abs(d) * 0.39) * 0.28;     // restrained bloom
    // Wake dragged INWARD behind the front, frayed by the same grain, because an expanding shell
    // leaves its material behind it rather than in front.
    float wake = saturate(-d * 0.22) * saturate(grain * 1.3 - 0.16);
    // Bone shards struck off the leading edge.
    float spark = saturate(grain * 2.65 - 1.72) * body;

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
    float3 tint = lerp(DarkColor, MidColor, saturate(body * 0.78 + wake * 0.48));
    float alpha = saturate(body * 0.86 + wake * 0.30 + halo * 0.50) * Opacity;
    return float4(tint * alpha + CoreColor * ((core * 0.40 + spark * 0.58) * Opacity), alpha);
}

// One material rupture in Quietus' staggered explosion. The caller draws this technique four times
// with independent clocks, sizes, palette families and phase offsets. Keeping the layers as separate
// draws gives each blast a real temporal break and keeps every Reach pass comfortably focused.
// Progress = this layer's 0..1 lifetime; Active = reciprocal relative scale; Direction = phase seed.
float4 DeathBlastPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateNitoUV(c);
    float2 offset = float2(Direction * 0.035 - 0.020, 0.0);
    float2 p = (c - 0.5 - offset) * Active;
    float len = length(p);
    float r = len * 2.0;

    float n1 = tex2D(PrimarySampler,
        p * 1.18 + float2(Direction * 0.37 - Time * 0.09, Direction * 0.16 + Time * 0.05) + 0.5).r;
    float n2 = tex2D(DetailSampler,
        p * 1.32 + float2(Direction * 2.1 - Time * 0.16, -Time * 0.23)).r;
    float shape = saturate(n1 * 0.70 + n2 * 0.58 - 0.27);

    float blastRadius = 0.10 + Progress * 0.78;
    float raggedRadius = blastRadius * (0.70 + shape * 0.34);
    float body = saturate((raggedRadius - r) * 4.4);
    float shell = saturate(1.0 - abs(r - raggedRadius + (n2 - 0.5) * 0.08) * 9.5);
    float hollow = saturate(r * 4.5 - 0.28);
    float tongue = saturate(n2 * 2.05 - 0.78) * body * hollow;
    float ember = saturate(n2 * 2.70 - 1.76) * shell;

    float alpha = saturate(body * 0.98) * Opacity;
    float3 rgb = DarkColor * body
               + MidColor * (tongue * 1.25 + shell * 0.42)
               + CoreColor * (ember * 0.48 + shell * 0.16);
    return float4(rgb * Opacity, alpha);
}

technique NitoDeathRing
{
    pass Pass1 { PixelShader = compile ps_2_0 DeathRingPixel(); }
}

technique NitoDeathBlast
{
    pass Pass1 { PixelShader = compile ps_2_0 DeathBlastPixel(); }
}
