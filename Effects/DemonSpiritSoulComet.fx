sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Trailing wisp behind the homing soul projectile. Procedural now — T_Aurax44 turned out to be the
// same "shape trap" as T_Windstreak3/T_trail12 (tips-skill §1): a discrete wisp graphic, mostly
// black canvas, not a tileable turbulence field, so scrolling through it as if it were noise sampled
// black almost everywhere and the trail was a barely-visible scribble.
//
// PrimarySampler is Turbulence_05 now (soft light/dark blobs — a smooth macro layer, unlike the fine
// cellular Vein_07 tried first, which read as jagged lightning), sampled TWICE at independent
// scale/phase (the Marilith multi-layer trick from VFX_ARSENAL.md) for the body. DetailSampler is
// Vein_07, demoted to a sparse high-threshold glint layer (soul sparks) rather than the shape.
float4 Comet(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    // Spine profile: pinched at the tail (along=0), fattest just behind the head (along~0.6), and
    // the head bulge itself sits toward along=1 where the actual projectile sprite is.
    float spine = saturate(along * 1.7) * saturate((1.0 - along) * 2.4);
    // Subtract-and-scale instead of the divide this started with (tips-skill §36: division is not
    // free). Same tapering read — narrower where `spine` is small — for about a third of the cost.
    float width = saturate((spine * 1.05 - across) * 5.0);

    float n1 = tex2D(PrimarySampler, float2(along * 1.5 - Time * 0.55, c.y * 1.3 + Time * 0.04)).r;
    float n2 = tex2D(PrimarySampler, float2(along * 2.7 - Time * 0.95, c.y * 2.3 + Time * 0.11)).r;
    float wisp = saturate(n1 * 0.85 + n2 * 0.55 - 0.30);
    float sparkle = tex2D(DetailSampler, float2(along * 2.6 - Time * 0.35, c.y * 2.0 + Time * 0.12)).r;

    float body = width * width * saturate(wisp + along * 0.40 - 0.02);
    float glints = width * saturate(sparkle * 2.6 - 1.65);
    float head = width * saturate(along - 0.68) * 2.8;

    // Noise-independent cutoff on all four quad edges.
    float fade = saturate(along * 5.2) * saturate((1.0 - along) * 3.4) * saturate((1.0 - across) * 3.0);
    body *= fade;
    glints *= fade;
    head *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.6));
    color = lerp(color, CoreColor, saturate(glints * 1.4 + head * 0.84));
    float alpha = saturate(body * 0.75 + glints * 0.55 + head * 0.55) * 0.95;
    return float4(color * (body * 0.85 + glints * 1.35 + head * 1.05), alpha * Opacity) * v;
}

float4 Brackets(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = abs(c - 0.5);
    float corner = saturate((0.045 - min(abs(p.x - 0.43), abs(p.y - 0.43))) * 28.0);
    float arms = saturate((0.17 - min(p.x, p.y)) * 10.0);
    float marker = corner * arms;
    float3 color = lerp(MidColor, CoreColor, Progress);
    return float4(color * marker * (0.65 + Progress * 0.75), marker * Opacity) * v;
}

// Detonation. The old version banded `box = max(abs(p.x), abs(p.y))` — a box SDF, tips-skill §31 —
// into a literal white square around the blast that never faded (full-bright at every Progress
// value), which is the "white line border" complaint. Replaced with ring-space shard noise: the
// grain is sampled along the shock ring's own direction rather than the quad's axes, so the shard
// pattern is round and different every detonation, never a rectangle.
//
// PrimarySampler was bound to `circle` (a blank white disc) and never actually read by the old
// shader either — dead weight in the call site, not a texture this technique needs. The fix samples
// DetailSampler (the one texture that was doing anything) twice at independent scale/phase instead
// of wiring up a new binding.
float4 Burst(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.42 + r * 0.30) + float2(0.5 + Time * 0.10, 0.5 - Time * 0.08)).r;
    float n2 = tex2D(DetailSampler, dir * (0.24 - r * 0.14) + float2(0.5 - Time * 0.06, 0.5 + Time * 0.05)).r;
    float grain = saturate(n1 * 0.85 + n2 * 0.60 - 0.24);

    // No extra quad cutoff needed: every term below is already bounded well inside the r=0.707 quad
    // corner (the ring's own outer edge maxes at 0.55 + 0.08 = 0.63 at Progress=1; smoke and flash
    // are bounded tighter still), so nothing here can reach the boundary regardless of Progress or
    // noise — unlike the deleted `square` term, which was full-bright at the boundary always.
    float blast = saturate((0.08 - abs(r - Progress * 0.55)) * 14.0);
    float shards = blast * saturate(grain * 2.3 - 1.05);
    float smoke = saturate((0.6 - r) * 2.6) * grain * (1.0 - Progress);
    float flash = saturate((0.22 - r) * 5.0) * (1.0 - Progress);

    float3 color = lerp(DarkColor, MidColor, saturate(blast * 1.1 + smoke * 0.5));
    color = lerp(color, CoreColor, saturate(shards * 1.2 + flash * 1.3));
    return float4(color * (smoke * 0.30 + blast * 0.95 + shards * 1.1 + flash * 1.35),
        saturate(smoke + blast + shards * 0.8 + flash) * Opacity) * v;
}

technique DemonSpiritSoulComet { pass P { PixelShader = compile ps_2_0 Comet(); } }
technique DemonSpiritExpiryBrackets { pass P { PixelShader = compile ps_2_0 Brackets(); } }
technique DemonSpiritSoulBurst { pass P { PixelShader = compile ps_2_0 Burst(); } }
