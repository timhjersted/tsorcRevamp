sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;
// Pixel grid, PRE-DIVIDED on the C# side by EnemyVFX.Draw: xy = block count across the quad,
// zw = its reciprocal. Set via ?.SetValue so techniques that ignore it are unaffected.
//
// Computing this in HLSL from a quad-size uniform (the ElandToxicVFX form) costs 12 slots here, not
// the ~4 it looks like: a raw ps_2_0 entry point has no preshader, so `max()` and the reciprocal are
// emitted per pixel even though they depend only on uniforms. Measured by compiling with and without
// the filter — 56 vs 68 arithmetic. These techniques cannot afford that, hence the C#-side divide.
float4 PixelGrid;

// 2px blocks at 1x gameplay zoom, measured against the real on-screen quad (NOT DrawSize, which is
// the source TEXTURE size under EnemyVFX's legacy UV contract).
float2 PixelateSealUV(float2 uv)
{
    return (floor(uv * PixelGrid.xy) + 0.5) * PixelGrid.zw;
}

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

// ─── Black-flame seal candidates ──────────────────────────────────────────────────────────────
// Both replace Seal()'s smooth ring. The reason that one reads as "basically a circle" is that its
// noise only ever modulates BRIGHTNESS (`rot` scales the glow) — the silhouette itself is a perfect
// `abs(r - radius)` circle at every angle. Here the ring's radius is noise-driven PER ANGLE, so the
// shape licks in and out (the noise-modulated falloff-distance trick from vfx-pipeline).
//
// "Black flame" is real here, not just dark colours: under premultiplied AlphaBlend the returned
// `float4(rgb * a, a)` with rgb≈0 and a>0 evaluates to `dst * (1 - a)`, which genuinely DARKENS what
// is behind it. The body subtracts light and only the rim adds it — that contrast is the effect.
//
// Both preserve the shrink: `radius` still lerps 0.38 -> 0.24 on Progress, and the C# call site
// still swaps the quad 74x74 (dormant) -> 48x40 (active).

// A — Black Corona. Ragged, fire-like: deep tongues, broad soft halo. More motion, less symmetry.
float4 SealBlackCorona(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateSealUV(c);
    float2 p = c - 0.5;
    // rsqrt instead of a 2-component divide, and the radial term dropped from the sample scales.
    // Purely ANGULAR noise is what this technique actually wants: the shape comes from `lick`
    // deforming the radius per angle, so radial variation in the sample only cost slots.
    float r2 = dot(p, p);
    float r = sqrt(r2);
    float2 dir = p * rsqrt(max(r2, 0.0000001));

    float n1 = tex2D(DetailSampler, dir * 0.46 + float2(0.5 + Time * 0.085, 0.5 - Time * 0.062)).r;
    float n2 = tex2D(PrimarySampler, dir * 0.74 + float2(0.5 - Time * 0.051, 0.5 + Time * 0.097)).r;
    float flame = saturate(n1 * 0.88 + n2 * 0.56 - 0.20);

    // The silhouette itself, per angle — this is what stops it being a circle.
    float d = r - (lerp(0.38, 0.24, Progress) + (flame - 0.44) * 0.21);

    // quadFade is applied ONCE to alpha rather than to each component. Because the return is
    // premultiplied (color * alpha), folding it into alpha fades the colour identically for ~4
    // fewer slots — this shader is budget-bound at ps_2_0 (see the header note).
    float quadFade = saturate((0.5 - r) * 4.4);
    // abs(d) computed once and shared by both edge terms.
    float ad = abs(d);
    float body = saturate(-d * 6.4);                                  // light-eating interior
    float rim = saturate((0.06 - ad) * 14.0) * (0.28 + flame * 0.86);
    float glow = saturate((0.19 - ad) * 3.0) * flame;

    // Additive accumulation rather than a lerp chain: cheaper, and the squared term confines the
    // hot core to the brightest rim naturally (vfx-pipeline "Additive x + x*x colour layering").
    //
    // No DarkColor term. It was `DarkColor * 0.22`, and DarkColor here is HexGoldDark (8,4,12) —
    // already essentially black, so at 0.22 it contributed nothing visible while costing slots the
    // pixel filter needs. The dark interior is carried by ALPHA instead: premultiplied output with
    // rgb~0 and a>0 resolves to dst * (1 - a), which is what actually makes it eat light.
    float3 color = MidColor * (rim * 0.9 + glow * 0.5) + CoreColor * rim * rim * 1.15;
    float alpha = saturate(body * 0.80 + rim * 0.95 + glow * 0.34) * quadFade * Opacity;
    return float4(color * alpha, alpha) * v;
}

// B — Void Halo. Cleaner and more sigil-like: a near-black core with a tight, intense light ring.
// Tongues live only on the OUTER edge, so the halo reads as a deliberate object rather than as fire.
float4 SealVoidHalo(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    c = PixelateSealUV(c);
    float2 p = c - 0.5;
    // Same rsqrt / angular-only simplification as Corona above, for the same budget reason.
    float r2 = dot(p, p);
    float r = sqrt(r2);
    float2 dir = p * rsqrt(max(r2, 0.0000001));

    float n1 = tex2D(DetailSampler, dir * 0.58 + float2(0.5 + Time * 0.043, 0.5 - Time * 0.036)).r;
    float n2 = tex2D(PrimarySampler, dir * 0.27 + float2(0.5 - Time * 0.029, 0.5 + Time * 0.055)).r;
    float flame = saturate(n1 * 0.80 + n2 * 0.62 - 0.24);

    // Much shallower lick than Corona: the ring stays legible as a deliberate object.
    float d = r - (lerp(0.38, 0.24, Progress) + (flame - 0.46) * 0.085);

    float quadFade = saturate((0.5 - r) * 5.0);
    float ad = abs(d);                                                 // shared by both edge terms
    float voidCore = saturate(-d * 4.2);                               // deep interior, eats light
    float halo = saturate((0.038 - ad) * 22.0) * (0.55 + flame * 0.55);
    float bloom = saturate((0.13 - ad) * 5.2) * (0.20 + flame * 0.44);

    // Same budget reasoning as Corona, including dropping the near-black DarkColor term: the void is
    // carried by alpha under premultiplied blending, not by painting a dark colour.
    float3 color = MidColor * bloom * 1.05 + CoreColor * halo * 1.45;
    float alpha = saturate(voidCore * 0.86 + halo + bloom * 0.30) * quadFade * Opacity;
    return float4(color * alpha, alpha) * v;
}

technique BlackKnightHexSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
technique BlackKnightHexSealCorona { pass P { PixelShader = compile ps_2_0 SealBlackCorona(); } }
technique BlackKnightHexSealVoid { pass P { PixelShader = compile ps_2_0 SealVoidHalo(); } }
technique BlackKnightHexComet { pass P { PixelShader = compile ps_2_0 Comet(); } }
technique BlackKnightHexShatter { pass P { PixelShader = compile ps_2_0 Shatter(); } }
