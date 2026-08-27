#include "PixelShaderCommon.fxh"

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

// Nito's rotational sword attacks use the same procedural sweep construction proven by Gwyn's
// greatsword, but the material language is Nito's: violet death magic in phase one and blood-red
// destined death in phase two. The caller rotates this quad with the blade and flips local Y for
// reverse-handed swings. The thrust deliberately does NOT use this technique; it retains the narrow
// blade-local sheath below because a broad crescent would imply sideways danger during a lunge.
float4 NitoReaperSweepPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);
    float2 p = uv * 2.0 - 1.0;

    // Circular blade path centred behind the quad, with +X pointing along the sword. These bounds
    // are the same resolution-independent construction used by GwynCinderSlash: the farthest flame
    // reaches local x = 0.88, leaving 0.12 of clear quad for a guaranteed soft cutoff.
    float d = length(p - float2(-0.62, 0.0)) - 1.16;

    // Progress moves the travelling tip from one side of the swing to the other. Resolve that taper
    // before thickness so the leading end closes to a point instead of being clipped into a bar.
    float sweepY = Progress * 2.35 - 1.15;
    float behind = sweepY - p.y;
    float lead01 = saturate(behind * 3.40);
    float age = saturate(1.0 - behind * 0.42);
    float halfWidth = 0.34 * (1.0 - p.y * p.y) * lead01;

    // Macro turbulence shapes the trailing edge; finer turbulence only textures its heat. Sampling
    // across the arc faster than along it stretches the features into directional flame tongues.
    float2 flowUV = float2(d * 1.30 - Time * 0.55, p.y * 0.55 + Time * 0.10);
    float shape = tex2D(PrimarySampler, flowUV).r;
    float detail = tex2D(DetailSampler, flowUV * 1.90 + float2(Time * 0.31, -Time * 0.12)).r;

    // A crisp travelling edge and a noise-frayed wake. Noise can extend only backward, never ahead
    // of the cutting edge, so the brightest region remains attached to the current weapon motion.
    float lead = saturate((halfWidth - d) * 13.0);
    float tail = saturate((d + halfWidth * (0.85 + shape * 2.30)) * 3.20);
    float blade = lead * tail;
    float tipHeat = blade * lead01 * (1.0 - lead01) * 4.0;

    float body = blade * (0.30 + age * 0.70);
    float heat = body * (0.42 + detail * 0.85) + tipHeat * 0.55;
    float edge = body * saturate((halfWidth * 0.55 - abs(d)) * 6.0) + tipHeat * 0.70;

    // Premultiplied AlphaBlend: the dark aged wake can occlude on bright sky while the colored core
    // remains emissive. The caller supplies purple or red ramps through the same parameter contract.
    float alpha = saturate(body * 1.25 + edge * 0.35) * Opacity;
    float3 color = DarkColor * (body * 0.95)
        + MidColor * (heat * 0.85)
        + CoreColor * (edge * edge * 0.95);
    return float4(color * Opacity, alpha);
}

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

technique NitoReaperSweep
{
    pass NitoReaperSweepPass
    {
        PixelShader = compile ps_2_0 NitoReaperSweepPixel();
    }
}
