sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// The plague bomb itself — the one effect that already read well, kept in concept and polished.
// The blockiness came from sampling a 1080x1080 noise at 3.7 repeats across a 48-70px quad: each
// repeat covered ~19 screen pixels, so single noise cells were visible as chunks. Now the fissures
// are sampled in RING SPACE at a much lower repeat count, which wraps continuously around the orb
// and puts the cell size well above one screen pixel. A soft bloom spills past the orb so it lights
// its surroundings instead of ending at a circle.
float4 Coal(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);       // computed once and reused: r and dir both need it
    float r = len * 2.0;         // 0 at the centre, 1.0 at the quad edge
    float2 dir = p / max(len, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.30 + r * 0.16) + float2(0.5 + Time * 0.045, 0.5 - Time * 0.038)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.19 - r * 0.09) + float2(0.5 - Time * 0.031, 0.5 + Time * 0.053)).r;
    float rot = saturate(n1 * 0.90 + n2 * 0.60 - 0.26);

    float f = saturate((1.0 - r) * 3.5);
    f *= f;

    float orb = saturate((0.86 - r) * 5.2) * f;
    // Softer threshold than the old saturate(n * 1.8 - 0.85): the fissures grade in instead of
    // snapping on, which is the other half of the chunky look.
    float fissure = orb * saturate(rot * 1.45 - 0.42);
    float bloom = saturate((1.05 - r) * 1.5) * (0.10 + rot * 0.26) * f;
    // Coal started at 77/64 arithmetic slots; a sin() fuse pulse was the priciest decorative term
    // and came out. Progress still brightens the core as the fuse burns down.
    float core = saturate((0.44 - r) * 4.4) * (0.62 + Progress * 0.26 + Active * 0.20);

    float glow = orb * 0.35 + bloom * 0.55 + fissure * 1.15 + core * 1.30;
    float alpha = saturate(orb * 0.55 + bloom * 0.35 + fissure + core);

    float3 color = lerp(DarkColor, MidColor, saturate(orb * 0.75 + fissure * 1.35));
    color = lerp(color, CoreColor, saturate(core * 1.25 + fissure * 0.45));
    return float4(color * glow, alpha * Opacity) * v;
}

// Fuse smoke streaming off the thrown bomb. Alpha-blended dark billow, not a glow.
float4 Smoke(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.4) * saturate((1.0 - along) * 2.2);
    float width = saturate(1.0 - across / max(spine, 0.12));

    float n1 = tex2D(DetailSampler, float2(c.x * 1.5 - Time * 0.55, c.y * 2.0 + Time * 0.13)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 2.6 - Time * 0.85, c.y * 1.3 - Time * 0.09)).r;
    float billow = saturate(n1 * 0.95 + n2 * 0.65 - 0.34);

    float body = width * width * saturate(billow + 0.22);

    float fade = saturate(along * 4.0) * saturate((1.0 - along) * 3.0) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;

    float3 color = lerp(DarkColor, MidColor, saturate(billow * 0.9));
    // PREMULTIPLIED — see the note in BlackKnightSpearWake.fx. Returning straight colour here is
    // what drew the bomb's trail/blast smoke as a flat purple box with no faded edges.
    float alpha = body * Opacity * 0.85;
    return float4(color * alpha, alpha) * v;
}

// Detonation. The old version banded max(abs(p.x), abs(p.y)) — a BOX distance field — into a
// "boundary" term, which drew a literal white rectangle outline around every explosion. Deleted.
// Everything here is radial and every term is forced to zero before the quad edge.
float4 Blast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float len = length(p);
    float r = len * 2.0;

    // Billow cells sampled in ORDINARY uv space, NOT ring space. Ring space walks a straight line
    // through the texture as the radius varies, which smeared every feature into a radial spike and
    // made the old detonation look like a starburst. The sampling scale contracts as the ball grows,
    // so the cells appear to rush outward.
    // Linear contraction rather than a reciprocal, and scalar uv offsets rather than float2
    // constructions — both purely to fit the slot budget; the motion is indistinguishable.
    float expand = 1.0 - Progress * 0.45;
    float n1 = tex2D(DetailSampler, p * (expand * 2.1) + (0.5 + Time * 0.03)).r;
    float n2 = tex2D(PrimarySampler, p * (expand * 3.6) + (0.5 - Time * 0.04)).r;
    float churn = saturate(n1 * 0.95 + n2 * 0.65 - 0.26);

    // Fireball: fast out then stalling (sqrt), with a lumpy noise-perturbed edge. The perturbation
    // is deliberately gentle — erode it hard and the ball shatters into disconnected islands
    // instead of reading as one mass of fire.
    // Ease-out rather than sqrt(): same fast-then-stalling shape, several slots cheaper. Blast
    // started at 77/64, and sqrt plus a separate hot-core term were the expensive parts.
    // NO saturate() here: the argument is pure-uniform (no per-pixel term), which the wrapper's
    // D3DX effect compiler refuses to compile — reporting it as an error on the `technique` line.
    // Progress is already clamped 0..1 by every caller, so the saturate was redundant anyway.
    float grow = Progress * (2.0 - Progress) * 0.90;
    // One shared density modulation for the body and the rim, with the noise-independent quad
    // cutoff folded straight into it. Two separate churn ramps plus a separate cutoff cost slots
    // and are visually interchangeable here. NOTE: the wrapper's effect compiler is stricter than
    // standalone fxc for this file, so this needs headroom below 64, not just to fit it.
    float f = saturate((1.0 - r) * 2.4);
    float dens = (0.52 + churn * 0.82) * f * f;
    float ball = saturate((grow - r + (churn - 0.5) * 0.22) * 4.0) * dens;
    // Multiply-form instead of a divide (same 0.09..0.19 half-width, no reciprocal).
    float rim = saturate(1.0 - abs(r - grow) * (11.0 - churn * 5.5)) * dens;
    // Uniform folded in as a per-pixel bias: a pure-uniform saturate() argument will not compile.
    float flash = saturate((0.30 - r) * 3.5 - Progress * 3.4);

    float3 color = lerp(DarkColor, MidColor, saturate(ball * 1.4));
    color = lerp(color, CoreColor, saturate(rim * 0.9 + flash * 1.8));
    // Cools as it expands: violent and bright early, dark and smoky late.
    float energy = (ball * 1.15 + rim * 1.2) * (1.0 - Progress * 0.58) + flash * 2.4;
    return float4(color * energy, saturate(ball * 0.9 + rim * 0.8 + flash) * Opacity) * v;
}

technique BlackKnightMoonfuryCoal { pass P { PixelShader = compile ps_2_0 Coal(); } }
technique BlackKnightMoonfurySmoke { pass P { PixelShader = compile ps_2_0 Smoke(); } }
technique BlackKnightMoonfuryBlast { pass P { PixelShader = compile ps_2_0 Blast(); } }
