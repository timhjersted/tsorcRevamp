// WitchkingVoidCrescent.fx
// The Witchking's Void Flame: a crescent of purple fire, shaped like ")" on its travel axis (the bulge leads), thrown
// off the void blade. Fully procedural; the drawn quad's own texture is only a noise field.
//
// Quad: drawn rotated to the velocity, uv.x = 0 at the back (the crescent's tips) to 1 at the front (its bulge),
// uv.y = 0..1 across its height. Blend contract: BlendState.AlphaBlend, which in XNA is PREMULTIPLIED, so the return is
// float4(color * alpha, alpha). That lets the dark purple actually darken a bright sky instead of vanishing the way it
// would additively.
//
// Uniforms (set from C# by EnemyVFX.DrawWitchkingVoidCrescent):
//   DarkColor = dark purple, MidColor = bright purple, CoreColor = the white accents.
//   Active    = the band's half-thickness at its middle, in quad-width UV (C# divides, so no per-pixel maths here).
//   Direction = a per-projectile noise phase, so several crescents in a fan don't burn identically.
//   PixelGrid = the 2px pixel filter (pre-divided in C#).

#include "PixelShaderCommon.fxh"

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Active;
float Direction;
float4 PixelGrid;

float4 VoidCrescentPixel(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = PixelateShaderUV(coords, PixelGrid);

    // The half oval: its centre line runs from the tips (back edge) out to the front at the middle height,
    // following sqrt(1 - y^2). The band is thickest at the middle and thins to 60% at the tips.
    float y = uv.y * 2.0 - 1.0;
    // +0.0001: ps_2_0 emits sqrt as 1/rsqrt, which divides by zero exactly at the tips.
    float arc = sqrt(saturate(1.0 - y * y) + 0.0001);
    float tipHalf = Active * 0.6;
    float halfThickness = tipHalf + (Active - tipHalf) * arc;
    float centerX = tipHalf + (1.0 - Active - tipHalf) * arc;

    // Two noise layers drifting in opposing directions read as burning, not scrolling.
    float n1 = tex2D(PrimarySampler, uv * float2(1.6, 2.4) + float2(Time * 0.55 + Direction, Time * 0.18)).r;
    float n2 = tex2D(DetailSampler, uv * float2(2.7, 1.5) + float2(-Time * 0.42, Time * 0.33 + Direction)).r;
    float flame = n1 * 0.6 + n2 * 0.5;

    // Distance from the centre line in band-widths. Behind the line (the trailing side) it is scaled down, so the fire
    // licks backward off the leading edge; the noise pushes the edge in and out so the silhouette burns.
    float offset = uv.x - centerX;
    float trailing = step(offset, 0.0);
    float band = abs(offset) / (halfThickness + 0.002) * (1.0 - trailing * 0.45);
    float body = saturate(1.3 - band + (flame - 0.55) * 1.4);

    // Fade the very tips so the ends burn out instead of being cut.
    body *= saturate(arc * 4.0);

    // Dark purple through the outer half of the band, bright purple toward the spine, white accents where both noises
    // peak inside the body.
    float3 color = lerp(DarkColor, MidColor, saturate(body * 1.5 - 0.45));
    float accent = saturate((n1 * n2 - 0.22) * 6.0) * body;
    float core = body * body * body;
    color = lerp(color, CoreColor, saturate(accent + core * 0.6));

    float alpha = saturate(body * 1.6) * Opacity;
    return float4(color * alpha, alpha);
}

technique WitchkingVoidCrescent { pass P { PixelShader = compile ps_2_0 VoidCrescentPixel(); } }
