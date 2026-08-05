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

// Nito's greatsword wake — the death-magic sheath that burns along the blade during a swing.
//
// The old version took its SHAPE from T_trail12, which is a small centred four-point star flare.
// Smeared down a 255px blade-aligned quad that is the worst possible fit: nearly the whole draw
// sampled the star's black background, so all that survived was the taper falloff — a flat, static
// lozenge. The silhouette is procedural now; the textures only fray it.
//
// The quad is rotated to the blade angle and spans blade-local [-77, +178]px, so c.x = 0 sits just
// behind the hilt and c.x = 1 just past the tip. Deliberately HOLLOW: the edges carry the light and
// the interior stays thin, because the boss draws its actual blade sprite through the middle of this.
//
// Progress = 0..1 across the 18-tick swing. Direction = a per-swing phase (identity + swing kind) so
// the three hits of a combo do not stamp the identical pattern three times.
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float along = c.x;
    float across = abs(c.y - 0.5) * 2.0;    // 0 on the centreline, 1 at the quad rim
    float2 phase = float2(Direction * 5.7, Direction * 2.3);

    // Two opposing-drift layers streaming backward down the blade toward the hilt.
    // Low frequency ALONG the blade and high frequency across it: that ratio is what makes the noise
    // features elongate down the swing. The first pass had it the other way round and the wake broke
    // into round puffs.
    float n1 = tex2D(PrimarySampler, float2(along * 1.3 - Time * 1.15, c.y * 2.6) + phase).r;
    float n2 = tex2D(DetailSampler, float2(along * 2.4 - Time * 1.85, c.y * 3.6) + phase).r;
    // Note the high baseline: churn is meant to MODULATE a solid blade, not decide whether each
    // pixel exists. With a low baseline the wake dissolved into speckle and stopped reading as a
    // swing at all.
    float churn = saturate(n1 * 0.75 + n2 * 0.45 - 0.10);

    // Blade profile: rooted at the hilt, widest around three quarters out where the tip travels
    // fastest, closing to a point past the tip.
    float lens = saturate((along - 0.03) * 4.2) * saturate((1.0 - along) * 5.2);

    // Noise-modulated falloff DISTANCE (rather than eroding a finished shape): the sheath reaches
    // further in some places than others, so the outline is never a clean curve.
    // reach maxes at 0.30 + 0.50 = 0.80 and is zero at both ends of the blade, so `sheath` provably
    // hits zero before every one of the four quad edges no matter what the noise does — that is the
    // noise-independent cutoff, folded into the shape rather than bolted on. Subtract-and-scale
    // instead of the usual 1 - across/reach: same feathered edge, no divide.
    // The noise weight is deliberately the SMALLER half of this. Letting it dominate (the first pass
    // ran 0.30 + churn * 0.50) shredded the lens into disconnected puffs and the swing stopped
    // reading as a blade at all.
    float reach = lens * (0.62 + churn * 0.26);
    float sheath = saturate((reach - across) * 5.0);

    // Tattering: early in the swing the sheath is solid, late in it the noise eats holes through it.
    float tatter = saturate(churn + 0.60 - Progress * 0.80);
    float body = sheath * tatter;
    // The burning edge — a band just inside the rim rather than a fill, which is what keeps the
    // middle hollow for the blade sprite and stops the whole quad washing out to bone white.
    float rim = saturate(1.0 - abs(sheath - 0.28) * 3.2) * (0.40 + churn * 0.70) * tatter;
    // Bone glints struck off the edge.
    float spark = saturate(churn * 2.55 - 1.60) * sheath;

    float fade = 1.0 - Progress * 0.50;

    // PREMULTIPLIED over AlphaBlend rather than additive. Purple death magic added to a bright sky
    // clips to white long before it is bright enough to read; occluding with it and adding only the
    // bone highlights back keeps the wake dark, saturated and legible against sky or cave alike.
    float3 tint = lerp(DarkColor, MidColor, saturate(body * 1.10 + rim * 0.80));
    float alpha = saturate(body * 0.80 + rim * 0.55) * Opacity * fade;
    return float4(tint * alpha + CoreColor * ((rim * 0.35 + spark * 0.95) * Opacity * fade), alpha);
}

technique NitoReaperTrail
{
    pass NitoReaperTrailPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
