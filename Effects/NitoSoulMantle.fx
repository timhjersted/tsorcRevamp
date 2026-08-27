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
    c = PixelateNitoUV(c);
    float2 p = c - 0.5;
    float r = length(p) * 2.0;
    float flow = Direction;

    // Two layers drifting vertically against each other; both reverse with `flow`.
    float n1 = tex2D(PrimarySampler, float2(c.x * 1.46, c.y * 1.05 - Time * 0.083 * flow)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 2.15 + 0.31, c.y * 1.72 - Time * 0.145 * flow)).r;
    float cloth = saturate(n1 * 0.92 + n2 * 0.48 - 0.34);

    // Hollow shroud: dense around the silhouette, open through the middle so the boss sprite still
    // reads through it. The outer radius is noise-driven and tops out at 0.74 + 0.16 = 0.90, so the
    // quad edges and corners are provably never touched.
    // The inner cut is wide on purpose (it was 0.12, which only cleared a small disc and left the
    // middle of the quad filled): the boss sprite is 250x320 and has to read THROUGH this.
    float shellR = 0.79 + cloth * 0.15;
    float shell = saturate((shellR - r) * 2.8) * saturate((r - 0.34) * 3.0);

    // Ragged hem: the lower half hangs in torn strips instead of closing into a neat ellipse.
    float strips = saturate((c.y - 0.38) * 2.3) * saturate(n1 * 2.0 - 0.76) * shell;
    float body = shell * saturate(cloth + 0.32) + strips * 0.58;
    // Soul-lights caught in the cloth. Phase two makes them numerous and hot.
    float glint = saturate(n2 * 2.78 - 1.84) * shell * (0.28 + Active * 0.72);
    float pulse = 0.82 + Progress * 0.28;

    float3 tint = lerp(DarkColor, MidColor,
        saturate(body * 0.88 + strips * Active * 0.34));
    float alpha = saturate(body * 0.94 + strips * Active * 0.38) * Opacity;
    return float4(tint * alpha + CoreColor * (glint * 0.25 * Opacity * pulse), alpha);
}

technique NitoSoulMantle
{
    pass Pass1 { PixelShader = compile ps_2_0 SoulMantlePixel(); }
}
