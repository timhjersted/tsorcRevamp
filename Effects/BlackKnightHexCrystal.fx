sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Plague sigil around the dormant hex crystal. The old version built its spokes from
// min(abs(p.x - p.y), abs(p.x + p.y)) — a box diagonal, which draws hard straight bars. Angular
// variation now comes from noise sampled in ring-space, which is both cheaper and seamless.
float4 Seal(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.40 + r * 0.26) + float2(0.5 + Time * 0.035, 0.5 - Time * 0.028)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.25 - r * 0.14) + float2(0.5 - Time * 0.024, 0.5 + Time * 0.019)).r;
    float rot = saturate(n1 * 0.85 + n2 * 0.60 - 0.24);

    float radius = lerp(0.38, 0.24, Progress);
    float dist = abs(r - radius);

    // Blurry, soft feathered radial glow — no hard step edges
    float softGlow = smoothstep(0.24, 0.0, dist) * (0.35 + rot * 0.65);
    float softCenter = smoothstep(radius, 0.0, r) * (0.20 + rot * 0.30);

    float f = smoothstep(0.5, 0.05, r);
    softGlow *= f;
    softCenter *= f;

    float body = saturate(softCenter + softGlow * 0.7);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, softGlow * softGlow);

    float alpha = saturate(body * 0.85 + softGlow * 0.5) * Opacity;
    return float4(color * alpha, alpha) * v;
}

// Trail behind the live crystal. Procedural comet: a head bulge with a wake tapering off behind it.
float4 Comet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.8) * saturate((1.0 - along) * 2.6);
    float width = saturate(1.0 - across / max(spine, 0.07));

    float n1 = tex2D(DetailSampler, float2(c.x * 2.1 - Time * 1.15, c.y * 2.6 + Time * 0.07)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 3.5 - Time * 1.80, c.y * 1.6 - Time * 0.10)).r;
    float churn = saturate(n1 * 0.88 + n2 * 0.55 - 0.28);

    float body = width * width * saturate(churn + along * 0.42 - 0.08);
    float head = width * saturate(along - 0.66) * 2.8;

    float fade = saturate(along * 5.5) * saturate((1.0 - along) * 3.8) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    head *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.35));
    color = lerp(color, CoreColor, saturate(head * 1.30));
    return float4(color * (body * 0.72 + head * 1.25), saturate(body * 0.62 + head * 0.55) * Opacity) * v;
}

// Crystal shattering. Cross and diagonal shard bars replaced by an expanding rim whose shards are
// carved out of ring-space noise, so no two detonations look like the same stamp.
float4 Shatter(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.44 + r * 0.30) + float2(0.5 + Time * 0.05, 0.5 - Time * 0.04)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.28 - r * 0.17) + float2(0.5 - Time * 0.03, 0.5 + Time * 0.06)).r;
    float grain = saturate(n1 * 0.85 + n2 * 0.60 - 0.22);

    float radius = 0.08 + Progress * 0.36;
    float rim = saturate((0.06 - abs(r - radius)) * 17.0) * (0.35 + grain * 0.85);
    float shards = saturate((0.20 - abs(r - radius)) * 6.0) * saturate(grain * 2.1 - 0.90);
    // Uniform folded in as a per-pixel bias: a pure-uniform saturate() argument will not compile.
    float flash = saturate((0.22 - r) * 4.6 - Progress * 2.4);

    float f = saturate((0.5 - r) * 7.0);
    f *= f;
    rim *= f;
    shards *= f;

    float body = saturate(rim + shards * 0.9);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate(flash * 1.3 + rim * 0.6));
    return float4(color * (body * 1.10 + flash * 1.30), saturate(body + flash * 0.8) * Opacity) * v;
}

technique BlackKnightHexSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
technique BlackKnightHexComet { pass P { PixelShader = compile ps_2_0 Comet(); } }
technique BlackKnightHexShatter { pass P { PixelShader = compile ps_2_0 Shatter(); } }
