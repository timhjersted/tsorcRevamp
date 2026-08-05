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

// Compression front of a charging body.
//
// The old version built this from `box = max(abs(p.x), abs(p.y))` — a BOX distance field — banded
// into `edge = abs(box - 0.94)`, which is what drew the hard pink rectangle around the boss. That is
// gone completely, along with any reference to the 200x300 contact AABB: by the owner's decision
// this is now free-form atmosphere and no longer advertises the hitbox.
//
// The quad is rotated to the charge axis, so +x is the direction of travel: the field piles up ahead
// of the body and frays off behind it. Direction is -1 on the void plunge, +1 on the lunge.
float4 RamCorePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    // Elongated along the travel axis. Squashing x before taking the length turns the circle into an
    // ellipse without needing a second distance term.
    float2 p = (c - 0.5) * float2(1.35, 2.0);
    float r = length(p);
    float along = c.x - 0.5;

    float n1 = tex2D(PrimarySampler, c * float2(1.6, 2.2) + float2(-Time * 0.55, Time * 0.09)).r;
    float n2 = tex2D(DetailSampler, c * float2(2.7, 1.4) + float2(-Time * 0.85, -Time * 0.06)).r;
    float churn = saturate(n1 * 0.90 + n2 * 0.60 - 0.28);

    // Pressure builds toward the leading edge rather than sitting evenly around the body.
    float lead = saturate(0.5 + along * 1.8);
    float body = saturate(1.0 - r * 1.6) * (0.35 + churn * 0.9) * (0.45 + lead * 0.85);
    // Hot compression skin on the front face only.
    float skin = saturate(1.0 - abs(r - 0.42) * 3.4) * lead * lead * (0.4 + churn * 0.8);

    // Noise-independent cutoff.
    float edge = saturate((1.0 - r) * 2.2);
    edge *= edge;
    body *= edge;
    skin *= edge;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.5));
    color = lerp(color, CoreColor, saturate(skin * 1.4));
    return float4(color * (body * 0.55 + skin * 1.35), saturate(body * 0.65 + skin * 0.8) * Opacity);
}

// The torn-space wake dragged behind the charge. Procedural — same T_Windstreak3 trap as the trail.
// Heavier and wider than a weapon wake: a 200x300 body should read as mass, not as a bright streak,
// so the palette stays in wine and magenta rather than running to a pale core.
float4 RamWakePixel(float4 sampleColor : COLOR0, float2 c : TEXCOORD0) : COLOR0
{
    float across = abs(c.y - 0.5) * 2.0;
    float along = c.x;

    float spine = saturate(along * 1.3) * saturate((1.0 - along) * 2.2);
    float width = saturate(1.0 - across / max(spine, 0.12));

    float n1 = tex2D(PrimarySampler, float2(c.x * 1.4 - Time * 0.95, c.y * 2.0 + Time * 0.05)).r;
    float n2 = tex2D(DetailSampler, float2(c.x * 2.5 - Time * 1.45, c.y * 1.3 - Time * 0.07)).r;
    float torn = saturate(n1 * 0.95 + n2 * 0.60 - 0.32);

    float body = width * width * saturate(torn + along * 0.35 - 0.06);
    float soul = width * saturate(torn * 2.0 - 0.95) * (0.4 + Active * 0.6);

    float fade = saturate(along * 4.0) * saturate((1.0 - along) * 3.0) * saturate((1.0 - across) * 3.0);
    body *= fade * fade;
    soul *= fade;

    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.45));
    color = lerp(color, CoreColor, saturate(soul * 1.2));
    return float4(color * (body * 0.72 + soul * 1.1), saturate(body * 0.75 + soul * 0.5) * Opacity);
}

technique VesselRammingCore { pass CorePass { PixelShader = compile ps_2_0 RamCorePixel(); } }
technique VesselRammingWake { pass WakePass { PixelShader = compile ps_2_0 RamWakePixel(); } }
