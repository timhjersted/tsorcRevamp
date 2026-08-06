sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Ground fissure the gravefall erupts from. The old version drew a straight slit with a hard edge
// band at a fixed |y|, which read as two drawn lines. Here the falloff DISTANCE is noise-driven, so
// the tear reaches further in some places than others and the edge is organic rather than eroded
// after the fact — the Marilith firewall trick from the arsenal.
float4 Tear(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;

    float n1 = tex2D(DetailSampler, float2(c.x * 2.6 + Time * 0.07, c.y * 4.5 - Time * 0.05)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 4.9 - Time * 0.05, c.y * 2.2 + Time * 0.09)).r;
    float rot = saturate(n1 * 0.90 + n2 * 0.55 - 0.26);

    // Half-thickness of the tear, widening as the telegraph runs and varying along its length.
    float half = 0.035 + Progress * 0.030 + rot * 0.045;
    float lengthwise = saturate((0.46 - abs(p.x)) * 5.0);
    float slit = saturate((half - abs(p.y)) / max(half, 0.01)) * lengthwise;

    // Rim taken from the same distance field, so it stays a consistent width at any draw size
    // instead of collapsing to sub-pixel the way a fixed-texel outline would.
    float lip = saturate((0.018 - abs(abs(p.y) - half)) * 42.0) * lengthwise * (0.45 + rot * 0.55);
    float ash = slit * (0.25 + rot * 0.45);

    float3 color = lerp(DarkColor, MidColor, saturate(ash * 1.6));
    color = lerp(color, CoreColor, saturate(lip * 1.25));
    return float4(color * (ash * 0.60 + lip * 1.40), saturate(ash * 0.85 + lip) * Opacity) * v;
}

// Rot-cloud streak trailing the cursed breath projectile.
float4 Trail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.5) * saturate((1.0 - along) * 2.5);
    float width = saturate(1.0 - across / max(spine, 0.10));

    float n1 = tex2D(DetailSampler, float2(c.x * 1.6 - Time * 0.80, c.y * 2.3 + Time * 0.10)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 2.7 - Time * 1.25, c.y * 1.5 - Time * 0.07)).r;
    float billow = saturate(n1 * 0.95 + n2 * 0.60 - 0.32);

    float body = width * width * saturate(billow + along * 0.30);
    float mote = width * saturate(billow * 2.3 - 1.15);

    float fade = saturate(along * 4.5) * saturate((1.0 - along) * 3.2) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    mote *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.25));
    color = lerp(color, CoreColor, saturate(mote * 1.2));
    // PREMULTIPLIED — this technique is alpha-blended; see BlackKnightSpearWake.fx.
    float alpha = saturate(body * 0.60 + mote * 0.45) * Opacity;
    return float4(color * alpha, alpha) * v;
}

technique BlackKnightGraveTear { pass P { PixelShader = compile ps_2_0 Tear(); } }
technique BlackKnightGraveTrail { pass P { PixelShader = compile ps_2_0 Trail(); } }
