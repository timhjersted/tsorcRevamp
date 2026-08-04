sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Displaced air behind a moving weapon: wind, not a glow. Fully procedural — the silhouette is a
// tapered lens built from the quad UV and the noise only erodes it, so there is no shape texture to
// stretch into a hard white lozenge the way the old T_Windstreak3 lookup did (that file is a
// VERTICAL teardrop, not a streak). No hard edges anywhere: every boundary is reached through a
// noise-independent fade.
float4 Wake(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;   // 0 on the centreline, 1 at the rim
    float along = c.x;                     // 0 at the tail, 1 at the leading edge

    // Lens: widest just behind the head, pinched to nothing at both ends.
    float spine = saturate(along * 1.7) * saturate((1.0 - along) * 3.2);
    float width = saturate(1.0 - across / max(spine, 0.06));

    // Two layers streaming backwards at different rates, so the interior churns instead of sliding.
    float n1 = tex2D(DetailSampler, float2(c.x * 1.9 - Time * 1.35, c.y * 2.4 + Time * 0.06)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 3.3 - Time * 2.10, c.y * 1.5 - Time * 0.09)).r;
    float streak = saturate(n1 * 0.85 + n2 * 0.55 - 0.26);

    // Density gathers at the leading edge and frays out behind it.
    float body = width * width * saturate(streak + along * 0.45 - 0.10);
    float edge = width * saturate(along - 0.58) * 2.4 * streak;

    // Noise-independent cutoff: provably zero before every quad boundary, in every direction.
    float fade = saturate(along * 6.0) * saturate((1.0 - along) * 4.0) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    edge *= fade;

    float3 color = lerp(MidColor, CoreColor, saturate(body * 0.7 + edge * 1.2));
    return float4(color, saturate(body * 0.50 + edge * 0.40) * Opacity) * v;
}

// Spear landing / parry impact. The old version drew a literal plus-sign from
// min(abs(p.x), abs(p.y)); this is a true radial ring whose rim is eroded by ring-space noise.
float4 Impact(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    // Ring-space sampling: walking around the impact traces a circle through texture space, so it is
    // continuous by construction — no tiling grid, no atan2 wrap seam, ~4 slots instead of ~14.
    float n1 = tex2D(DetailSampler, dir * (0.40 + r * 0.30) + float2(0.5 + Time * 0.05, 0.5 - Time * 0.04)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.26 - r * 0.16) + float2(0.5 - Time * 0.03, 0.5 + Time * 0.06)).r;
    float grain = saturate(n1 * 0.8 + n2 * 0.6 - 0.22);

    float radius = 0.10 + Progress * 0.32;
    float ring = saturate((0.075 - abs(r - radius)) * 15.0) * (0.40 + grain * 0.80);
    float spokes = saturate((0.17 - abs(r - radius)) * 7.0) * saturate(grain * 1.9 - 0.72);
    // Uniform folded in as a per-pixel bias: a pure-uniform saturate() argument will not compile.
    float flash = saturate((0.20 - r) * 5.0 - Progress * 2.2);

    float f = saturate((0.5 - r) * 7.0);
    float total = (ring + spokes * 0.8 + flash) * f * f;

    float3 color = lerp(DarkColor, MidColor, saturate(ring + spokes));
    color = lerp(color, CoreColor, saturate(flash * 1.4 + ring * ring * 0.7));
    return float4(color * (total + flash * 0.6), saturate(total) * Opacity) * v;
}

technique BlackKnightSpearWake { pass P { PixelShader = compile ps_2_0 Wake(); } }
technique BlackKnightSpearImpact { pass P { PixelShader = compile ps_2_0 Impact(); } }
