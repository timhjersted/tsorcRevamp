#include "PixelShaderCommon.fxh"

// The Elemental Necromancer's eruption column: a copy of ArtoriasAbyssEruption's EruptionPixel (same plume, rise mask and
// core maths) recoloured to a purple body, a white core and a BLUE outer edge. Never edit the Artorias shader for this one.

sampler PrimarySampler : register(s0);
sampler DetailSampler : register(s1);

float3 DarkColor;
float3 MidColor;
float3 CoreColor;
float Opacity;
float Time;
float Progress;
float2 DrawSize;
float2 PrimaryTextureSize;
float4 PixelGrid;

// The edge band's own colour, so the column stays blue-rimmed whatever body palette the caller passes.
static const float3 EdgeBlue = float3(0.10, 0.42, 1.00);

float4 EruptionPixel(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // 0..1 across the on-screen quad (the caller's source rect is the draw size), then the 4px gameplay-pixel filter.
    float2 uv = PixelateShaderUV(coords * PrimaryTextureSize / max(DrawSize, float2(1.0, 1.0)), PixelGrid);
    float smoke = tex2D(PrimarySampler, uv).r;
    float noise = tex2D(DetailSampler, uv * float2(2.5, 4.2) + float2(Time * 0.055, -Time * 0.24)).r;
    float x = abs(uv.x - 0.5);
    float rise = saturate((Progress - (1.0 - uv.y) * 0.72) * 6.0);

    // The plume wisps outside the core are cut to zero at the side edges (x = 0.5), so no wisp is sliced flat by the quad.
    float sideFade = saturate((0.5 - x) * 10.0);
    float plume = saturate(smoke * 0.68 + noise * 0.46 - 0.39) * rise * sideFade;
    float exactCore = saturate((0.39 - x) * 6.0) * rise;
    float silverChannel = saturate((0.12 - x) * 8.33) * rise;

    // Blue edge: everything outside the purple body (blue from x 0.14, fully blue past 0.30) and every plume wisp beyond
    // it. The extra edge * exactCore intensity keeps the rim bright enough to read as blue, not as a dim violet fringe.
    float edge = saturate((x - 0.14) * 6.25);
    float3 color = lerp(DarkColor, MidColor, plume + exactCore);
    color = lerp(color, EdgeBlue, edge);
    color = lerp(color, CoreColor, silverChannel);
    float intensity = plume * 0.35 + exactCore * (0.68 + edge * 0.45) + silverChannel * 1.35;

    // Same vertical dissolve as the original: the top fades over 22% of its height, the lower 10% sinks into the ground.
    float verticalFade = smoothstep(0.0, 0.22, uv.y) * (1.0 - smoothstep(0.90, 1.0, uv.y));
    return float4(sampleColor.rgb * color * intensity,
        sampleColor.a * saturate(plume * 0.52 + exactCore + silverChannel) * Opacity * verticalFade);
}

technique ElementalEruption
{
    pass EruptionPass { PixelShader = compile ps_2_0 EruptionPixel(); }
}
