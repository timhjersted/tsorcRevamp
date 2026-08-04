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
    return float4(color, body * Opacity * 0.85) * v;
}

// Detonation. The old version banded max(abs(p.x), abs(p.y)) — a BOX distance field — into a
// "boundary" term, which drew a literal white rectangle outline around every explosion. Deleted.
// Everything here is radial and every term is forced to zero before the quad edge.
float4 Blast(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.46 + r * 0.28) + float2(0.5 + Time * 0.06, 0.5 - Time * 0.05)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.29 - r * 0.16) + float2(0.5 - Time * 0.04, 0.5 + Time * 0.07)).r;
    float churn = saturate(n1 * 0.90 + n2 * 0.60 - 0.24);

    float radius = 0.09 + Progress * 0.36;
    float wave = saturate((0.065 + churn * 0.035 - abs(r - radius)) * 16.0) * (0.40 + churn * 0.75);
    float cloud = saturate((radius + 0.10 - r) * 3.0) * saturate(churn * 1.7 - 0.55);
    // Uniform folded in as a per-pixel bias: a pure-uniform saturate() argument will not compile.
    float flash = saturate((0.24 - r) * 4.4 - Progress * 2.6);

    float f = saturate((0.5 - r) * 7.0);
    f *= f;
    wave *= f;
    cloud *= f;

    float body = saturate(cloud * 0.9 + wave);
    float3 color = lerp(DarkColor, MidColor, body);
    color = lerp(color, CoreColor, saturate(flash * 1.35 + wave * 0.55));
    return float4(color * (body * 1.05 + flash * 1.35), saturate(body + flash * 0.85) * Opacity) * v;
}

technique BlackKnightMoonfuryCoal { pass P { PixelShader = compile ps_2_0 Coal(); } }
technique BlackKnightMoonfurySmoke { pass P { PixelShader = compile ps_2_0 Smoke(); } }
technique BlackKnightMoonfuryBlast { pass P { PixelShader = compile ps_2_0 Blast(); } }
