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

// Nito's mantle — the shroud of corpses and grave-air hanging off a 250x320 body.
//
// The old shell (`saturate((0.51 - radius) * 4.3)`) was still 0.043 at the quad edge midpoint, so it
// was being sliced flat by the boundary, and it was additive: a "mantle of corpses" that made him
// GLOW. Premultiplied over AlphaBlend now, so he sits in a pool of gloom with soul-lights caught in
// it, which is what the silhouette in the screenshot wants.
//
// Direction = flow (+1 normally, -1 while he inhales during the phase transition and the Death Nova
// channel, so the shroud is visibly drawn inward instead of streaming off him).
// Active = phase two. Progress = the attack pulse (0 while idle).
float4 SoulMantlePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p) * 2.0;                 // 1.0 at the axis edges, 1.41 in the corners
    float flow = Direction;

    // Two layers drifting vertically against each other; both reverse with `flow`.
    float n1 = tex2D(PrimarySampler, float2(c.x * 1.7, c.y * 1.3 - Time * 0.100 * flow)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 2.6 + 0.31, c.y * 2.1 - Time * 0.170 * flow)).r;
    float cloth = saturate(n1 * 0.90 + n2 * 0.55 - 0.32);

    // Hollow shroud: dense around the silhouette, open through the middle so the boss sprite still
    // reads through it. The outer radius is noise-driven and tops out at 0.74 + 0.16 = 0.90, so the
    // quad edges and corners are provably never touched.
    // The inner cut is wide on purpose (it was 0.12, which only cleared a small disc and left the
    // middle of the quad filled): the boss sprite is 250x320 and has to read THROUGH this.
    float shellR = 0.74 + cloth * 0.16;
    float shell = saturate((shellR - r) * 3.0) * saturate((r - 0.30) * 2.6);

    // Ragged hem: the lower half hangs in torn strips instead of closing into a neat ellipse.
    float hem = saturate((c.y - 0.42) * 2.2) * saturate(cloth * 1.80 - 0.35);
    float body = shell * saturate(cloth + 0.35) + shell * hem * 0.55;
    // Soul-lights caught in the cloth. Phase two makes them numerous and hot.
    float glint = saturate(n2 * 2.50 - 1.55) * shell * (0.35 + Active * 0.90);
    // Soul threads drawn up through the cloth — vertical filaments in the lower shroud, which is
    // what stops the hem reading as a flat wash of noise.
    float thread = saturate(n1 * 2.20 - 1.05) * saturate((c.y - 0.30) * 2.0) * shell;
    float pulse = 0.82 + Progress * 0.28;

    float3 tint = lerp(DarkColor, MidColor, saturate(body * 0.95 + thread * 0.8));
    float alpha = saturate(body * 0.88 + thread * 0.35) * Opacity;
    return float4(tint * alpha + CoreColor * ((glint * 0.32 + thread * 0.10) * Opacity * pulse), alpha);
}

technique NitoSoulMantle
{
    pass Pass1 { PixelShader = compile ps_2_0 SoulMantlePixel(); }
}
