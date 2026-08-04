sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// The big collapsing death sigil (drawn 500px shrinking to 96px). This is the effect that read as a
// giant white ring with star rays: a hard `outer` band plus cross/diagonal bars from
// min(abs(p.x), abs(p.y)), additive, with a near-white core colour. All three are gone. What is left
// is a rot-veined rim eroded by ring-space noise over a dark violet interior.
float4 Seal(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.42 + r * 0.28) + float2(0.5 + Time * 0.030, 0.5 - Time * 0.024)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.26 - r * 0.15) + float2(0.5 - Time * 0.021, 0.5 + Time * 0.017)).r;
    float veins = saturate(n1 * 0.90 + n2 * 0.55 - 0.26);

    float radius = lerp(0.45, 0.16, Progress);
    // Rim thickness is itself noise-driven, so the boundary is organic rather than a drawn circle.
    float rim = saturate((0.035 + veins * 0.030 - abs(r - radius)) * 22.0) * (0.35 + veins * 0.75);
    float ticks = saturate((0.11 - abs(r - radius)) * 9.0) * saturate(veins * 2.0 - 0.88);
    float interior = saturate((radius - r) * 3.6) * (0.14 + veins * 0.30);

    float f = saturate((0.5 - r) * 7.0);
    f *= f;
    rim *= f;
    ticks *= f;
    interior *= f;

    float body = saturate(interior + ticks * 0.75);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate(rim * 1.15));
    return float4(color * (body * 0.65 + rim * 1.25), saturate(body * 0.80 + rim) * Opacity) * v;
}

// The aim line drawn from the knight to its stored target. The old version sampled T_trail12 — a
// small centred 4-point star flare — stretched down a long lane, so nearly the whole quad landed on
// its black background. Now fully procedural: a tight core with a soft halo, beaded along its length
// so it reads as a channelled thread rather than a drawn line.
float4 Thread(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float n1 = tex2D(DetailSampler, float2(c.x * 5.0 - Time * 0.90, 0.35 + Time * 0.05)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 9.0 - Time * 1.60, 0.65 - Time * 0.04)).r;
    float bead = saturate(n1 * 0.80 + n2 * 0.55 - 0.20);

    float core = saturate((0.20 - across) * 6.0);
    float halo = saturate((0.85 - across) * 1.5);

    // Progress ramps the thread in from the caster end, so it reads as reaching toward the target.
    float reach = saturate((Progress * 1.25 - along) * 5.0);
    float fade = saturate(along * 8.0) * saturate((1.0 - along) * 8.0) * saturate((1.0 - across) * 3.0);

    float thread = core * (0.55 + bead * 0.65) * reach * fade;
    float glow = halo * halo * (0.20 + bead * 0.30) * reach * fade;

    float3 color = lerp(DarkColor, MidColor, saturate(glow * 2.0));
    color = lerp(color, CoreColor, saturate(thread * 1.4));
    return float4(color * (glow * 0.55 + thread * 1.35), saturate(glow * 0.55 + thread) * Opacity) * v;
}

// Ragged spectral trail behind the death-strike and cursed-breath projectiles.
float4 SpectralTrail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.6) * saturate((1.0 - along) * 2.4);
    float width = saturate(1.0 - across / max(spine, 0.09));

    float n1 = tex2D(DetailSampler, float2(c.x * 1.8 - Time * 0.95, c.y * 2.8 + Time * 0.09)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 3.1 - Time * 1.45, c.y * 1.7 - Time * 0.12)).r;
    float rot = saturate(n1 * 0.95 + n2 * 0.60 - 0.34);

    // Ragged: the noise cuts into the body hard, so the silhouette frays instead of tapering evenly.
    float body = width * saturate(rot * 1.5 + along * 0.35 - 0.18);
    float wisp = width * width * saturate(rot * 2.2 - 1.05);

    float fade = saturate(along * 4.5) * saturate((1.0 - along) * 3.4) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    wisp *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.30));
    color = lerp(color, CoreColor, saturate(wisp * 1.25));
    return float4(color * (body * 0.68 + wisp * 1.15), saturate(body * 0.70 + wisp * 0.50) * Opacity) * v;
}

technique BlackKnightDeathSeal { pass P { PixelShader = compile ps_2_0 Seal(); } }
technique BlackKnightAimThread { pass P { PixelShader = compile ps_2_0 Thread(); } }
technique BlackKnightDeathTrail { pass P { PixelShader = compile ps_2_0 SpectralTrail(); } }
