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

// Soul-wisp streaming off a screaming skull. Fully procedural: the old version took its shape from
// T_Windstreak3, which is a VERTICAL TEARDROP BLOB rather than a streak, so stretching it down the
// lane produced the flat grey lozenges.
//
// Direction carries a PER-SKULL random phase (from projectile.identity). Without it every one of the
// 100+ skulls in the death fountain samples the identical pattern in local UV space and the swarm
// reads as one stamp repeated. Active = the skull's empowered flag.
float4 SoulTrailPixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;   // 0 on the centreline, 1 at the rim
    float along = c.x;                     // 0 at the tail, 1 at the skull

    // Ribbon: pinched at the tail, widest just behind the skull, closed at both ends.
    float spine = saturate(along * 1.5) * saturate((1.0 - along) * 3.0);
    float width = saturate(1.0 - across / max(spine, 0.07));

    // Per-instance phase offsets both layers so no two skulls share a pattern.
    float2 phase = float2(Direction * 7.3, Direction * 3.1);
    float n1 = tex2D(PrimarySampler, float2(c.x * 1.7 - Time * 0.85, c.y * 2.3) + phase).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 3.1 - Time * 1.30, c.y * 1.6) + phase).r;
    float wisp = saturate(n1 * 0.90 + n2 * 0.60 - 0.30);

    // The body frays: noise cuts into it hard so the silhouette breaks up toward the tail instead
    // of tapering as a clean lozenge.
    float body = width * width * saturate(wisp + along * 0.45 - 0.10);
    float soul = width * saturate(wisp * 2.1 - 1.0) * (0.45 + Active * 0.55);

    // Noise-independent cutoff on all four quad edges.
    float fade = saturate(along * 5.0) * saturate((1.0 - along) * 3.5) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    soul *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.4));
    color = lerp(color, CoreColor, saturate(soul * 1.35));

    float alpha = saturate(body * 0.70 + soul * 0.55) * Opacity;
    return float4(color * (body * 0.75 + soul * 1.25), alpha);
}

technique VesselSoulTrail
{
    pass SoulTrailPass { PixelShader = compile ps_2_0 SoulTrailPixel(); }
}
