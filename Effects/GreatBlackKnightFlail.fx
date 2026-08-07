sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);
float3 DarkColor, MidColor, CoreColor;
float Opacity, Time, Progress, Active, Direction;
float2 DrawSize, PrimaryTextureSize;

// Plague corona around the flail head. The old version drew a crisp analytic shell at r = 0.38,
// which read as a hard ellipse pasted behind the sprite; the shell is gone. What is left is a dark
// rotting core with a corona whose reach is itself noise-driven, so the edge is organic everywhere.
float4 FlailHead(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    // Ring-space: continuous all the way around, no tiling grid and no atan2 seam.
    float n1 = tex2D(DetailSampler, dir * (0.38 + r * 0.34) + float2(0.5 + Time * 0.043, 0.5 - Time * 0.037)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.24 - r * 0.15) + float2(0.5 - Time * 0.029, 0.5 + Time * 0.051)).r;
    float rot = saturate(n1 * 0.85 + n2 * 0.60 - 0.24);

    // Noise-modulated falloff distance: the corona reaches further in some directions than others,
    // which kills the hard rim without eroding the shape after the fact.
    float reach = 0.20 + rot * 0.22;
    float corona = saturate((reach - r) * 4.6) * (0.35 + rot * 0.85);
    // Pulse folded straight into the core rather than kept as its own term: FlailHead started at
    // 69/64 arithmetic slots. The separate pulse variable, a corona*corona bloom and the Active
    // brightening were the decorative parts; the shape math is untouched. Empowered still reads
    // clearly because the C# call varies both draw size (62 vs 50) and opacity (0.9 vs 0.54).
    float core = saturate((0.15 - r) * 7.0) * (0.70 + rot * 0.30 + 0.16 * sin(Time * 7.0));

    float f = saturate((0.5 - r) * 7.0);
    f *= f;
    corona *= f;
    core *= f;   // core lives inside r < 0.15 where f is already 1.0, so sharing it is free

    float3 color = lerp(DarkColor, MidColor, saturate(corona * 1.25));
    color = lerp(color, CoreColor, saturate(core * 1.3));
    return float4(color * (corona * 0.65 + core * 1.25), saturate(corona * 0.85 + core) * Opacity) * v;
}

// Motion smear behind the swinging head: the spear wake's wider, heavier sibling. Procedural, so
// nothing here can degrade into the stretched T_Windstreak3 blob the old version drew.
float4 FlailTrail(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.5) * saturate((1.0 - along) * 2.8);
    float width = saturate(1.0 - across / max(spine, 0.08));

    float n1 = tex2D(DetailSampler, float2(c.x * 1.7 - Time * 1.05, c.y * 2.2 + Time * 0.05)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 2.9 - Time * 1.70, c.y * 1.4 - Time * 0.08)).r;
    float churn = saturate(n1 * 0.90 + n2 * 0.55 - 0.30);

    float body = width * width * saturate(churn + along * 0.40 - 0.06);
    float ember = width * saturate(along - 0.62) * 2.4 * saturate(churn * 1.6 - 0.45) * Active;

    float fade = saturate(along * 5.0) * saturate((1.0 - along) * 3.6) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    ember *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.3));
    color = lerp(color, CoreColor, saturate(ember * 1.5));
    // PREMULTIPLIED — this technique is alpha-blended; see BlackKnightSpearWake.fx.
    float alpha = saturate(body * 0.62 + ember * 0.45) * Opacity;
    return float4(color * alpha, alpha) * v;
}

// Ground shockwave telegraph. The collapsing ring is the readable threat, so it stays; the static
// outer ring at r = 0.46 (a second hard ellipse) is gone and the survivor is eroded by noise.
float4 FlailPulse(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float2 p = c - 0.5;
    float r = length(p);
    float2 dir = p / max(r, 0.0005);

    float n1 = tex2D(DetailSampler, dir * (0.42 + r * 0.24) + float2(0.5 + Time * 0.031, 0.5 - Time * 0.026)).r;
    float n2 = tex2D(PrimarySampler, dir * (0.27 - r * 0.13) + float2(0.5 - Time * 0.022, 0.5 + Time * 0.018)).r;
    float grain = saturate(n1 * 0.80 + n2 * 0.60 - 0.22);

    // Inward collapse: the shrinking radius is the tell, and outrunning it is the counterplay.
    float radius = lerp(0.44, 0.13, Progress);
    float ring = saturate((0.055 - abs(r - radius)) * 20.0) * (0.40 + grain * 0.80);
    float haze = saturate((radius - r) * 3.4) * saturate(grain * 1.5 - 0.42);

    float f = saturate((0.5 - r) * 7.0);
    ring *= f * f;
    haze *= f * f;

    float3 color = lerp(DarkColor, MidColor, saturate(haze * 1.4 + ring * 0.5));
    color = lerp(color, CoreColor, saturate(ring * ring * 1.2));
    return float4(color * (haze * 0.45 + ring * 1.30), saturate(haze * 0.7 + ring) * Opacity) * v;
}

// Falling ember shed by the flail: a small teardrop with a wisp trailing it.
float4 FlailEmber(float4 v : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 2.6) * saturate((1.0 - along) * 2.2);
    float width = saturate(1.0 - across / max(spine, 0.10));

    float n1 = tex2D(DetailSampler, float2(c.x * 2.4 - Time * 0.85, c.y * 2.0 + Time * 0.11)).r;
    float n2 = tex2D(PrimarySampler, float2(c.x * 1.3 + Time * 0.30, c.y * 3.1 - Time * 0.07)).r;
    float grain = saturate(n1 * 0.80 + n2 * 0.65 - 0.28);

    float body = width * width * saturate(grain + 0.30);
    float head = width * saturate(along - 0.70) * 3.0;

    float fade = saturate(along * 5.5) * saturate((1.0 - along) * 4.0) * saturate((1.0 - across) * 3.2);
    body *= fade * fade;
    head *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.5));
    color = lerp(color, CoreColor, saturate(head * 1.4));
    return float4(color * (body * 0.70 + head * 1.20), saturate(body * 0.55 + head * 0.60) * Opacity) * v;
}

technique GreatBlackKnightFlailHead { pass P { PixelShader = compile ps_2_0 FlailHead(); } }
technique GreatBlackKnightFlailTrail { pass P { PixelShader = compile ps_2_0 FlailTrail(); } }
technique GreatBlackKnightFlailPulse { pass P { PixelShader = compile ps_2_0 FlailPulse(); } }
technique GreatBlackKnightFlailEmber { pass P { PixelShader = compile ps_2_0 FlailEmber(); } }
