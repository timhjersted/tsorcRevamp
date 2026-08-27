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

// Nito's grave-miasma — the lingering poison cloud from Miasma Breath.
//
// Two things were wrong with it. It had no noise-independent quad cutoff (`saturate((0.53 - radius)
// * 7.5)` is still 0.225 at the edge midpoint, so the square quad sliced the cloud flat), and it was
// drawn ADDITIVELY. Additive smoke over a bright sky can only ever add light — it can never read as
// dark, thick or occluding, which is why a supposedly choking poison fog looked like a faint green
// smear. It is premultiplied over AlphaBlend now, so it genuinely blocks what is behind it.
//
// The quad rotates with the projectile, so the noise tumbles with the puff instead of sliding
// through a stationary field. Direction carries a per-cloud phase — Miasma Breath spawns a bank of
// these and without it every puff in the bank samples the identical pattern.
float4 MiasmaPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateNitoUV(c);
    float2 p = c - 0.5;
    float r = length(float2(p.x * 0.93, p.y * 1.04)) * 2.0;
    float2 phase = float2(Direction * 4.3, Direction * 1.9);

    // The MACRO layer carries the silhouette and the fibrous layer only textures it. Weighted the
    // other way (0.95 / 0.60 at 2.9x) the fine layer dominated the outline and the puff came out
    // looking like green hair rather than a cloud.
    float n1 = tex2D(PrimarySampler, c * 0.98 + float2(-Time * 0.043, Time * 0.027) + phase).r;
    float n2 = tex2D(DetailSampler, c * 1.28 + float2(Time * 0.066, -Time * 0.050) + phase).r;
    float billow = saturate(n1 * 1.18 + n2 * 0.32 - 0.38);

    // Noise-modulated radius, so the silhouette is lumpy and globular rather than a disc. It tops
    // out at 0.40 + 0.10 + 0.30 = 0.80 — provably inside the quad in every direction, whatever the
    // noise does. Squaring the falloff also domes it, which is what makes a small puff read as a
    // volume instead of a flat coin.
    float puff = saturate((0.44 + Progress * 0.08 + billow * 0.31 - r) * 3.15);
    puff *= puff;

    // A cheap fake key light from the upper left. This is most of what sells the volume: without it
    // the cloud is uniformly lit and reads as paper.
    float lit = saturate(0.55 - p.x * 1.15 - p.y * 1.42);
    float pocket = saturate(0.68 - n2 * 1.22);
    float vein = saturate(n2 * 2.35 - 1.50) * puff;

    // A wider, much thinner skirt of vapour so the cloud does not end at its own lumpy boundary.
    // Tops out at 0.62 + 0.22 = 0.84, so it too stays provably inside the quad.
    float haze = saturate((0.72 + billow * 0.18 - r) * 1.9);
    haze = haze * haze * 0.30;

    float3 tint = lerp(DarkColor, MidColor, saturate(lit * 1.08 - pocket * 0.72 + 0.16));
    float alpha = saturate(puff * 0.96 + haze * 0.70) * Opacity;
    // Premultiplied body, plus a small emissive bonus for the veins so they glow through the fog.
    return float4(tint * alpha + CoreColor * (vein * 0.18 * Opacity), alpha);
}

technique NitoMiasmaVolume
{
    pass Pass1 { PixelShader = compile ps_2_0 MiasmaPixel(); }
}
